using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.Workflow;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Application.Services;

public interface IWorkflowService
{
    Task<Guid> CreateAsync(Guid companyId, CreateWorkflowRequest request, Guid requestedBy);
    Task<WorkflowResponse?> GetByIdAsync(Guid id, Guid companyId);
    Task<PagedResult<WorkflowResponse>> GetByCompanyAsync(Guid companyId, int page, int pageSize, string? status = null, string? workflowType = null);
    Task<IEnumerable<WorkflowResponse>> GetPendingByUserAsync(Guid userId, Guid companyId);
    Task ApproveStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string? comments = null);
    Task RejectStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string comments);
    Task CancelAsync(Guid workflowId, Guid companyId, Guid cancelledBy, string reason);
}

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _repo;
    private readonly INotificationService _notifications;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WorkflowService> _logger;
    private readonly IConfiguration _configuration;

    public WorkflowService(
        IWorkflowRepository repo,
        INotificationService notifications,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<WorkflowService> logger,
        IConfiguration configuration)
    {
        _repo = repo;
        _notifications = notifications;
        _emailService = emailService;
        _uow = uow;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetFrontendUrl()
    {
        return (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
    }

    public async Task<Guid> CreateAsync(Guid companyId, CreateWorkflowRequest request, Guid requestedBy)
    {
        var workflow = new Workflow
        {
            CompanyId = companyId,
            WorkflowType = request.WorkflowType,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            Title = request.Title,
            Description = request.Description,
            Status = WorkflowStatuses.Pending,
            Priority = request.Priority,
            RequestedBy = requestedBy,
            RequestedAt = DateTime.UtcNow,
            DueDate = request.DueDate
        };
        workflow.CreatedBy = requestedBy;
        workflow.UpdatedBy = requestedBy;

        var steps = request.Steps.Select(s => new WorkflowStep
        {
            Name = s.Name,
            Description = s.Description,
            StepType = s.StepType,
            AssignedRole = s.AssignedRole,
            AssignedUserId = s.AssignedUserId,
            DueDate = s.DueDate
        }).ToList();

        var workflowId = await _repo.CreateAsync(workflow, steps);

        // Notificar aprovador da primeira etapa
        var firstStep = steps.FirstOrDefault(s => s.StepOrder == 1) ?? steps.FirstOrDefault();
        if (firstStep?.AssignedUserId != null)
        {
            var requester = await _uow.Users.GetByIdAsync(requestedBy);
            var requesterName = requester?.Name ?? "Sistema";
            var inAppActionUrl = $"/approvals/{workflowId}";
            var emailActionUrl = $"{GetFrontendUrl()}/approvals/{workflowId}";

            await _notifications.NotifyAsync(
                companyId,
                firstStep.AssignedUserId.Value,
                "workflow_assigned",
                $"Aprovação solicitada: {workflow.Title}",
                $"Você foi designado como aprovador na etapa \"{firstStep.Name}\". Solicitado por: {requesterName}.",
                actionUrl: inAppActionUrl,
                referenceType: "workflow",
                referenceId: workflowId);

            var pref = await _notifications.GetPreferenceChannelAsync(firstStep.AssignedUserId.Value, "workflow_assigned");
            if (pref != "none" && pref != "in_app")
            {
                var assignee = await _uow.Users.GetByIdAsync(firstStep.AssignedUserId.Value);
                if (assignee != null)
                {
                    try
                    {
                        await _emailService.SendApprovalAssignedEmailAsync(
                            assignee.Email, assignee.Name,
                            workflow.Title, firstStep.Name, requesterName,
                            workflow.Priority, firstStep.DueDate, emailActionUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao enviar e-mail de atribuição de aprovação para {Email}", assignee.Email);
                    }
                }
            }
        }

        return workflowId;
    }

    public async Task<WorkflowResponse?> GetByIdAsync(Guid id, Guid companyId)
    {
        var w = await _repo.GetByIdAsync(id, companyId);
        return w == null ? null : MapToResponse(w);
    }

    public async Task<PagedResult<WorkflowResponse>> GetByCompanyAsync(
        Guid companyId, int page, int pageSize, string? status = null, string? workflowType = null)
    {
        var (items, total) = await _repo.GetByCompanyAsync(companyId, page, pageSize, status, workflowType);
        return new PagedResult<WorkflowResponse>(items.Select(MapToResponse), total, page, pageSize);
    }

    public async Task<IEnumerable<WorkflowResponse>> GetPendingByUserAsync(Guid userId, Guid companyId)
    {
        var items = await _repo.GetPendingByUserAsync(userId, companyId);
        return items.Select(MapToResponse);
    }

    public async Task ApproveStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string? comments = null)
    {
        var approval = new WorkflowApproval
        {
            WorkflowStepId = stepId,
            UserId = userId,
            Decision = WorkflowDecisions.Approved,
            Comments = comments,
            DecidedAt = DateTime.UtcNow
        };
        await _repo.RecordApprovalAsync(approval);
        await _repo.UpdateStepStatusAsync(stepId, WorkflowStepStatuses.Completed, userId);

        var workflow = await _repo.GetByIdAsync(workflowId, companyId)
            ?? throw new InvalidOperationException("Workflow não encontrado.");

        var approver = await _uow.Users.GetByIdAsync(userId);
        var approverName = approver?.Name ?? "Aprovador";

        if (workflow.CurrentStep >= workflow.TotalSteps)
        {
            // Fluxo completamente aprovado — notificar solicitante
            await _repo.CompleteWorkflowAsync(workflowId, WorkflowStatuses.Approved);

            await _NotifyRequesterDecisionAsync(
                companyId, workflow, "approved", approverName, comments);
        }
        else
        {
            // Avança para próxima etapa
            var nextStepNumber = workflow.CurrentStep + 1;
            await _repo.AdvanceStepAsync(workflowId, nextStepNumber);

            // Notificar aprovador da próxima etapa
            var nextStepAssignee = workflow.Steps
                .FirstOrDefault(s => s.StepOrder == nextStepNumber);
            if (nextStepAssignee?.AssignedUserId != null)
            {
                var requester = await _uow.Users.GetByIdAsync(workflow.RequestedBy);
                var requesterName = requester?.Name ?? "Sistema";
                var inAppActionUrl = $"/approvals/{workflowId}";
                var emailActionUrl = $"{GetFrontendUrl()}/approvals/{workflowId}";

                await _notifications.NotifyAsync(
                    companyId,
                    nextStepAssignee.AssignedUserId.Value,
                    "workflow_assigned",
                    $"Aprovação solicitada: {workflow.Title}",
                    $"Você foi designado como aprovador na etapa \"{nextStepAssignee.Name}\". Solicitado por: {requesterName}.",
                    actionUrl: inAppActionUrl,
                    referenceType: "workflow",
                    referenceId: workflowId);

                var pref = await _notifications.GetPreferenceChannelAsync(nextStepAssignee.AssignedUserId.Value, "workflow_assigned");
                if (pref != "none" && pref != "in_app")
                {
                    var assigneeUser = await _uow.Users.GetByIdAsync(nextStepAssignee.AssignedUserId.Value);
                    if (assigneeUser != null)
                    {
                        try
                        {
                            await _emailService.SendApprovalAssignedEmailAsync(
                                assigneeUser.Email, assigneeUser.Name,
                                workflow.Title, nextStepAssignee.Name, requesterName,
                                workflow.Priority, nextStepAssignee.DueDate, emailActionUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Falha ao enviar e-mail de etapa ao aprovador {Email}", assigneeUser.Email);
                        }
                    }
                }
            }
        }
    }

    public async Task RejectStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string comments)
    {
        var approval = new WorkflowApproval
        {
            WorkflowStepId = stepId,
            UserId = userId,
            Decision = WorkflowDecisions.Rejected,
            Comments = comments,
            DecidedAt = DateTime.UtcNow
        };
        await _repo.RecordApprovalAsync(approval);
        await _repo.UpdateStepStatusAsync(stepId, WorkflowStepStatuses.Completed, userId);
        await _repo.CompleteWorkflowAsync(workflowId, WorkflowStatuses.Rejected);

        var workflow = await _repo.GetByIdAsync(workflowId, companyId);
        if (workflow == null) return;

        var approver = await _uow.Users.GetByIdAsync(userId);
        var approverName = approver?.Name ?? "Aprovador";

        await _NotifyRequesterDecisionAsync(
            companyId, workflow, "rejected", approverName, comments);
    }

    private async Task _NotifyRequesterDecisionAsync(
        Guid companyId, Workflow workflow, string finalStatus,
        string approverName, string? comments)
    {
        var statusLabel = finalStatus == "approved" ? "aprovado" : "rejeitado";
        var inAppActionUrl = $"/approvals/{workflow.Id}";
        var emailActionUrl = $"{GetFrontendUrl()}/approvals/{workflow.Id}";

        // Notificação in-app
        await _notifications.NotifyAsync(
            companyId,
            workflow.RequestedBy,
            finalStatus == "approved" ? "workflow_approved" : "workflow_rejected",
            $"Fluxo {statusLabel}: {workflow.Title}",
            string.IsNullOrWhiteSpace(comments)
                ? $"O fluxo \"{workflow.Title}\" foi {statusLabel} por {approverName}."
                : $"O fluxo \"{workflow.Title}\" foi {statusLabel} por {approverName}. Comentários: {comments}",
            actionUrl: inAppActionUrl,
            referenceType: "workflow",
            referenceId: workflow.Id);

        // E-mail ao solicitante
        var notifType = finalStatus == "approved" ? "workflow_approved" : "workflow_rejected";
        var pref = await _notifications.GetPreferenceChannelAsync(workflow.RequestedBy, notifType);
        if (pref == "none" || pref == "in_app") return;

        var requester = await _uow.Users.GetByIdAsync(workflow.RequestedBy);
        if (requester == null) return;

        try
        {
            await _emailService.SendApprovalDecisionEmailAsync(
                requester.Email, requester.Name,
                workflow.Title, finalStatus, approverName,
                comments, emailActionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de decisão de aprovação para {Email}", requester.Email);
        }
    }

    public Task CancelAsync(Guid workflowId, Guid companyId, Guid cancelledBy, string reason)
        => _repo.CancelWorkflowAsync(workflowId, cancelledBy, reason);

    private static readonly Dictionary<string, string> WorkflowTypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["contract_approval"]       = "Aprovação de Contrato",
        ["shareholder_change"]      = "Alteração de Sócio",
        ["communication_approval"]  = "Aprovação de Comunicado",
        ["document_verification"]   = "Verificação de Documento",
        ["vesting_approval"]        = "Aprovação de Vesting",
    };

    private static WorkflowResponse MapToResponse(Workflow w) => new()
    {
        Id = w.Id,
        CompanyId = w.CompanyId,
        WorkflowType = w.WorkflowType,
        WorkflowTypeLabel = WorkflowTypeLabels.TryGetValue(w.WorkflowType, out var label) ? label : w.WorkflowType,
        ReferenceType = w.ReferenceType,
        ReferenceId = w.ReferenceId,
        Title = w.Title,
        Description = w.Description,
        Status = w.Status,
        Priority = w.Priority,
        CurrentStep = w.CurrentStep,
        TotalSteps = w.TotalSteps,
        RequestedBy = w.RequestedBy,
        RequestedByName = w.RequestedByName ?? w.RequestedBy.ToString(),
        RequestedAt = w.RequestedAt,
        DueDate = w.DueDate,
        CompletedAt = w.CompletedAt,
        CreatedAt = w.CreatedAt,
        Steps = w.Steps.Select(s => new WorkflowStepResponse
        {
            Id = s.Id,
            StepOrder = s.StepOrder,
            Name = s.Name,
            Description = s.Description,
            StepType = s.StepType,
            AssignedRole = s.AssignedRole,
            AssignedUserId = s.AssignedUserId,
            Status = s.Status,
            IsCurrent = s.IsCurrent,
            StartedAt = s.StartedAt,
            DueDate = s.DueDate,
            CompletedAt = s.CompletedAt,
            Notes = s.Notes
        }).ToList()
    };
}
