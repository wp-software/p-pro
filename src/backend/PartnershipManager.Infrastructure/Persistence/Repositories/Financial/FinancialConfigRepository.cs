using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

public class FinancialConfigRepository : IFinancialConfigRepository
{
    private readonly DapperContext _context;

    public FinancialConfigRepository(DapperContext context)
    {
        _context = context;
    }

    private const string DefinitionColumns = @"
        id, client_id, company_id, metric_key, label, category, data_type, unit,
        description, display_order, is_system, is_calculated, is_active";

    // ─── Definitions ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialMetricDefinition>> GetDefinitionsAsync(
        Guid clientId, Guid companyId, bool includeInactive)
    {
        var sql = $@"
            SELECT {DefinitionColumns}
            FROM financial_metric_definitions
            WHERE client_id = @ClientId AND company_id = @CompanyId
              {(includeInactive ? "" : "AND is_active = 1")}
            ORDER BY display_order, label";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, _context.Transaction);

        return rows.Select(MapDefinition).ToList();
    }

    public async Task<FinancialMetricDefinition?> GetDefinitionByIdAsync(Guid id, Guid clientId)
    {
        var sql = $@"
            SELECT {DefinitionColumns}
            FROM financial_metric_definitions
            WHERE id = @Id AND client_id = @ClientId";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, _context.Transaction);

        return row is null ? null : MapDefinition(row);
    }

    public async Task AddDefinitionAsync(FinancialMetricDefinition d)
    {
        const string sql = @"
            INSERT INTO financial_metric_definitions (
                id, client_id, company_id, metric_key, label, category, data_type, unit,
                description, display_order, is_system, is_calculated, is_active
            ) VALUES (
                @Id, @ClientId, @CompanyId, @MetricKey, @Label, @Category, @DataType, @Unit,
                @Description, @DisplayOrder, @IsSystem, @IsCalculated, @IsActive
            )";

        await _context.Connection.ExecuteAsync(sql, DefinitionParams(d), _context.Transaction);
    }

    public async Task UpdateDefinitionAsync(FinancialMetricDefinition d)
    {
        const string sql = @"
            UPDATE financial_metric_definitions SET
                label = @Label, category = @Category, data_type = @DataType, unit = @Unit,
                description = @Description, display_order = @DisplayOrder, is_active = @IsActive
            WHERE id = @Id AND client_id = @ClientId";

        await _context.Connection.ExecuteAsync(sql, DefinitionParams(d), _context.Transaction);
    }

    public async Task DeleteDefinitionAsync(Guid id, Guid clientId)
    {
        var p = new { Id = id.ToString(), ClientId = clientId.ToString() };
        await _context.Connection.ExecuteAsync(
            "DELETE FROM financial_metric_values WHERE definition_id = @Id AND client_id = @ClientId",
            p, _context.Transaction);
        await _context.Connection.ExecuteAsync(
            "DELETE FROM financial_metric_definitions WHERE id = @Id AND client_id = @ClientId AND is_system = 0",
            p, _context.Transaction);
    }

    public async Task<IReadOnlyCollection<Guid>> GetDefinitionIdsWithValuesAsync(Guid clientId, Guid companyId)
    {
        const string sql = @"
            SELECT DISTINCT v.definition_id
            FROM financial_metric_values v
            JOIN financial_metric_definitions d ON d.id = v.definition_id
            WHERE v.client_id = @ClientId AND d.company_id = @CompanyId AND v.value IS NOT NULL";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, _context.Transaction);

        return rows.Select(r => ParseGuid((object?)r.definition_id)).ToList();
    }

    // ─── Values ──────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<FinancialMetricValue>> GetValuesByPeriodAsync(Guid periodId, Guid clientId)
        => GetValuesByPeriodsAsync([periodId], clientId);

    public async Task<IReadOnlyList<FinancialMetricValue>> GetValuesByPeriodsAsync(
        IEnumerable<Guid> periodIds, Guid clientId)
    {
        var ids = periodIds.Select(x => x.ToString()).ToArray();
        if (ids.Length == 0) return [];

        const string sql = @"
            SELECT period_id, definition_id, value
            FROM financial_metric_values
            WHERE client_id = @ClientId AND period_id IN @PeriodIds";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            PeriodIds = ids
        }, _context.Transaction);

        return rows.Select(r => new FinancialMetricValue
        {
            PeriodId = ParseGuid(r.period_id),
            DefinitionId = ParseGuid(r.definition_id),
            Value = (decimal?)r.value
        }).ToList();
    }

    public async Task UpsertValueAsync(Guid clientId, Guid periodId, Guid definitionId, decimal? value)
    {
        const string sql = @"
            INSERT INTO financial_metric_values (id, client_id, period_id, definition_id, value)
            VALUES (@Id, @ClientId, @PeriodId, @DefinitionId, @Value)
            ON DUPLICATE KEY UPDATE value = VALUES(value), updated_at = CURRENT_TIMESTAMP(6)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = clientId.ToString(),
            PeriodId = periodId.ToString(),
            DefinitionId = definitionId.ToString(),
            Value = value
        }, _context.Transaction);
    }

    // ─── Widgets ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialDashboardWidget>> GetWidgetsAsync(Guid clientId, Guid companyId)
    {
        const string sql = @"
            SELECT id, client_id, company_id, title, widget_type, metric_keys, size, scope, aggregation, display_order
            FROM financial_dashboard_widgets
            WHERE client_id = @ClientId AND company_id = @CompanyId
            ORDER BY display_order, created_at";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, _context.Transaction);

        return rows.Select(r => new FinancialDashboardWidget
        {
            Id = ParseGuid(r.id),
            ClientId = ParseGuid(r.client_id),
            CompanyId = ParseGuid(r.company_id),
            Title = (string)r.title,
            WidgetType = (string)r.widget_type,
            MetricKeys = ((string)r.metric_keys).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Size = (string)r.size,
            Scope = (string)r.scope,
            Aggregation = (string)r.aggregation,
            DisplayOrder = (int)r.display_order
        }).ToList();
    }

    public async Task ReplaceWidgetsAsync(Guid clientId, Guid companyId, IEnumerable<FinancialDashboardWidget> widgets)
    {
        await _context.Connection.ExecuteAsync(
            "DELETE FROM financial_dashboard_widgets WHERE client_id = @ClientId AND company_id = @CompanyId",
            new { ClientId = clientId.ToString(), CompanyId = companyId.ToString() }, _context.Transaction);

        const string sql = @"
            INSERT INTO financial_dashboard_widgets
                (id, client_id, company_id, title, widget_type, metric_keys, size, scope, aggregation, display_order)
            VALUES (@Id, @ClientId, @CompanyId, @Title, @WidgetType, @MetricKeys, @Size, @Scope, @Aggregation, @DisplayOrder)";

        foreach (var w in widgets)
        {
            await _context.Connection.ExecuteAsync(sql, new
            {
                Id = w.Id.ToString(),
                ClientId = clientId.ToString(),
                CompanyId = companyId.ToString(),
                w.Title,
                w.WidgetType,
                MetricKeys = string.Join(',', w.MetricKeys),
                w.Size,
                w.Scope,
                w.Aggregation,
                w.DisplayOrder
            }, _context.Transaction);
        }
    }

    // ─── Mapping ─────────────────────────────────────────────────────────────

    private static Guid ParseGuid(object? val)
    {
        if (val is Guid g) return g;
        return val is string s ? Guid.Parse(s) : Guid.Empty;
    }

    private static bool ParseBool(object? val)
    {
        if (val is bool b) return b;
        return Convert.ToInt32(val) != 0;
    }

    private static FinancialMetricDefinition MapDefinition(dynamic r) => new()
    {
        Id = ParseGuid(r.id),
        ClientId = ParseGuid(r.client_id),
        CompanyId = ParseGuid(r.company_id),
        MetricKey = (string)r.metric_key,
        Label = (string)r.label,
        Category = (string)r.category,
        DataType = (string)r.data_type,
        Unit = (string?)r.unit,
        Description = (string?)r.description,
        DisplayOrder = (int)r.display_order,
        IsSystem = ParseBool(r.is_system),
        IsCalculated = ParseBool(r.is_calculated),
        IsActive = ParseBool(r.is_active)
    };

    private static object DefinitionParams(FinancialMetricDefinition d) => new
    {
        Id = d.Id.ToString(),
        ClientId = d.ClientId.ToString(),
        CompanyId = d.CompanyId.ToString(),
        d.MetricKey,
        d.Label,
        d.Category,
        d.DataType,
        d.Unit,
        d.Description,
        d.DisplayOrder,
        d.IsSystem,
        d.IsCalculated,
        d.IsActive
    };
}
