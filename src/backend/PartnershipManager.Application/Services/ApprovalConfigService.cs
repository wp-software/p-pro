using PartnershipManager.Application.DTOs.Workflow;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Application.Services;

public interface IApprovalConfigService
{
    Task<IEnumerable<ApprovalGroupResponse>> GetGroupsAsync(Guid companyId);
    Task<ApprovalGroupResponse?> GetGroupAsync(Guid id, Guid companyId);
    Task<Guid> CreateGroupAsync(Guid companyId, Guid userId, SaveApprovalGroupRequest request);
    Task<bool> UpdateGroupAsync(Guid id, Guid companyId, Guid userId, SaveApprovalGroupRequest request);
    Task DeleteGroupAsync(Guid id, Guid companyId);

    Task<IEnumerable<WorkflowTemplateResponse>> GetTemplatesAsync(Guid companyId);
    Task<WorkflowTemplateResponse?> GetTemplateAsync(Guid id, Guid companyId);
    Task<Guid> CreateTemplateAsync(Guid companyId, Guid userId, SaveWorkflowTemplateRequest request);
    Task<bool> UpdateTemplateAsync(Guid id, Guid companyId, Guid userId, SaveWorkflowTemplateRequest request);
    Task DeleteTemplateAsync(Guid id, Guid companyId);
}

/// <summary>Grupos de aprovação e modelos de fluxo. Erros de validação lançam <see cref="ArgumentException"/>.</summary>
public class ApprovalConfigService : IApprovalConfigService
{
    private static readonly string[] DueModes = { "none", "calendar_days", "business_days" };
    private static readonly string[] StepTypes = { "approval", "review", "notification", "automated" };

    private readonly IApprovalGroupRepository _groups;
    private readonly IWorkflowTemplateRepository _templates;
    private readonly IUnitOfWork _uow;

    public ApprovalConfigService(
        IApprovalGroupRepository groups,
        IWorkflowTemplateRepository templates,
        IUnitOfWork uow)
    {
        _groups = groups;
        _templates = templates;
        _uow = uow;
    }

    // ── Groups ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ApprovalGroupResponse>> GetGroupsAsync(Guid companyId)
        => (await _groups.GetByCompanyAsync(companyId)).Select(MapGroup);

    public async Task<ApprovalGroupResponse?> GetGroupAsync(Guid id, Guid companyId)
    {
        var g = await _groups.GetByIdAsync(id, companyId);
        return g == null ? null : MapGroup(g);
    }

