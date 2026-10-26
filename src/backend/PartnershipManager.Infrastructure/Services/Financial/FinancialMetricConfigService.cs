using System.Text;
using System.Text.RegularExpressions;
using PartnershipManager.Application.Features.Financial.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Services.Financial;

public interface IFinancialMetricConfigService
{
    Task<IReadOnlyList<FinancialMetricDefinitionResponse>> GetDefinitionsAsync(Guid clientId, Guid companyId, bool includeInactive = false);
    Task<FinancialMetricDefinitionResponse> CreateDefinitionAsync(Guid clientId, CreateMetricDefinitionRequest request);
    Task<FinancialMetricDefinitionResponse> UpdateDefinitionAsync(Guid id, Guid clientId, UpdateMetricDefinitionRequest request);
    Task DeleteDefinitionAsync(Guid id, Guid clientId);
    Task<PeriodMetricValuesResponse> GetPeriodValuesAsync(Guid periodId, Guid clientId);
    Task<PeriodMetricValuesResponse> SavePeriodValuesAsync(Guid periodId, Guid clientId, SavePeriodMetricValuesRequest request, Guid userId);
    Task<FinancialMetricSeriesResponse> GetSeriesAsync(Guid clientId, Guid companyId, int year);
    Task<FinancialDashboardConfigResponse> GetDashboardConfigAsync(Guid clientId, Guid companyId);
    Task<FinancialDashboardConfigResponse> SaveDashboardConfigAsync(Guid clientId, SaveDashboardConfigRequest request);
}

