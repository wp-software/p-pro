using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Persistence;

namespace PartnershipManager.Infrastructure.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly DapperContext _context;

    public WorkflowRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(Workflow workflow, IEnumerable<WorkflowStep> steps)
    {
        var stepList = steps.ToList();
        workflow.TotalSteps = stepList.Count;

        using var tx = await _context.BeginTransactionAsync();
        try
        {
            var workflowSql = @"
                INSERT INTO workflows (id, company_id, workflow_type, reference_type, reference_id, title, description,
                    status, priority, current_step, total_steps, requested_by, requested_at, due_date, metadata,
                    created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @CompanyId, @WorkflowType, @ReferenceType, @ReferenceId, @Title, @Description,
                    @Status, @Priority, @CurrentStep, @TotalSteps, @RequestedBy, @RequestedAt, @DueDate, @Metadata,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy)";
            await _context.Connection.ExecuteAsync(workflowSql, workflow, tx);

            for (int i = 0; i < stepList.Count; i++)
            {
                var step = stepList[i];
                step.Id = Guid.NewGuid();
                step.WorkflowId = workflow.Id;
                step.StepOrder = i + 1;
                step.IsCurrent = i == 0;
                step.Status = i == 0 ? WorkflowStepStatuses.InProgress : WorkflowStepStatuses.Pending;
                step.CreatedAt = DateTime.UtcNow;
                step.UpdatedAt = DateTime.UtcNow;
                if (i == 0) step.StartedAt = DateTime.UtcNow;

                var stepSql = @"
                    INSERT INTO workflow_steps (id, workflow_id, step_order, name, description, step_type,
                        assigned_role, assigned_user_id, assigned_group_id, approval_mode, status, is_current, started_at, due_date, created_at, updated_at)
                    VALUES (@Id, @WorkflowId, @StepOrder, @Name, @Description, @StepType,
                        @AssignedRole, @AssignedUserId, @AssignedGroupId, @ApprovalMode, @Status, @IsCurrent, @StartedAt, @DueDate, @CreatedAt, @UpdatedAt)";

                await _context.Connection.ExecuteAsync(stepSql, step, tx);

                foreach (var approver in step.Approvers)
                {
                    await _context.Connection.ExecuteAsync(
                        "INSERT INTO workflow_step_approvers (workflow_step_id, user_id) VALUES (@StepId, @UserId)",
                        new { StepId = step.Id, approver.UserId }, tx);
                }
            }

            _context.CommitTransaction();
            return workflow.Id;
        }
        catch
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public async Task<Workflow?> GetByIdAsync(Guid id, Guid companyId)
    {
        var sql = companyId == Guid.Empty
            ? @"SELECT id AS Id, company_id AS CompanyId, workflow_type AS WorkflowType, reference_type AS ReferenceType,
                       reference_id AS ReferenceId, title AS Title, description AS Description, status AS Status,
                       priority AS Priority, current_step AS CurrentStep, total_steps AS TotalSteps,
                       requested_by AS RequestedBy, requested_at AS RequestedAt, due_date AS DueDate,
                       completed_at AS CompletedAt, cancelled_at AS CancelledAt, cancelled_by AS CancelledBy,
                       cancellation_reason AS CancellationReason, metadata AS Metadata,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workflows WHERE id = @Id AND deleted_at IS NULL"
            : @"SELECT id AS Id, company_id AS CompanyId, workflow_type AS WorkflowType, reference_type AS ReferenceType,
                       reference_id AS ReferenceId, title AS Title, description AS Description, status AS Status,
                       priority AS Priority, current_step AS CurrentStep, total_steps AS TotalSteps,
                       requested_by AS RequestedBy, requested_at AS RequestedAt, due_date AS DueDate,
                       completed_at AS CompletedAt, cancelled_at AS CancelledAt, cancelled_by AS CancelledBy,
                       cancellation_reason AS CancellationReason, metadata AS Metadata,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM workflows WHERE id = @Id AND company_id = @CompanyId AND deleted_at IS NULL";

        var workflow = await _context.Connection.QueryFirstOrDefaultAsync<Workflow>(sql, new { Id = id, CompanyId = companyId });
        if (workflow != null)
            workflow.Steps = (await GetStepsWithDetailsAsync(id)).ToList();
        return workflow;
    }

    public async Task<(IEnumerable<Workflow> Items, int Total)> GetByCompanyAsync(
        Guid companyId, int page, int pageSize, string? status = null, string? workflowType = null)
    {
        var conditions = new List<string> { "company_id = @CompanyId", "deleted_at IS NULL" };
        var parameters = new DynamicParameters();
        parameters.Add("CompanyId", companyId);

        if (!string.IsNullOrWhiteSpace(status)) { conditions.Add("status = @Status"); parameters.Add("Status", status); }
        if (!string.IsNullOrWhiteSpace(workflowType)) { conditions.Add("workflow_type = @WorkflowType"); parameters.Add("WorkflowType", workflowType); }

        var where = string.Join(" AND ", conditions);
        var total = await _context.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM workflows WHERE {where}", parameters);

        var sql = $@"
            SELECT id AS Id, company_id AS CompanyId, workflow_type AS WorkflowType,
                   reference_type AS ReferenceType, reference_id AS ReferenceId,
                   title AS Title, description AS Description, status AS Status, priority AS Priority,
                   current_step AS CurrentStep, total_steps AS TotalSteps,
                   requested_by AS RequestedBy, requested_at AS RequestedAt, due_date AS DueDate,
                   completed_at AS CompletedAt, cancelled_at AS CancelledAt, cancelled_by AS CancelledBy,
                   cancellation_reason AS CancellationReason, metadata AS Metadata,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM workflows WHERE {where}
            ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var items = await _context.Connection.QueryAsync<Workflow>(sql, parameters);
        return (items, total);
    }

    public async Task<IEnumerable<Workflow>> GetPendingByUserAsync(Guid userId, Guid companyId)
    {
        var sql = @"
            SELECT DISTINCT w.id AS Id, w.company_id AS CompanyId, w.workflow_type AS WorkflowType,
                   w.reference_type AS ReferenceType, w.reference_id AS ReferenceId,
                   w.title AS Title, w.description AS Description, w.status AS Status,
                   w.priority AS Priority, w.current_step AS CurrentStep, w.total_steps AS TotalSteps,
                   w.requested_by AS RequestedBy,
                   COALESCE(u.name, CAST(w.requested_by AS CHAR)) AS RequestedByName,
                   w.requested_at AS RequestedAt, w.due_date AS DueDate,
                   w.created_at AS CreatedAt, w.updated_at AS UpdatedAt
            FROM workflows w
            LEFT JOIN users u ON u.id = w.requested_by
            INNER JOIN workflow_steps ws ON ws.workflow_id = w.id AND ws.is_current = 1
                AND (ws.assigned_user_id = @UserId OR ws.assigned_role IS NOT NULL
                     OR EXISTS (SELECT 1 FROM workflow_step_approvers sa
                                WHERE sa.workflow_step_id = ws.id AND sa.user_id = @UserId))
                AND NOT EXISTS (SELECT 1 FROM workflow_approvals wa
                                WHERE wa.workflow_step_id = ws.id AND wa.user_id = @UserId)
            WHERE w.company_id = @CompanyId AND w.status IN ('pending','in_progress') AND w.deleted_at IS NULL";
        return await _context.Connection.QueryAsync<Workflow>(sql, new { UserId = userId, CompanyId = companyId });
    }

    public async Task<WorkflowStep?> GetCurrentStepAsync(Guid workflowId)
    {
        var sql = @"
            SELECT ws.id AS Id, ws.workflow_id AS WorkflowId, ws.step_order AS StepOrder, ws.name AS Name,
                   ws.description AS Description, ws.step_type AS StepType, ws.assigned_role AS AssignedRole,
                   ws.assigned_user_id AS AssignedUserId, ws.assigned_group_id AS AssignedGroupId,
                   g.name AS AssignedGroupName, ws.approval_mode AS ApprovalMode,
                   ws.status AS Status, ws.is_current AS IsCurrent,
                   ws.started_at AS StartedAt, ws.due_date AS DueDate, ws.completed_at AS CompletedAt,
                   ws.completed_by AS CompletedBy, ws.notes AS Notes, ws.created_at AS CreatedAt, ws.updated_at AS UpdatedAt
            FROM workflow_steps ws LEFT JOIN approval_groups g ON g.id = ws.assigned_group_id WHERE ws.workflow_id = @WorkflowId AND ws.is_current = 1 AND ws.deleted_at IS NULL";
        return await _context.Connection.QueryFirstOrDefaultAsync<WorkflowStep>(sql, new { WorkflowId = workflowId });
    }

    public async Task<IEnumerable<WorkflowStep>> GetStepsAsync(Guid workflowId)
    {
        var sql = @"
            SELECT ws.id AS Id, ws.workflow_id AS WorkflowId, ws.step_order AS StepOrder, ws.name AS Name,
                   ws.description AS Description, ws.step_type AS StepType, ws.assigned_role AS AssignedRole,
                   ws.assigned_user_id AS AssignedUserId, ws.assigned_group_id AS AssignedGroupId,
                   g.name AS AssignedGroupName, ws.approval_mode AS ApprovalMode,
                   ws.status AS Status, ws.is_current AS IsCurrent,
                   ws.started_at AS StartedAt, ws.due_date AS DueDate, ws.completed_at AS CompletedAt,
                   ws.completed_by AS CompletedBy, ws.notes AS Notes, ws.created_at AS CreatedAt, ws.updated_at AS UpdatedAt
            FROM workflow_steps ws LEFT JOIN approval_groups g ON g.id = ws.assigned_group_id WHERE ws.workflow_id = @WorkflowId AND ws.deleted_at IS NULL ORDER BY ws.step_order";
        return await _context.Connection.QueryAsync<WorkflowStep>(sql, new { WorkflowId = workflowId });
    }

    private async Task<IEnumerable<WorkflowStep>> GetStepsWithDetailsAsync(Guid workflowId)
    {
        var steps = (await GetStepsAsync(workflowId)).ToList();
        if (steps.Count == 0) return steps;

        var ids = steps.Select(s => s.Id).ToArray();
        var approvers = (await _context.Connection.QueryAsync<WorkflowStepApprover>(@"
            SELECT sa.workflow_step_id AS WorkflowStepId, sa.user_id AS UserId, u.name AS UserName
            FROM workflow_step_approvers sa LEFT JOIN users u ON u.id = sa.user_id
            WHERE sa.workflow_step_id IN @Ids", new { Ids = ids })).ToList();
        var approvals = (await _context.Connection.QueryAsync<WorkflowApproval>(@"
            SELECT wa.id AS Id, wa.workflow_step_id AS WorkflowStepId, wa.user_id AS UserId,
                   wa.decision AS Decision, wa.comments AS Comments, wa.decided_at AS DecidedAt,
                   u.name AS UserName
            FROM workflow_approvals wa LEFT JOIN users u ON u.id = wa.user_id
            WHERE wa.workflow_step_id IN @Ids ORDER BY wa.decided_at", new { Ids = ids })).ToList();

        foreach (var step in steps)
        {
            step.Approvers = approvers.Where(a => a.WorkflowStepId == step.Id).ToList();
            step.Approvals = approvals.Where(a => a.WorkflowStepId == step.Id).ToList();
        }
        return steps;
    }

    public async Task RecordApprovalAsync(WorkflowApproval approval)
    {
        approval.Id = Guid.NewGuid();
        approval.CreatedAt = DateTime.UtcNow;
        approval.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO workflow_approvals (id, workflow_step_id, user_id, decision, comments, decided_at, ip_address, created_at, updated_at)
            VALUES (@Id, @WorkflowStepId, @UserId, @Decision, @Comments, @DecidedAt, @IpAddress, @CreatedAt, @UpdatedAt)";
        await _context.Connection.ExecuteAsync(sql, approval);
    }

    public async Task AdvanceStepAsync(Guid workflowId, int nextStep)
    {
        var sql = @"
            UPDATE workflow_steps
            SET is_current = CASE WHEN step_order = @NextStep THEN 1 ELSE 0 END,
                status = CASE WHEN step_order < @NextStep THEN 'completed' WHEN step_order = @NextStep THEN 'in_progress' ELSE status END,
                started_at = CASE WHEN step_order = @NextStep THEN @Now ELSE started_at END,
                updated_at = @Now
            WHERE workflow_id = @WorkflowId AND deleted_at IS NULL";
        await _context.Connection.ExecuteAsync(sql, new { WorkflowId = workflowId, NextStep = nextStep, Now = DateTime.UtcNow });

        await _context.Connection.ExecuteAsync(
            "UPDATE workflows SET current_step = @NextStep, status = 'in_progress', updated_at = @Now WHERE id = @WorkflowId",
            new { WorkflowId = workflowId, NextStep = nextStep, Now = DateTime.UtcNow });
    }

    public async Task CompleteWorkflowAsync(Guid workflowId, string finalStatus)
    {
        var sql = @"UPDATE workflows SET status = @Status, completed_at = @Now, updated_at = @Now WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = workflowId, Status = finalStatus, Now = DateTime.UtcNow });
    }

    public async Task CancelWorkflowAsync(Guid workflowId, Guid cancelledBy, string reason)
    {
        var sql = @"UPDATE workflows SET status = 'cancelled', cancelled_at = @Now, cancelled_by = @CancelledBy,
                    cancellation_reason = @Reason, updated_at = @Now WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = workflowId, CancelledBy = cancelledBy, Reason = reason, Now = DateTime.UtcNow });
    }

    public async Task UpdateStepStatusAsync(Guid stepId, string status, Guid? completedBy = null)
    {
        var sql = @"UPDATE workflow_steps
                    SET status = @Status,
                        completed_at = CASE WHEN @Status = 'completed' THEN @Now ELSE completed_at END,
                        completed_by = CASE WHEN @Status = 'completed' THEN @CompletedBy ELSE completed_by END,
                        updated_at = @Now
                    WHERE id = @Id AND deleted_at IS NULL";
        await _context.Connection.ExecuteAsync(sql, new { Id = stepId, Status = status, CompletedBy = completedBy, Now = DateTime.UtcNow });
    }
}
