namespace PartnershipManager.Domain.Enums;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Pending = 3,
    Blocked = 4
}

public enum ClientStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public enum DocumentType
{
    Cpf = 1,
    Cnpj = 2
}

public enum CompanyStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public enum LegalForm
{
    LTDA = 1,
    SA = 2,
    EIRELI = 3,
    MEI = 4,
    SLU = 5,
    Other = 99
}

public enum Language
{
    Portuguese = 1,
    English = 2,
    Spanish = 3
}

public enum Role
{
    SuperAdmin = 1,
    Admin = 2,
    Founder = 3,
    BoardMember = 4,
    Legal = 5,
    Finance = 6,
    HR = 7,
    Employee = 8,
    Investor = 9,
    Viewer = 10
}

public enum ShareholderType
{
    Founder = 1,
    Investor = 2,
    Employee = 3,
    Advisor = 4,
    ESOP = 5,
    Partner = 6,
    Other = 99
}

public enum ShareholderStatus
{
    Active = 1,
    Inactive = 2,
    Pending = 3,
    Exited = 4
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3,
    NotInformed = 4
}

public enum MaritalStatus
{
    Single = 1,
    Married = 2,
    StableUnion = 3,
    Divorced = 4,
    Widowed = 5
}

public enum WorkflowStatus
{
    Pending = 1,
    InProgress = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum VestingGrantStatus
{
    Active = 1,
    Completed = 2,
    Terminated = 3,
    Forfeited = 4
}

public enum ValuationStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Superseded = 4
}

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Login = 4,
    Logout = 5,
    PasswordChange = 6,
    StatusChange = 7
}

public enum ShareClassStatus
{
    Active = 1,
    Inactive = 2
}

public enum AntiDilutionType
{
    None = 0,
    FullRatchet = 1,
    WeightedAverage = 2
}

public enum ShareOrigin
{
    Issue = 1,
    Transfer = 2,
    Conversion = 3,
    Grant = 4
}

public enum ShareStatus
{
    Active = 1,
    Cancelled = 2,
    Converted = 3,
    Transferred = 4
}

public enum TransactionType
{
    Issue = 1,
    Transfer = 2,
    Cancel = 3,
    Convert = 4,
    Split = 5,
    ReverseSplit = 6
}

// ─── Vesting Module ───────────────────────────────────────────────────────────

public enum VestingType
{
    TimeBasedLinear = 1,
    TimeBasedCliff = 2,
    MilestoneBasedOnly = 3,
    HybridTimeMilestone = 4
}

public enum VestingPlanStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4
}

/// <summary>
/// Detailed status for individual grants (separate from VestingGrantStatus which
/// tracks the overall lifecycle Active/Completed/Terminated/Forfeited).
/// </summary>
public enum VestingGrantDetailStatus
{
    Pending = 1,
    Approved = 2,
    Active = 3,
    Exercised = 4,
    Expired = 5,
    Cancelled = 6
}

public enum VestingScheduleStatus
{
    Pending = 1,
    Vested = 2,
    Skipped = 3
}

public enum MilestoneStatus
{
    Pending = 1,
    InProgress = 2,
    Achieved = 3,
    Failed = 4,
    Cancelled = 5
}

public enum MilestoneType
{
    Financial = 1,
    Product = 2,
    Operational = 3,
    Custom = 4
}

// ─── Grant Milestones Module ──────────────────────────────────────────────────

/// <summary>Category for performance milestones (5-category model).</summary>
public enum MilestoneCategory
{
    Financial = 1,
    Operational = 2,
    Product = 3,
    Market = 4,
    Strategic = 5
}

/// <summary>Type of measurement metric for a milestone target.</summary>
public enum MetricType
{
    Revenue = 1,
    Profit = 2,
    Ebitda = 3,
    UserCount = 4,
    Mrr = 5,
    Arr = 6,
    CustomerCount = 7,
    Nps = 8,
    MarketShare = 9,
    ProductMilestone = 10,
    Custom = 11
}

/// <summary>Comparison operator used to evaluate whether the target is met.</summary>
public enum TargetOperator
{
    GreaterThan = 1,
    GreaterThanOrEqual = 2,
    LessThan = 3,
    LessThanOrEqual = 4,
    Equal = 5
}

/// <summary>How often progress is measured / recorded.</summary>
public enum MeasurementFrequency
{
    OneTime = 1,
    Monthly = 2,
    Quarterly = 3,
    Annual = 4
}

/// <summary>Mechanism used to accelerate the vesting schedule.</summary>
public enum VestingAccelerationType
{
    /// <summary>Accelerate by a percentage of the total vesting period.</summary>
    Percentage = 1,
    /// <summary>Accelerate by a fixed number of months.</summary>
    Months = 2,
    /// <summary>Immediately unlock a specific number of shares.</summary>
    Shares = 3
}

/// <summary>Source system that submitted a progress measurement.</summary>
public enum ProgressDataSource
{
    Manual = 1,
    ApiIntegration = 2,
    SystemCalculation = 3
}

public enum VestingTransactionType
{
    Exercise = 1,
    EarlyExercise = 2,
    AcceleratedExercise = 3
}
