namespace PartnershipManager.Domain.Entities;

public static class FinancialMetricDataTypes
{
    public const string Currency = "currency";
    public const string Number = "number";
    public const string Integer = "integer";
    public const string Percentage = "percentage";

    public static readonly string[] All = [Currency, Number, Integer, Percentage];
}

public static class FinancialWidgetTypes
{
    public const string Kpi = "kpi";
    public const string Line = "line";
    public const string Bar = "bar";
    public const string Area = "area";

    public static readonly string[] All = [Kpi, Line, Bar, Area];
}

public static class FinancialWidgetScopes
{
    public const string Annual = "annual";
    public const string Monthly = "monthly";

    public static readonly string[] All = [Annual, Monthly];
}

public static class FinancialWidgetAggregations
{
    public const string Last = "last";
    public const string Sum = "sum";
    public const string Avg = "avg";

    public static readonly string[] All = [Last, Sum, Avg];
}

/// <summary>
/// Company-specific financial metric definition. System definitions map to the
/// legacy fixed columns of financial_metrics; custom ones store values in financial_metric_values.
/// </summary>
public class FinancialMetricDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Guid CompanyId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = "Geral";
    public string DataType { get; set; } = FinancialMetricDataTypes.Number;
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsCalculated { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FinancialMetricValue
{
    public Guid PeriodId { get; set; }
    public Guid DefinitionId { get; set; }
    public decimal? Value { get; set; }
}

public class FinancialDashboardWidget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WidgetType { get; set; } = FinancialWidgetTypes.Kpi;
    public List<string> MetricKeys { get; set; } = [];
    public string Size { get; set; } = "small";
    /// <summary>annual | monthly (competência). Charts are always annual.</summary>
    public string Scope { get; set; } = FinancialWidgetScopes.Annual;
    /// <summary>How an annual KPI consolidates the year: last | sum | avg.</summary>
    public string Aggregation { get; set; } = FinancialWidgetAggregations.Last;
    public int DisplayOrder { get; set; }
}
