namespace PartnershipManager.Application.DTOs.Workflow;

public class SaveApprovalGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>"any" = basta um membro aprovar; "all" = todos devem aprovar.</summary>
    public string ApprovalMode { get; set; } = "any";
    public bool IsActive { get; set; } = true;
    public List<Guid> MemberIds { get; set; } = new();
}

public class ApprovalGroupMemberResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class ApprovalGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ApprovalMode { get; set; } = "any";
    public bool IsActive { get; set; }
    public List<ApprovalGroupMemberResponse> Members { get; set; } = new();
}

public class SaveWorkflowTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public bool IsActive { get; set; } = true;
    public List<WorkflowTemplateStepDto> Steps { get; set; } = new();
}

public class WorkflowTemplateStepDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StepType { get; set; } = "approval";
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedGroupId { get; set; }
    public string? AssignedGroupName { get; set; }
    public string? ApprovalMode { get; set; }
    /// <summary>none | calendar_days | business_days</summary>
    public string DueMode { get; set; } = "none";
    public int DueDays { get; set; }
}

public class WorkflowTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public bool IsActive { get; set; }
    public List<WorkflowTemplateStepDto> Steps { get; set; } = new();
}
