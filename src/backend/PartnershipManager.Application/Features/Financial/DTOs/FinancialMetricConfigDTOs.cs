namespace PartnershipManager.Application.Features.Financial.DTOs;

// ────────────────────────────────────────────────────────────────────────────
// CONFIGURABLE METRICS
// ────────────────────────────────────────────────────────────────────────────

public record FinancialMetricDefinitionResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsSystem { get; init; }
    public bool IsCalculated { get; init; }
    public bool IsActive { get; init; }
}

public record CreateMetricDefinitionRequest
{
    public Guid CompanyId { get; init; }
    public string? Key { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string DataType { get; init; } = "number";
    public string? Unit { get; init; }
    public string? Description { get; init; }
}

public record UpdateMetricDefinitionRequest
{
    public string Label { get; init; } = string.Empty;
    public string? Category { get; init; }
    /// <summary>Ignored for system metrics.</summary>
    public string? DataType { get; init; }
    public string? Unit { get; init; }
    public string? Description { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsActive { get; init; }
}

/// <summary>Values of all metrics of a period keyed by metric key.</summary>
public record PeriodMetricValuesResponse
{
    public Guid PeriodId { get; init; }
    public Dictionary<string, decimal?> Values { get; init; } = new();
}

public record SavePeriodMetricValuesRequest
{
    public Dictionary<string, decimal?> Values { get; init; } = new();
}

public record MetricSeriesPoint
{
    public Guid PeriodId { get; init; }
    public short Year { get; init; }
    public byte Month { get; init; }
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, decimal?> Values { get; init; } = new();
}

// ────────────────────────────────────────────────────────────────────────────
// DASHBOARD CONFIGURATION
// ────────────────────────────────────────────────────────────────────────────

public record DashboardWidgetDto
{
    public Guid? Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string WidgetType { get; init; } = "kpi";
    public List<string> MetricKeys { get; init; } = [];
    public string Size { get; init; } = "small";
    public string Scope { get; init; } = "annual";
    public string Aggregation { get; init; } = "last";
    public int DisplayOrder { get; init; }
}

public record SaveDashboardConfigRequest
{
    public Guid CompanyId { get; init; }
    public List<DashboardWidgetDto> Widgets { get; init; } = [];
}

public record FinancialDashboardConfigResponse
{
    public Guid CompanyId { get; init; }
    public IReadOnlyList<FinancialMetricDefinitionResponse> Definitions { get; init; } = [];
    public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
}

public record FinancialMetricSeriesResponse
{
    public Guid CompanyId { get; init; }
    public int Year { get; init; }
    public IReadOnlyList<MetricSeriesPoint> Points { get; init; } = [];
}
