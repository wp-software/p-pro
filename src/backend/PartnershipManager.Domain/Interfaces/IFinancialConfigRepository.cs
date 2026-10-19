using PartnershipManager.Domain.Entities;

namespace PartnershipManager.Domain.Interfaces;

/// <summary>Repository for configurable financial metrics, their values and dashboard widgets.</summary>
public interface IFinancialConfigRepository
{
    Task<IReadOnlyList<FinancialMetricDefinition>> GetDefinitionsAsync(Guid clientId, Guid companyId, bool includeInactive);
    Task<FinancialMetricDefinition?> GetDefinitionByIdAsync(Guid id, Guid clientId);
    Task AddDefinitionAsync(FinancialMetricDefinition definition);
    Task UpdateDefinitionAsync(FinancialMetricDefinition definition);
    /// <summary>Deletes a custom definition together with its stored values.</summary>
    Task DeleteDefinitionAsync(Guid id, Guid clientId);

    Task<IReadOnlyList<FinancialMetricValue>> GetValuesByPeriodAsync(Guid periodId, Guid clientId);
    Task<IReadOnlyList<FinancialMetricValue>> GetValuesByPeriodsAsync(IEnumerable<Guid> periodIds, Guid clientId);
    Task UpsertValueAsync(Guid clientId, Guid periodId, Guid definitionId, decimal? value);

    Task<IReadOnlyList<FinancialDashboardWidget>> GetWidgetsAsync(Guid clientId, Guid companyId);
    Task ReplaceWidgetsAsync(Guid clientId, Guid companyId, IEnumerable<FinancialDashboardWidget> widgets);
}