    public async Task<Guid> CreateGroupAsync(Guid companyId, Guid userId, SaveApprovalGroupRequest request)
    {
        await ValidateGroupAsync(companyId, request);
        var group = new ApprovalGroup
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ApprovalMode = request.ApprovalMode,
            IsActive = request.IsActive,
            CreatedBy = userId,
            UpdatedBy = userId
        };
        return await _groups.CreateAsync(group, request.MemberIds);
    }

    public async Task<bool> UpdateGroupAsync(Guid id, Guid companyId, Guid userId, SaveApprovalGroupRequest request)
    {
        var group = await _groups.GetByIdAsync(id, companyId);
        if (group == null) return false;

        await ValidateGroupAsync(companyId, request);
        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.ApprovalMode = request.ApprovalMode;
        group.IsActive = request.IsActive;
        group.UpdatedAt = DateTime.UtcNow;
        group.UpdatedBy = userId;
        await _groups.UpdateAsync(group, request.MemberIds);
        return true;
    }

    public Task DeleteGroupAsync(Guid id, Guid companyId) => _groups.DeleteAsync(id, companyId);

    private async Task ValidateGroupAsync(Guid companyId, SaveApprovalGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("O nome do grupo é obrigatório.");
        if (!ApprovalModes.IsValid(request.ApprovalMode))
            throw new ArgumentException("Modo de aprovação inválido. Use 'any' ou 'all'.");
        if (request.MemberIds.Count == 0)
            throw new ArgumentException("O grupo precisa ter ao menos um membro.");

        var validIds = (await _uow.Users.GetActiveUsersByCompanyAsync(companyId)).Select(u => u.Id).ToHashSet();
        if (request.MemberIds.Any(id => !validIds.Contains(id)))
            throw new ArgumentException("Um ou mais membros não pertencem à empresa ou estão inativos.");
    }

    private static ApprovalGroupResponse MapGroup(ApprovalGroup g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Description = g.Description,
        ApprovalMode = g.ApprovalMode,
        IsActive = g.IsActive,
        Members = g.Members.Select(m => new ApprovalGroupMemberResponse
        {
            UserId = m.UserId,
            Name = m.UserName ?? string.Empty,
            Email = m.UserEmail
        }).ToList()
    };

    // ── Templates ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<WorkflowTemplateResponse>> GetTemplatesAsync(Guid companyId)
        => (await _templates.GetByCompanyAsync(companyId)).Select(MapTemplate);

    public async Task<WorkflowTemplateResponse?> GetTemplateAsync(Guid id, Guid companyId)
    {
        var t = await _templates.GetByIdAsync(id, companyId);
        return t == null ? null : MapTemplate(t);
    }

    public async Task<Guid> CreateTemplateAsync(Guid companyId, Guid userId, SaveWorkflowTemplateRequest request)
    {
        await ValidateTemplateAsync(companyId, request);
        var template = new WorkflowTemplate
        {
            CompanyId = companyId,
            CreatedBy = userId,
            UpdatedBy = userId
        };
        ApplyTemplate(template, request);
        return await _templates.CreateAsync(template);
    }

    public async Task<bool> UpdateTemplateAsync(Guid id, Guid companyId, Guid userId, SaveWorkflowTemplateRequest request)
    {
        var template = await _templates.GetByIdAsync(id, companyId);
        if (template == null) return false;

        await ValidateTemplateAsync(companyId, request);
        ApplyTemplate(template, request);
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = userId;
        await _templates.UpdateAsync(template);
        return true;
    }

    public Task DeleteTemplateAsync(Guid id, Guid companyId) => _templates.DeleteAsync(id, companyId);

    private async Task ValidateTemplateAsync(Guid companyId, SaveWorkflowTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("O nome do modelo é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.WorkflowType))
            throw new ArgumentException("O tipo do fluxo é obrigatório.");
        if (request.Steps.Count == 0)
            throw new ArgumentException("O modelo precisa ter ao menos uma etapa.");

        var groupIds = (await _groups.GetByCompanyAsync(companyId)).Select(g => g.Id).ToHashSet();
        foreach (var step in request.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
                throw new ArgumentException("Todas as etapas precisam de nome.");
            if (!StepTypes.Contains(step.StepType))
                throw new ArgumentException($"Tipo de etapa inválido: {step.StepType}.");
            if (!DueModes.Contains(step.DueMode))
                throw new ArgumentException($"Modo de prazo inválido: {step.DueMode}.");
            if (step.ApprovalMode != null && !ApprovalModes.IsValid(step.ApprovalMode))
                throw new ArgumentException("Modo de aprovação inválido. Use 'any' ou 'all'.");
            if (step.AssignedGroupId.HasValue && !groupIds.Contains(step.AssignedGroupId.Value))
                throw new ArgumentException("Grupo de aprovação não encontrado.");
        }
    }

    private static void ApplyTemplate(WorkflowTemplate template, SaveWorkflowTemplateRequest request)
    {
        template.Name = request.Name.Trim();
        template.Description = request.Description?.Trim();
        template.WorkflowType = request.WorkflowType;
        template.Priority = request.Priority;
        template.IsActive = request.IsActive;
        template.Steps = request.Steps.Select(s => new WorkflowTemplateStep
        {
            Name = s.Name.Trim(),
            Description = s.Description,
            StepType = s.StepType,
            AssignedRole = s.AssignedGroupId.HasValue || s.AssignedUserId.HasValue ? null : s.AssignedRole,
            AssignedUserId = s.AssignedGroupId.HasValue ? null : s.AssignedUserId,
            AssignedGroupId = s.AssignedGroupId,
            ApprovalMode = s.AssignedGroupId.HasValue ? s.ApprovalMode : null,
            DueMode = s.DueMode,
            DueDays = s.DueMode == "none" ? 0 : Math.Max(0, s.DueDays)
        }).ToList();
    }

    private static WorkflowTemplateResponse MapTemplate(WorkflowTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        WorkflowType = t.WorkflowType,
        Priority = t.Priority,
        IsActive = t.IsActive,
        Steps = t.Steps.OrderBy(s => s.StepOrder).Select(s => new WorkflowTemplateStepDto
        {
            Name = s.Name,
            Description = s.Description,
            StepType = s.StepType,
            AssignedRole = s.AssignedRole,
            AssignedUserId = s.AssignedUserId,
            AssignedGroupId = s.AssignedGroupId,
            AssignedGroupName = s.AssignedGroupName,
            ApprovalMode = s.ApprovalMode,
            DueMode = s.DueMode,
            DueDays = s.DueDays
        }).ToList()
    };
}
