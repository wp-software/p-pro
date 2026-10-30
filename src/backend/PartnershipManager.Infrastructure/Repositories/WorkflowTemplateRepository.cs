using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Persistence;

namespace PartnershipManager.Infrastructure.Repositories;

public class WorkflowTemplateRepository : IWorkflowTemplateRepository
{
    private const string TemplateColumns = @"id AS Id, company_id AS CompanyId, name AS Name, description AS Description,
        workflow_type AS WorkflowType, priority AS Priority, is_active AS IsActive,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    private readonly DapperContext _context;

    public WorkflowTemplateRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkflowTemplate>> GetByCompanyAsync(Guid companyId)
    {
        var templates = (await _context.Connection.QueryAsync<WorkflowTemplate>(
            $"SELECT {TemplateColumns} FROM workflow_templates WHERE company_id = @CompanyId ORDER BY name",
            new { CompanyId = companyId })).ToList();
        if (templates.Count == 0) return templates;

        var steps = (await QuerySteps("s.template_id IN @Ids", new { Ids = templates.Select(t => t.Id).ToArray() })).ToList();
        foreach (var t in templates)
            t.Steps = steps.Where(s => s.TemplateId == t.Id).ToList();
        return templates;
    }

    public async Task<WorkflowTemplate?> GetByIdAsync(Guid id, Guid companyId)
    {
        var template = await _context.Connection.QueryFirstOrDefaultAsync<WorkflowTemplate>(
            $"SELECT {TemplateColumns} FROM workflow_templates WHERE id = @Id AND company_id = @CompanyId",
            new { Id = id, CompanyId = companyId });
        if (template != null)
            template.Steps = (await QuerySteps("s.template_id = @Id", new { Id = id })).ToList();
        return template;
    }

    public async Task<Guid> CreateAsync(WorkflowTemplate template)
    {
        using var tx = await _context.BeginTransactionAsync();
        try
        {
            await _context.Connection.ExecuteAsync(@"
                INSERT INTO workflow_templates (id, company_id, name, description, workflow_type, priority, is_active,
                    created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @CompanyId, @Name, @Description, @WorkflowType, @Priority, @IsActive,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy)", template, tx);
            await InsertStepsAsync(template, tx);
            _context.CommitTransaction();
            return template.Id;
        }
        catch
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public async Task UpdateAsync(WorkflowTemplate template)
    {
        using var tx = await _context.BeginTransactionAsync();
        try
        {
            await _context.Connection.ExecuteAsync(@"
                UPDATE workflow_templates
                SET name = @Name, description = @Description, workflow_type = @WorkflowType,
                    priority = @Priority, is_active = @IsActive, updated_at = @UpdatedAt, updated_by = @UpdatedBy
                WHERE id = @Id AND company_id = @CompanyId", template, tx);
            await _context.Connection.ExecuteAsync(
                "DELETE FROM workflow_template_steps WHERE template_id = @Id", new { template.Id }, tx);
            await InsertStepsAsync(template, tx);
            _context.CommitTransaction();
        }
        catch
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public Task DeleteAsync(Guid id, Guid companyId)
        => _context.Connection.ExecuteAsync(
            "DELETE FROM workflow_templates WHERE id = @Id AND company_id = @CompanyId",
            new { Id = id, CompanyId = companyId });

    private Task<IEnumerable<WorkflowTemplateStep>> QuerySteps(string where, object param)
        => _context.Connection.QueryAsync<WorkflowTemplateStep>($@"
            SELECT s.id AS Id, s.template_id AS TemplateId, s.step_order AS StepOrder, s.name AS Name,
                   s.description AS Description, s.step_type AS StepType, s.assigned_role AS AssignedRole,
                   s.assigned_user_id AS AssignedUserId, s.assigned_group_id AS AssignedGroupId,
                   g.name AS AssignedGroupName, s.approval_mode AS ApprovalMode,
                   s.due_mode AS DueMode, s.due_days AS DueDays
            FROM workflow_template_steps s
            LEFT JOIN approval_groups g ON g.id = s.assigned_group_id
            WHERE {where} ORDER BY s.template_id, s.step_order", param);

    private async Task InsertStepsAsync(WorkflowTemplate template, System.Data.IDbTransaction tx)
    {
        var order = 1;
        foreach (var step in template.Steps)
        {
            step.TemplateId = template.Id;
            step.StepOrder = order++;
            await _context.Connection.ExecuteAsync(@"
                INSERT INTO workflow_template_steps (id, template_id, step_order, name, description, step_type,
                    assigned_role, assigned_user_id, assigned_group_id, approval_mode, due_mode, due_days)
                VALUES (@Id, @TemplateId, @StepOrder, @Name, @Description, @StepType,
                    @AssignedRole, @AssignedUserId, @AssignedGroupId, @ApprovalMode, @DueMode, @DueDays)", step, tx);
        }
    }
}
