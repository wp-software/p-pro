namespace PartnershipManager.Domain.Entities;

public class Workflow : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "pending";
    public string Priority { get; set; } = "medium";
    public int CurrentStep { get; set; } = 1;
    public int TotalSteps { get; set; } = 1;
    public Guid RequestedBy { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? Metadata { get; set; }
    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
}

public class WorkflowStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "approval";
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedGroupId { get; set; }
    public string? AssignedGroupName { get; set; }
    public string? ApprovalMode { get; set; }
    public string Status { get; set; } = "pending";
    public bool IsCurrent { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public string? Notes { get; set; }
    public ICollection<WorkflowApproval> Approvals { get; set; } = new List<WorkflowApproval>();
    public ICollection<WorkflowStepApprover> Approvers { get; set; } = new List<WorkflowStepApprover>();
}

/// <summary>Membro elegível (snapshot do grupo) para decidir uma etapa.</summary>
public class WorkflowStepApprover
{
    public Guid WorkflowStepId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
}

public class WorkflowApproval : BaseEntity
{
    public Guid WorkflowStepId { get; set; }
    public Guid UserId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserName { get; set; }
}

public static class WorkflowStatuses
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
}

public static class WorkflowTypes
{
    public const string ContractApproval = "contract_approval";
    public const string ShareholderChange = "shareholder_change";
    public const string CommunicationApproval = "communication_approval";
    public const string DocumentVerification = "document_verification";
    public const string VestingApproval = "vesting_approval";
}

public static class WorkflowStepStatuses
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Skipped = "skipped";
}

public static class WorkflowDecisions
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string RequestedChanges = "requested_changes";
}

public static class ApprovalModes
{
    public const string Any = "any";
    public const string All = "all";

    public static bool IsValid(string? mode) => mode is Any or All;
}

public class ApprovalGroup : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ApprovalMode { get; set; } = ApprovalModes.Any;
    public bool IsActive { get; set; } = true;
    public ICollection<ApprovalGroupMember> Members { get; set; } = new List<ApprovalGroupMember>();
}

public class ApprovalGroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}

public class WorkflowTemplate : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public bool IsActive { get; set; } = true;
    public ICollection<WorkflowTemplateStep> Steps { get; set; } = new List<WorkflowTemplateStep>();
}

public class WorkflowTemplateStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "approval";
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedGroupId { get; set; }
    public string? AssignedGroupName { get; set; }
    public string? ApprovalMode { get; set; }
    public string DueMode { get; set; } = "none";
    public int DueDays { get; set; }
}
