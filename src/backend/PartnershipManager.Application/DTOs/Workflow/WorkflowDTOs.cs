namespace PartnershipManager.Application.DTOs.Workflow;

public class CreateWorkflowRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string Priority { get; set; } = "medium";
    public DateTime? DueDate { get; set; }
    public List<CreateWorkflowStepRequest> Steps { get; set; } = new();
}

public class CreateWorkflowStepRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "approval";
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedGroupId { get; set; }
    /// <summary>"any" ou "all". Quando nulo, usa o modo padrão do grupo.</summary>
    public string? ApprovalMode { get; set; }
    public DateTime? DueDate { get; set; }
}

public class WorkflowDecisionRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class WorkflowResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public Guid RequestedBy { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string WorkflowTypeLabel { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowStepResponse> Steps { get; set; } = new();
}

public class WorkflowStepResponse
{
    public Guid Id { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedGroupId { get; set; }
    public string? AssignedGroupName { get; set; }
    public string? ApprovalMode { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public List<StepApproverResponse> Approvers { get; set; } = new();
    public List<StepApprovalResponse> Approvals { get; set; } = new();
}

public class StepApproverResponse
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class StepApprovalResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
}
