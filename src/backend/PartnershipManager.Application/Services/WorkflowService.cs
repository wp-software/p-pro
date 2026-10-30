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
    private readonly IApprovalGroupRepository _groups;
    private readonly INotificationService _notifications;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WorkflowService> _logger;
    private readonly IConfiguration _configuration;

    public WorkflowService(
        IWorkflowRepository repo,
        IApprovalGroupRepository groups,
        INotificationService notifications,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<WorkflowService> logger,
        IConfiguration configuration)
    {
        _repo = repo;
        _groups = groups;
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

        var steps = new List<WorkflowStep>();
        foreach (var r in request.Steps)
            steps.Add(await BuildStepAsync(companyId, r));

        var workflowId = await _repo.CreateAsync(workflow, steps);

        var firstStep = steps.FirstOrDefault(s => s.StepOrder == 1) ?? steps.FirstOrDefault();
        if (firstStep != null)
            await NotifyStepApproversAsync(companyId, workflowId, workflow, firstStep);

        return workflowId;
    }

    private async Task<WorkflowStep> BuildStepAsync(Guid companyId, CreateWorkflowStepRequest r)
    {
        var step = new WorkflowStep
        {
            Name = r.Name,
            Description = r.Description,
            StepType = r.StepType,
            AssignedRole = r.AssignedRole,
            AssignedUserId = r.AssignedUserId,
            DueDate = r.DueDate
        };

        if (r.AssignedGroupId == null) return step;

        if (r.ApprovalMode != null && !ApprovalModes.IsValid(r.ApprovalMode))
            throw new ArgumentException("Modo de aprovação inválido. Use 'any' ou 'all'.");

        var group = await _groups.GetByIdAsync(r.AssignedGroupId.Value, companyId)
            ?? throw new ArgumentException("Grupo de aprovação não encontrado.");
        if (!group.IsActive)
            throw new ArgumentException($"O grupo \"{group.Name}\" está inativo.");
        if (group.Members.Count == 0)
            throw new ArgumentException($"O grupo \"{group.Name}\" não possui membros.");

        // Membros e modo são copiados para a etapa: mudanças futuras no grupo não afetam fluxos em andamento.
        step.AssignedGroupId = group.Id;
        step.AssignedUserId = null;
        step.AssignedRole = null;
        step.ApprovalMode = r.ApprovalMode ?? group.ApprovalMode;
        step.Approvers = group.Members
            .Select(m => new WorkflowStepApprover { UserId = m.UserId, UserName = m.UserName })
            .ToList();
        return step;
    }

    private async Task NotifyStepApproversAsync(Guid companyId, Guid workflowId, Workflow workflow, WorkflowStep step)
    {
        var userIds = step.Approvers.Count > 0
            ? step.Approvers.Select(a => a.UserId).ToList()
            : step.AssignedUserId.HasValue ? new List<Guid> { step.AssignedUserId.Value } : new List<Guid>();
        if (userIds.Count == 0) return;

        var requester = await _uow.Users.GetByIdAsync(workflow.RequestedBy);
        var requesterName = requester?.Name ?? "Sistema";
        var inAppActionUrl = $"/approvals/{workflowId}";
        var emailActionUrl = $"{GetFrontendUrl()}/approvals/{workflowId}";

        foreach (var userId in userIds)
        {
            await _notifications.NotifyAsync(
                companyId,
                userId,
                "workflow_assigned",
                $"Aprovação solicitada: {workflow.Title}",
                $"Você foi designado como aprovador na etapa \"{step.Name}\". Solicitado por: {requesterName}.",
                actionUrl: inAppActionUrl,
                referenceType: "workflow",
                referenceId: workflowId);

            var pref = await _notifications.GetPreferenceChannelAsync(userId, "workflow_assigned");
            if (pref == "none" || pref == "in_app") continue;

            var assignee = await _uow.Users.GetByIdAsync(userId);
            if (assignee == null) continue;
            try
            {
                await _emailService.SendApprovalAssignedEmailAsync(
                    assignee.Email, assignee.Name,
                    workflow.Title, step.Name, requesterName,
                    workflow.Priority, step.DueDate, emailActionUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de atribuição de aprovação para {Email}", assignee.Email);
            }
        }
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

    /// <summary>Valida estado do fluxo/etapa e elegibilidade do usuário; retorna o fluxo e a etapa.</summary>
    private async Task<(Workflow Workflow, WorkflowStep Step)> LoadDecidableStepAsync(
        Guid workflowId, Guid stepId, Guid companyId, Guid userId)
    {
        var workflow = await _repo.GetByIdAsync(workflowId, companyId)
            ?? throw new InvalidOperationException("Workflow não encontrado.");
        if (workflow.Status is not (WorkflowStatuses.Pending or WorkflowStatuses.InProgress))
            throw new InvalidOperationException("Este fluxo não está mais em andamento.");

        var step = workflow.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException("Etapa não encontrada.");
        if (!step.IsCurrent || step.Status != WorkflowStepStatuses.InProgress)
            throw new InvalidOperationException("Esta etapa não está aguardando decisão.");

        if (step.AssignedGroupId.HasValue || step.Approvers.Count > 0)
        {
            if (step.Approvers.All(a => a.UserId != userId))
                throw new UnauthorizedAccessException("Você não é membro do grupo aprovador desta etapa.");
        }
        else if (step.AssignedUserId.HasValue && step.AssignedUserId != userId)
        {
            throw new UnauthorizedAccessException("Você não é o aprovador desta etapa.");
        }

        if (step.Approvals.Any(a => a.UserId == userId))
            throw new InvalidOperationException("Você já registrou sua decisão nesta etapa.");

        return (workflow, step);
    }

    public async Task ApproveStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string? comments = null)
    {
        var (workflow, step) = await LoadDecidableStepAsync(workflowId, stepId, companyId, userId);

        await _repo.RecordApprovalAsync(new WorkflowApproval
        {
            WorkflowStepId = stepId,
            UserId = userId,
            Decision = WorkflowDecisions.Approved,
            Comments = comments,
            DecidedAt = DateTime.UtcNow
        });

        // Grupo em modo "all": a etapa só conclui quando todos os membros aprovarem.
        if (step.Approvers.Count > 0 && step.ApprovalMode == ApprovalModes.All)
        {
            var approvedIds = step.Approvals
                .Where(a => a.Decision == WorkflowDecisions.Approved)
                .Select(a => a.UserId)
                .Append(userId)
                .ToHashSet();
            if (step.Approvers.Any(a => !approvedIds.Contains(a.UserId)))
                return;
        }

        await _repo.UpdateStepStatusAsync(stepId, WorkflowStepStatuses.Completed, userId);

        var approver = await _uow.Users.GetByIdAsync(userId);
        var approverName = approver?.Name ?? "Aprovador";

        if (workflow.CurrentStep >= workflow.TotalSteps)
        {
            await _repo.CompleteWorkflowAsync(workflowId, WorkflowStatuses.Approved);
            await _NotifyRequesterDecisionAsync(companyId, workflow, "approved", approverName, comments);
            return;
        }

        var nextStepNumber = workflow.CurrentStep + 1;
        await _repo.AdvanceStepAsync(workflowId, nextStepNumber);

        var nextStep = workflow.Steps.FirstOrDefault(s => s.StepOrder == nextStepNumber);
        if (nextStep != null)
            await NotifyStepApproversAsync(companyId, workflowId, workflow, nextStep);
    }

    public async Task RejectStepAsync(Guid workflowId, Guid stepId, Guid companyId, Guid userId, string comments)
    {
        var (workflow, _) = await LoadDecidableStepAsync(workflowId, stepId, companyId, userId);

        // Uma rejeição de qualquer membro encerra o fluxo, em qualquer modo de aprovação.
        await _repo.RecordApprovalAsync(new WorkflowApproval
        {
            WorkflowStepId = stepId,
            UserId = userId,
            Decision = WorkflowDecisions.Rejected,
            Comments = comments,
            DecidedAt = DateTime.UtcNow
        });
        await _repo.UpdateStepStatusAsync(stepId, WorkflowStepStatuses.Completed, userId);
        await _repo.CompleteWorkflowAsync(workflowId, WorkflowStatuses.Rejected);

        var approver = await _uow.Users.GetByIdAsync(userId);
        var approverName = approver?.Name ?? "Aprovador";

        await _NotifyRequesterDecisionAsync(companyId, workflow, "rejected", approverName, comments);
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
            AssignedGroupId = s.AssignedGroupId,
            AssignedGroupName = s.AssignedGroupName,
            ApprovalMode = s.ApprovalMode,
            Status = s.Status,
            IsCurrent = s.IsCurrent,
            StartedAt = s.StartedAt,
            DueDate = s.DueDate,
            CompletedAt = s.CompletedAt,
            Notes = s.Notes,
            Approvers = s.Approvers.Select(a => new StepApproverResponse
            {
                UserId = a.UserId,
                UserName = a.UserName ?? string.Empty
            }).ToList(),
            Approvals = s.Approvals.Select(a => new StepApprovalResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName ?? string.Empty,
                Decision = a.Decision,
                Comments = a.Comments,
                DecidedAt = a.DecidedAt
            }).ToList()
        }).ToList()
    };
}