public partial class FinancialMetricConfigService : IFinancialMetricConfigService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFinancialConfigRepository _config;

    public FinancialMetricConfigService(IUnitOfWork unitOfWork, IFinancialConfigRepository config)
    {
        _unitOfWork = unitOfWork;
        _config = config;
    }

    // Built-in metrics backed by the legacy financial_metrics columns.
    private record SystemMetric(string Key, string Label, string Category, string DataType, string? Unit, bool Calculated);

    private static readonly SystemMetric[] SystemMetrics =
    [
        new("gross_revenue", "Receita Bruta", "Receita", "currency", null, false),
        new("net_revenue", "Receita Líquida", "Receita", "currency", null, false),
        new("mrr", "MRR", "Receita", "currency", null, false),
        new("arr", "ARR", "Receita", "currency", null, true),
        new("cash_balance", "Saldo de Caixa", "Caixa & Burn", "currency", null, false),
        new("burn_rate", "Burn Rate", "Caixa & Burn", "currency", null, false),
        new("runway_months", "Runway", "Caixa & Burn", "number", "meses", true),
        new("customer_count", "Nº de Clientes", "Unit Economics", "integer", null, false),
        new("churn_rate", "Churn Rate", "Unit Economics", "percentage", null, false),
        new("cac", "CAC", "Unit Economics", "currency", null, false),
        new("ltv", "LTV", "Unit Economics", "currency", null, false),
        new("ltv_cac_ratio", "LTV / CAC", "Unit Economics", "number", "x", true),
        new("nps", "NPS", "Unit Economics", "integer", null, false),
        new("ebitda", "EBITDA", "Rentabilidade", "currency", null, false),
        new("ebitda_margin", "Margem EBITDA", "Rentabilidade", "percentage", null, true),
        new("net_income", "Lucro Líquido", "Rentabilidade", "currency", null, false),
    ];

    private static readonly DashboardWidgetDto[] DefaultWidgets =
    [
        new() { Title = "MRR do mês", WidgetType = "kpi", MetricKeys = ["mrr"], Scope = "monthly" },
        new() { Title = "ARR do mês", WidgetType = "kpi", MetricKeys = ["arr"], Scope = "monthly" },
        new() { Title = "Burn Rate do mês", WidgetType = "kpi", MetricKeys = ["burn_rate"], Scope = "monthly" },
        new() { Title = "Runway", WidgetType = "kpi", MetricKeys = ["runway_months"], Scope = "monthly" },
        new() { Title = "Receita líquida acumulada", WidgetType = "kpi", MetricKeys = ["net_revenue"], Scope = "annual", Aggregation = "sum" },
        new() { Title = "Burn médio do ano", WidgetType = "kpi", MetricKeys = ["burn_rate"], Scope = "annual", Aggregation = "avg" },
        new() { Title = "ARR atual", WidgetType = "kpi", MetricKeys = ["arr"], Scope = "annual", Aggregation = "last" },
        new() { Title = "MRR & ARR", WidgetType = "line", MetricKeys = ["mrr", "arr"] },
        new() { Title = "Burn Rate mensal", WidgetType = "bar", MetricKeys = ["burn_rate"] },
        new() { Title = "Clientes", WidgetType = "area", MetricKeys = ["customer_count"] },
        new() { Title = "CAC vs LTV", WidgetType = "bar", MetricKeys = ["cac", "ltv"] },
    ];

    // ─── Definitions ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialMetricDefinitionResponse>> GetDefinitionsAsync(
        Guid clientId, Guid companyId, bool includeInactive = false)
    {
        var defs = await LoadDefinitionsAsync(clientId, companyId, includeInactive);
        var withValues = await _config.GetDefinitionIdsWithValuesAsync(clientId, companyId);
        var widgets = await _config.GetWidgetsAsync(clientId, companyId);
        var usedIds = withValues.ToHashSet();
        var widgetKeys = widgets.SelectMany(w => w.MetricKeys).ToHashSet();
        return defs.Select(d => MapDefinition(d, usedIds.Contains(d.Id) || widgetKeys.Contains(d.MetricKey))).ToList();
    }

    public async Task<FinancialMetricDefinitionResponse> CreateDefinitionAsync(Guid clientId, CreateMetricDefinitionRequest request)
    {
        await EnsureCompanyAsync(clientId, request.CompanyId);
        var existing = await LoadDefinitionsAsync(clientId, request.CompanyId, includeInactive: true);

        var label = request.Label?.Trim() ?? string.Empty;
        if (label.Length is 0 or > 120)
            throw new ValidationException("label", "Nome da métrica é obrigatório (máx. 120 caracteres).");
        ValidateDataType(request.DataType);

        var key = string.IsNullOrWhiteSpace(request.Key) ? Slugify(label) : request.Key.Trim().ToLowerInvariant();
        if (!KeyRegex().IsMatch(key))
            throw new ValidationException("key", "Chave inválida. Use letras minúsculas, números e '_' (2–60 caracteres, iniciando com letra).");
        if (existing.Any(d => d.MetricKey == key))
            throw new DomainException($"Já existe uma métrica com a chave '{key}' nesta empresa.");

        var def = new FinancialMetricDefinition
        {
            ClientId = clientId,
            CompanyId = request.CompanyId,
            MetricKey = key,
            Label = label,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Geral" : request.Category.Trim(),
            DataType = request.DataType,
            Unit = NullIfEmpty(request.Unit),
            Description = NullIfEmpty(request.Description),
            DisplayOrder = existing.Count == 0 ? 0 : existing.Max(d => d.DisplayOrder) + 10
        };
        await _config.AddDefinitionAsync(def);
        return MapDefinition(def);
    }

    public async Task<FinancialMetricDefinitionResponse> UpdateDefinitionAsync(Guid id, Guid clientId, UpdateMetricDefinitionRequest request)
    {
        var def = await _config.GetDefinitionByIdAsync(id, clientId)
            ?? throw new NotFoundException("FinancialMetricDefinition", id);

        var label = request.Label?.Trim() ?? string.Empty;
        if (label.Length is 0 or > 120)
            throw new ValidationException("label", "Nome da métrica é obrigatório (máx. 120 caracteres).");

        def.Label = label;
        if (!string.IsNullOrWhiteSpace(request.Category)) def.Category = request.Category.Trim();
        def.Unit = NullIfEmpty(request.Unit);
        def.Description = NullIfEmpty(request.Description);
        if (request.DisplayOrder.HasValue) def.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsActive.HasValue) def.IsActive = request.IsActive.Value;

        if (!def.IsSystem && !string.IsNullOrWhiteSpace(request.DataType) && request.DataType != def.DataType)
        {
            ValidateDataType(request.DataType);
            def.DataType = request.DataType;
        }

        await _config.UpdateDefinitionAsync(def);
        return MapDefinition(def);
    }

    public async Task DeleteDefinitionAsync(Guid id, Guid clientId)
    {
        var def = await _config.GetDefinitionByIdAsync(id, clientId)
            ?? throw new NotFoundException("FinancialMetricDefinition", id);

        if (def.IsSystem)
        {
            // Built-in metrics are only hidden, since their data lives in the legacy columns.
            def.IsActive = false;
            await _config.UpdateDefinitionAsync(def);
            return;
        }

        var withValues = await _config.GetDefinitionIdsWithValuesAsync(clientId, def.CompanyId);
        var widgets = await _config.GetWidgetsAsync(clientId, def.CompanyId);
        if (withValues.Contains(def.Id) || widgets.Any(w => w.MetricKeys.Contains(def.MetricKey)))
            throw new DomainException("Esta métrica já foi utilizada e não pode ser excluída. Você pode ocultá-la.");

        await _config.DeleteDefinitionAsync(id, clientId);
    }

    // ─── Period values ───────────────────────────────────────────────────────

    public async Task<PeriodMetricValuesResponse> GetPeriodValuesAsync(Guid periodId, Guid clientId)
    {
        var period = await _unitOfWork.FinancialPeriods.GetByIdAsync(periodId, clientId)
            ?? throw new NotFoundException("FinancialPeriod", periodId);

        var defs = await LoadDefinitionsAsync(clientId, period.CompanyId, includeInactive: true);
        var metric = await _unitOfWork.FinancialMetrics.GetByPeriodAsync(periodId, clientId);
        var stored = await _config.GetValuesByPeriodAsync(periodId, clientId);

        return new PeriodMetricValuesResponse
        {
            PeriodId = periodId,
            Values = BuildValues(defs, metric, stored.Where(v => v.PeriodId == periodId))
        };
    }

    public async Task<PeriodMetricValuesResponse> SavePeriodValuesAsync(
        Guid periodId, Guid clientId, SavePeriodMetricValuesRequest request, Guid userId)
    {
        var period = await _unitOfWork.FinancialPeriods.GetByIdAsync(periodId, clientId)
            ?? throw new NotFoundException("FinancialPeriod", periodId);
        if (!period.CanBeEdited)
            throw new DomainException($"Período {period.PeriodLabel} está bloqueado e não pode ser editado (FI-03).");

        var defs = await LoadDefinitionsAsync(clientId, period.CompanyId, includeInactive: true);
        var byKey = defs.ToDictionary(d => d.MetricKey);

        foreach (var (key, value) in request.Values)
        {
            if (!byKey.TryGetValue(key, out var def))
                throw new ValidationException(key, $"Métrica '{key}' não existe para esta empresa.");
            if (def.IsCalculated)
                throw new ValidationException(key, $"Métrica '{def.Label}' é calculada automaticamente.");
            if (value.HasValue && def.DataType == FinancialMetricDataTypes.Integer && value.Value != decimal.Truncate(value.Value))
                throw new ValidationException(key, $"'{def.Label}' deve ser um número inteiro.");
        }

        var metric = await _unitOfWork.FinancialMetrics.GetByPeriodAsync(periodId, clientId);
        var systemInput = request.Values.Where(kv => byKey[kv.Key].IsSystem).ToDictionary(kv => kv.Key, kv => kv.Value);
        if (systemInput.Count > 0)
        {
            var isNew = metric is null;
            metric ??= FinancialMetric.Create(clientId, periodId, userId);
            ApplySystemValues(metric, systemInput, userId);
            if (isNew) await _unitOfWork.FinancialMetrics.AddAsync(metric);
            else await _unitOfWork.FinancialMetrics.UpdateAsync(metric);
        }

        foreach (var (key, value) in request.Values.Where(kv => !byKey[kv.Key].IsSystem))
            await _config.UpsertValueAsync(clientId, periodId, byKey[key].Id, value);

        var stored = await _config.GetValuesByPeriodAsync(periodId, clientId);
        return new PeriodMetricValuesResponse
        {
            PeriodId = periodId,
            Values = BuildValues(defs, metric, stored)
        };
    }

    public async Task<FinancialMetricSeriesResponse> GetSeriesAsync(Guid clientId, Guid companyId, int year)
    {
        var defs = await LoadDefinitionsAsync(clientId, companyId, includeInactive: true);
        var periods = (await _unitOfWork.FinancialPeriods.GetByYearAsync(clientId, companyId, (short)year))
            .OrderBy(p => p.Month).ToList();
        var stored = await _config.GetValuesByPeriodsAsync(periods.Select(p => p.Id), clientId);

        var points = new List<MetricSeriesPoint>();
        foreach (var p in periods)
        {
            var metric = await _unitOfWork.FinancialMetrics.GetByPeriodAsync(p.Id, clientId);
            points.Add(new MetricSeriesPoint
            {
                PeriodId = p.Id,
                Year = p.Year,
                Month = p.Month,
                Status = p.Status,
                Values = BuildValues(defs, metric, stored.Where(v => v.PeriodId == p.Id))
            });
        }

        return new FinancialMetricSeriesResponse { CompanyId = companyId, Year = year, Points = points };
    }

    // ─── Dashboard configuration ─────────────────────────────────────────────

    public async Task<FinancialDashboardConfigResponse> GetDashboardConfigAsync(Guid clientId, Guid companyId)
    {
        var defs = await LoadDefinitionsAsync(clientId, companyId, includeInactive: true);
        var widgets = await _config.GetWidgetsAsync(clientId, companyId);

        if (widgets.Count == 0)
        {
            widgets = DefaultWidgets.Select((w, i) => new FinancialDashboardWidget
            {
                ClientId = clientId,
                CompanyId = companyId,
                Title = w.Title,
                WidgetType = w.WidgetType,
                MetricKeys = [.. w.MetricKeys],
                Size = w.Size,
                Scope = w.Scope,
                Aggregation = w.Aggregation,
                DisplayOrder = i * 10
            }).ToList();
            await _config.ReplaceWidgetsAsync(clientId, companyId, widgets);
        }

        return BuildConfigResponse(companyId, defs, widgets);
    }

    public async Task<FinancialDashboardConfigResponse> SaveDashboardConfigAsync(Guid clientId, SaveDashboardConfigRequest request)
    {
        var defs = await LoadDefinitionsAsync(clientId, request.CompanyId, includeInactive: true);
        var keys = defs.Select(d => d.MetricKey).ToHashSet();

        var widgets = new List<FinancialDashboardWidget>();
        for (var i = 0; i < request.Widgets.Count; i++)
        {
            var w = request.Widgets[i];
            var title = w.Title?.Trim() ?? string.Empty;
            if (title.Length is 0 or > 120)
                throw new ValidationException("title", "Título do widget é obrigatório (máx. 120 caracteres).");
            if (!FinancialWidgetTypes.All.Contains(w.WidgetType))
                throw new ValidationException("widgetType", $"Tipo de widget inválido: '{w.WidgetType}'.");
            if (w.MetricKeys.Count == 0)
                throw new ValidationException("metricKeys", $"Widget '{title}' precisa de ao menos uma métrica.");
            if (w.WidgetType == FinancialWidgetTypes.Kpi && w.MetricKeys.Count != 1)
                throw new ValidationException("metricKeys", $"Widget KPI '{title}' deve ter exatamente uma métrica.");
            if (!FinancialWidgetScopes.All.Contains(w.Scope))
                throw new ValidationException("scope", $"Escopo de widget inválido: '{w.Scope}'.");
            if (w.Scope == FinancialWidgetScopes.Monthly && w.WidgetType != FinancialWidgetTypes.Kpi)
                throw new ValidationException("scope", $"Widget '{title}': gráficos são sempre anuais.");
            if (!FinancialWidgetAggregations.All.Contains(w.Aggregation))
                throw new ValidationException("aggregation", $"Agregação inválida: '{w.Aggregation}'.");
            var unknown = w.MetricKeys.FirstOrDefault(k => !keys.Contains(k));
            if (unknown != null)
                throw new ValidationException("metricKeys", $"Métrica '{unknown}' não existe para esta empresa.");

            widgets.Add(new FinancialDashboardWidget
            {
                Id = w.Id ?? Guid.NewGuid(),
                ClientId = clientId,
                CompanyId = request.CompanyId,
                Title = title,
                WidgetType = w.WidgetType,
                MetricKeys = w.MetricKeys.Distinct().ToList(),
                Size = w.Size == "large" ? "large" : "small",
                Scope = w.Scope,
                Aggregation = w.Aggregation,
                DisplayOrder = i * 10
            });
        }

        await _config.ReplaceWidgetsAsync(clientId, request.CompanyId, widgets);
        return BuildConfigResponse(request.CompanyId, defs, widgets);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task EnsureCompanyAsync(Guid clientId, Guid companyId)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
        if (company is null || company.ClientId != clientId)
            throw new NotFoundException("Company", companyId);
    }

    /// <summary>Loads definitions, creating the built-in set the first time a company is used.</summary>
    private async Task<IReadOnlyList<FinancialMetricDefinition>> LoadDefinitionsAsync(Guid clientId, Guid companyId, bool includeInactive)
    {
        var all = await _config.GetDefinitionsAsync(clientId, companyId, includeInactive: true);
        if (all.Count == 0)
        {
            await EnsureCompanyAsync(clientId, companyId);
            for (var i = 0; i < SystemMetrics.Length; i++)
            {
                var s = SystemMetrics[i];
                await _config.AddDefinitionAsync(new FinancialMetricDefinition
                {
                    ClientId = clientId,
                    CompanyId = companyId,
                    MetricKey = s.Key,
                    Label = s.Label,
                    Category = s.Category,
                    DataType = s.DataType,
                    Unit = s.Unit,
                    DisplayOrder = i * 10,
                    IsSystem = true,
                    IsCalculated = s.Calculated
                });
            }
            all = await _config.GetDefinitionsAsync(clientId, companyId, includeInactive: true);
        }

        return includeInactive ? all : all.Where(d => d.IsActive).ToList();
    }

    private static Dictionary<string, decimal?> BuildValues(
        IEnumerable<FinancialMetricDefinition> defs, FinancialMetric? metric, IEnumerable<FinancialMetricValue> stored)
    {
        var storedById = stored.ToDictionary(v => v.DefinitionId, v => v.Value);
        var result = new Dictionary<string, decimal?>();
        foreach (var d in defs)
        {
            result[d.MetricKey] = d.IsSystem
                ? ReadSystemValue(metric, d.MetricKey)
                : storedById.GetValueOrDefault(d.Id);
        }
        return result;
    }

    private static decimal? ReadSystemValue(FinancialMetric? m, string key) => m is null ? null : key switch
    {
        "gross_revenue" => m.GrossRevenue,
        "net_revenue" => m.NetRevenue,
        "mrr" => m.Mrr,
        "arr" => m.Arr,
        "cash_balance" => m.CashBalance,
        "burn_rate" => m.BurnRate,
        "runway_months" => m.RunwayMonths,
        "customer_count" => m.CustomerCount,
        "churn_rate" => m.ChurnRate,
        "cac" => m.Cac,
        "ltv" => m.Ltv,
        "ltv_cac_ratio" => m.LtvToCacRatio,
        "nps" => m.Nps,
        "ebitda" => m.Ebitda,
        "ebitda_margin" => m.EbitdaMargin,
        "net_income" => m.NetIncome,
        _ => null
    };

    private static void ApplySystemValues(FinancialMetric m, Dictionary<string, decimal?> input, Guid userId)
    {
        decimal? Pick(string key, decimal? current) => input.TryGetValue(key, out var v) ? v : current;

        if (input.Keys.Any(k => k is "gross_revenue" or "net_revenue" or "mrr"))
            m.UpdateRevenue(Pick("gross_revenue", m.GrossRevenue), Pick("net_revenue", m.NetRevenue), Pick("mrr", m.Mrr), userId);

        if (input.Keys.Any(k => k is "cash_balance" or "burn_rate"))
            m.UpdateCashBurn(Pick("cash_balance", m.CashBalance), Pick("burn_rate", m.BurnRate), userId);

        if (input.Keys.Any(k => k is "customer_count" or "churn_rate" or "cac" or "ltv" or "nps"))
            m.UpdateUnitEconomics(
                (int?)Pick("customer_count", m.CustomerCount),
                Pick("churn_rate", m.ChurnRate),
                Pick("cac", m.Cac),
                Pick("ltv", m.Ltv),
                (short?)Pick("nps", m.Nps),
                userId);

        if (input.Keys.Any(k => k is "ebitda" or "net_income"))
            m.UpdateProfitability(Pick("ebitda", m.Ebitda), Pick("net_income", m.NetIncome), userId);
    }

    private static FinancialDashboardConfigResponse BuildConfigResponse(
        Guid companyId, IReadOnlyList<FinancialMetricDefinition> defs, IReadOnlyList<FinancialDashboardWidget> widgets) =>
        new()
        {
            CompanyId = companyId,
            Definitions = defs.Select(MapDefinition).ToList(),
            Widgets = widgets.Select(w => new DashboardWidgetDto
            {
                Id = w.Id,
                Title = w.Title,
                WidgetType = w.WidgetType,
                MetricKeys = w.MetricKeys,
                Size = w.Size,
                Scope = w.Scope,
                Aggregation = w.Aggregation,
                DisplayOrder = w.DisplayOrder
            }).ToList()
        };

    private static FinancialMetricDefinitionResponse MapDefinition(FinancialMetricDefinition d) => MapDefinition(d, false);

    private static FinancialMetricDefinitionResponse MapDefinition(FinancialMetricDefinition d, bool inUse) => new()
    {
        InUse = inUse,
        Id = d.Id,
        CompanyId = d.CompanyId,
        Key = d.MetricKey,
        Label = d.Label,
        Category = d.Category,
        DataType = d.DataType,
        Unit = d.Unit,
        Description = d.Description,
        DisplayOrder = d.DisplayOrder,
        IsSystem = d.IsSystem,
        IsCalculated = d.IsCalculated,
        IsActive = d.IsActive
    };

    private static void ValidateDataType(string dataType)
    {
        if (!FinancialMetricDataTypes.All.Contains(dataType))
            throw new ValidationException("dataType", $"Tipo de dado inválido: '{dataType}'.");
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Slugify(string label)
    {
        var normalized = label.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToLowerInvariant(c) : '_');
        }
        var slug = Regex.Replace(sb.ToString(), "_+", "_").Trim('_');
        if (slug.Length > 0 && !char.IsLetter(slug[0])) slug = "m_" + slug;
        return slug.Length > 60 ? slug[..60].TrimEnd('_') : slug;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{1,59}$")]
    private static partial Regex KeyRegex();
}
