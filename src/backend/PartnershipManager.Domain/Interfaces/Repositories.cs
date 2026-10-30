using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Domain.Interfaces;

// =====================================================
// REPOSITÓRIOS
// =====================================================

/// <summary>
/// Interface base para repositórios
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
}

/// <summary>
/// Repositório de clientes (entidade raiz - Core Module)
/// </summary>
public interface ICoreClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByDocumentAsync(string document);
    Task<Client?> GetByEmailAsync(string email);
    Task<bool> DocumentExistsAsync(string document, Guid? excludeId = null);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<IEnumerable<Client>> GetActiveClientsAsync();
    Task<(IEnumerable<Client> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null);
    Task<IEnumerable<Company>> GetClientCompaniesAsync(Guid clientId);
    Task<int> GetClientCompaniesCountAsync(Guid clientId);
    Task<int> GetClientUsersCountAsync(Guid clientId);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id);
}

/// <summary>
/// Repositório de empresas
/// </summary>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company?> GetByCnpjAsync(string cnpj);
    Task<bool> CnpjExistsAsync(string cnpj, Guid? excludeId = null);
    Task<IEnumerable<Company>> GetActiveCompaniesAsync();
    Task<(IEnumerable<Company> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null);
    Task AddAsync(Company company);
    Task UpdateAsync(Company company);
    Task DeleteAsync(Guid id);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id);
}

/// <summary>
/// Repositório de sócios/acionistas
/// </summary>
public interface IShareholderRepository
{
    Task<(IEnumerable<Shareholder> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        string? search = null,
        string? type = null,
        string? status = null,
        string? sortDirection = null);

    Task<Shareholder?> GetByIdAsync(Guid id, Guid clientId);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(Guid clientId, IEnumerable<Guid> shareholderIds);
    Task<Shareholder?> GetByDocumentAsync(Guid clientId, string document);
    Task<Shareholder?> GetByEmailAsync(Guid clientId, Guid companyId, string email);
    Task<bool> DocumentExistsAsync(Guid clientId, string document, Guid? excludeId = null);
    Task AddAsync(Shareholder shareholder);
    Task UpdateAsync(Shareholder shareholder);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
    Task<Dictionary<Guid, List<ShareholderCompanyLink>>> GetCompanyLinksAsync(IEnumerable<Guid> shareholderIds);
    Task UpsertCompanyLinkAsync(Guid shareholderId, Guid clientId, ShareholderCompanyConfig config);
    Task RemoveCompanyLinkAsync(Guid shareholderId, Guid companyId);
    Task<bool> HasPositionInCompanyAsync(Guid shareholderId, Guid companyId);
    Task<bool> IsLinkedToCompanyAsync(Guid shareholderId, Guid companyId);
}

public record ShareholderCompanyConfig(
    Guid CompanyId,
    ShareholderType Type,
    bool EarnOut,
    bool TagAlong,
    bool DragAlong,
    bool ShareholdersAgreement,
    bool RightOfFirstRefusal,
    bool LiquidationPreferenceRight,
    bool AntiDilution);

public record ShareholderCompanyLink(Guid CompanyId, string CompanyName, ShareholderCompanyConfig Config);

/// <summary>
/// Repositório de classes de ações
/// </summary>
public interface IShareClassRepository
{
    Task<(IEnumerable<ShareClass> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        string? search = null,
        string? status = null);

    Task<IEnumerable<ShareClass>> GetByCompanyAsync(Guid clientId, Guid companyId);
    Task<ShareClass?> GetByIdAsync(Guid id, Guid clientId);
    Task<ShareClass?> GetByCodeAsync(Guid clientId, Guid companyId, string code);
    Task<bool> CodeExistsAsync(Guid clientId, Guid? companyId, string code, Guid? excludeId = null);
    Task AddAsync(ShareClass shareClass);
    Task UpdateAsync(ShareClass shareClass);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
    Task<bool> HasSharesAsync(Guid id);
}

/// <summary>
/// Repositório de ações/participações
/// </summary>
public interface IShareRepository
{
    Task<(IEnumerable<Share> Items, int Total, decimal TotalShares, decimal TotalValue)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        Guid? shareholderId = null,
        Guid? shareClassId = null,
        string? status = null);

    Task<IEnumerable<Share>> GetByShareholderAsync(Guid clientId, Guid shareholderId);
    Task<IEnumerable<Share>> GetByShareClassAsync(Guid clientId, Guid shareClassId);
    Task<IEnumerable<Share>> GetActiveByCompanyAsync(Guid clientId, Guid companyId);
    Task<Share?> GetByIdAsync(Guid id, Guid clientId);
    Task<decimal> GetShareholderBalanceAsync(Guid clientId, Guid shareholderId, Guid shareClassId);
    Task<decimal> GetTotalSharesByCompanyAsync(Guid clientId, Guid companyId);
    Task<decimal> GetTotalSharesByClassAsync(Guid clientId, Guid shareClassId);
    Task AddAsync(Share share);
    Task UpdateAsync(Share share);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>
/// Repositório de transações de ações (ledger imutável)
/// </summary>
public interface IShareTransactionRepository
{
    Task<(IEnumerable<ShareTransaction> Items, int Total, decimal TotalQuantity, decimal TotalValue)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        string? transactionType = null,
        Guid? shareholderId = null,
        Guid? shareClassId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<IEnumerable<ShareTransaction>> GetByShareAsync(Guid clientId, Guid shareId);
    Task<IEnumerable<ShareTransaction>> GetByShareholderAsync(Guid clientId, Guid shareholderId);
    Task<ShareTransaction?> GetByIdAsync(Guid id, Guid clientId);
    Task<string> GetNextTransactionNumberAsync(Guid companyId);
    Task AddAsync(ShareTransaction transaction);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

// =====================================================
// CONTRATOS
// =====================================================

/// <summary>
/// Repositório de templates de contratos reutilizáveis
/// </summary>
public interface IContractTemplateRepository
{
    Task<(IEnumerable<ContractTemplate> Items, int Total)> GetPagedAsync(
        Guid clientId,
        int page,
        int pageSize,
        string? search = null,
        string? templateType = null,
        bool? isActive = null);

    Task<ContractTemplate?> GetByIdAsync(Guid id, Guid clientId);
    Task<ContractTemplate?> GetByCodeAsync(Guid clientId, string code);
    Task<bool> CodeExistsAsync(Guid clientId, string code, Guid? excludeId = null);
    Task<IEnumerable<ContractTemplate>> GetActiveTemplatesAsync(Guid clientId);
    Task<IEnumerable<ContractTemplate>> GetByTypeAsync(Guid clientId, ContractTemplateType templateType);
    Task AddAsync(ContractTemplate template);
    Task UpdateAsync(ContractTemplate template);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>
/// Repositório de biblioteca de cláusulas padronizadas
/// </summary>
public interface IClauseRepository
{
    Task<(IEnumerable<Clause> Items, int Total)> GetPagedAsync(
        Guid clientId,
        int page,
        int pageSize,
        string? search = null,
        string? clauseType = null,
        bool? isMandatory = null,
        bool? isActive = null);

    Task<Clause?> GetByIdAsync(Guid id, Guid clientId);
    Task<Clause?> GetByCodeAsync(Guid clientId, string code);
    Task<IEnumerable<Clause>> GetByTypeAsync(Guid clientId, ClauseType clauseType);
    Task<IEnumerable<Clause>> GetMandatoryClausesAsync(Guid clientId);
    Task<IEnumerable<Clause>> GetActiveClausesAsync(Guid clientId);
    Task<bool> CodeExistsAsync(Guid clientId, string code, Guid? excludeId = null);
    Task AddAsync(Clause clause);
    Task UpdateAsync(Clause clause);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>
/// Repositório de contratos gerados
/// </summary>
public interface IContractRepository
{
    Task<(IEnumerable<Contract> Items, int Total)> GetPagedAsync(
        Guid clientId,
        int page,
        int pageSize,
        Guid? companyId = null,
        string? search = null,
        string? status = null,
        string? contractType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<Contract?> GetByIdAsync(Guid id, Guid clientId);
    Task<Contract?> GetWithDetailsAsync(Guid id, Guid clientId);
    Task<IEnumerable<Contract>> GetByCompanyAsync(Guid clientId, Guid companyId);
    Task<IEnumerable<Contract>> GetByStatusAsync(Guid clientId, ContractStatus status);
    Task<IEnumerable<Contract>> GetExpiredContractsAsync(Guid clientId);
    Task<IEnumerable<Contract>> GetByTemplateAsync(Guid clientId, Guid templateId);
    Task AddAsync(Contract contract);
    Task UpdateAsync(Contract contract);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>
/// Repositório de versões de contrato (histórico de documentos)
/// </summary>
public interface IContractVersionRepository
{
    Task<IEnumerable<ContractVersion>> GetByContractAsync(Guid contractId);
    Task<ContractVersion?> GetByIdAsync(Guid versionId);
    Task<ContractVersion?> GetByVersionNumberAsync(Guid contractId, int versionNumber);
    Task<ContractVersion?> GetLatestAsync(Guid contractId);
    Task AddAsync(ContractVersion version);
}

/// <summary>
/// Repositório de usuários
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email, Guid? companyId = null);
    Task<User?> GetByShareholderIdAsync(Guid shareholderId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<bool> EmailExistsAsync(string email, Guid companyId, Guid? excludeId = null);
    Task<IEnumerable<User>> GetByCompanyAsync(Guid companyId);
    Task<IEnumerable<User>> GetActiveUsersByCompanyAsync(Guid companyId);
    Task<(IEnumerable<User> Items, int Total)> GetPagedByCompanyAsync(Guid companyId, int page, int pageSize, string? search = null);
    Task<(IEnumerable<User> Items, int Total)> GetPagedByClientAsync(Guid clientId, int page, int pageSize, string? search = null);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id);
    Task UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiry);
    Task UpdateLoginInfoAsync(Guid userId, bool success);
    Task<User?> GetByPasswordResetTokenAsync(string hashedToken);
    Task UpdatePasswordResetTokenAsync(Guid userId, string? hashedToken, DateTime? expiry);
    Task UpdatePasswordHashAsync(Guid userId, string passwordHash);
}

/// <summary>
/// Repositório de papéis de usuário
/// </summary>
public interface IUserRoleRepository
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<string>> GetRoleNamesByUserIdAsync(Guid userId);
    Task AddAsync(UserRole userRole);
    Task DeactivateAsync(Guid userId, string role);
    Task<bool> ExistsAsync(Guid userId, string role);
}

/// <summary>
/// Repositório de logs de auditoria
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, int limit = 100);
    Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int limit = 100);
    Task<IEnumerable<AuditLog>> GetByCompanyAsync(Guid companyId, int limit = 100);
}

// =====================================================
// VESTING MODULE
// =====================================================

/// <summary>
/// Repositório de planos de vesting
/// </summary>
public interface IVestingPlanRepository
{
    Task<(IEnumerable<VestingPlan> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid companyId,
        int page,
        int pageSize,
        string? search = null,
        string? status = null);

    Task<VestingPlan?> GetByIdAsync(Guid id, Guid clientId);
    Task<IEnumerable<VestingPlan>> GetByCompanyAsync(Guid clientId, Guid companyId, string? status = null);
    Task AddAsync(VestingPlan plan);
    Task UpdateAsync(VestingPlan plan);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
    Task<bool> NameExistsAsync(Guid clientId, Guid companyId, string name, Guid? excludeId = null);
}

/// <summary>
/// Repositório de grants de vesting
/// </summary>
public interface IVestingGrantRepository
{
    Task<(IEnumerable<VestingGrant> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        Guid? vestingPlanId = null,
        Guid? shareholderId = null,
        string? status = null);

    Task<VestingGrant?> GetByIdAsync(Guid id, Guid clientId);
    Task<IEnumerable<VestingGrant>> GetByShareholderAsync(Guid clientId, Guid shareholderId, Guid? companyId = null);
    Task<IEnumerable<VestingGrant>> GetByPlanAsync(Guid clientId, Guid vestingPlanId);
    Task<IEnumerable<VestingGrant>> GetActiveGrantsForCompanyAsync(Guid clientId, Guid companyId);
    Task AddAsync(VestingGrant grant);
    Task UpdateAsync(VestingGrant grant);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>
/// Repositório de schedules de vesting (cronograma periódico)
/// </summary>
public interface IVestingScheduleRepository
{
    Task<IEnumerable<VestingSchedule>> GetByGrantAsync(Guid clientId, Guid vestingGrantId);
    Task<IEnumerable<VestingSchedule>> GetUpcomingAsync(Guid clientId, Guid companyId, DateTime fromDate, DateTime toDate);
    Task AddRangeAsync(IEnumerable<VestingSchedule> schedules);
    Task UpdateAsync(VestingSchedule schedule);
    Task DeleteByGrantAsync(Guid vestingGrantId);
}

/// <summary>
/// Repositório de milestones de vesting
/// </summary>
public interface IVestingMilestoneRepository
{
    Task<IEnumerable<VestingMilestone>> GetByPlanAsync(Guid clientId, Guid vestingPlanId);
    Task<(IEnumerable<VestingMilestone> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid companyId,
        int page,
        int pageSize,
        Guid? vestingPlanId = null,
        string? status = null);
    Task<VestingMilestone?> GetByIdAsync(Guid id, Guid clientId);
    Task AddAsync(VestingMilestone milestone);
    Task UpdateAsync(VestingMilestone milestone);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

// ─── Grant Milestones Module ──────────────────────────────────────────────────

/// <summary>
/// Repositório de templates de milestones reutilizáveis por empresa.
/// </summary>
public interface IMilestoneTemplateRepository
{
    Task<IEnumerable<MilestoneTemplate>> GetByCompanyAsync(Guid clientId, Guid companyId, bool activeOnly = true);
    Task<IEnumerable<MilestoneTemplate>> GetByCategoryAsync(Guid clientId, Guid companyId, MilestoneCategory category);
    Task<(IEnumerable<MilestoneTemplate> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid companyId, int page, int pageSize, string? category = null, bool? isActive = null);
    Task<MilestoneTemplate?> GetByIdAsync(Guid id, Guid clientId);
    Task AddAsync(MilestoneTemplate template);
    Task UpdateAsync(MilestoneTemplate template);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

/// <summary>
/// Repositório de milestones por grant (performance-based vesting).
/// </summary>
public interface IGrantMilestoneRepository
{
    Task<IEnumerable<GrantMilestone>> GetByGrantAsync(Guid clientId, Guid vestingGrantId);
    Task<(IEnumerable<GrantMilestone> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid companyId, int page, int pageSize,
        Guid? vestingGrantId = null, string? status = null, string? category = null);
    Task<GrantMilestone?> GetByIdAsync(Guid id, Guid clientId);
    Task<IEnumerable<GrantMilestone>> GetPendingAccelerationsAsync(Guid clientId, Guid companyId);
    Task AddAsync(GrantMilestone milestone);
    Task UpdateAsync(GrantMilestone milestone);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

/// <summary>
/// Repositório de progresso de milestones — série temporal imutável.
/// </summary>
public interface IMilestoneProgressRepository
{
    Task<IEnumerable<MilestoneProgress>> GetByMilestoneAsync(Guid clientId, Guid grantMilestoneId);
    Task<MilestoneProgress?> GetLatestAsync(Guid clientId, Guid grantMilestoneId);
    Task<IEnumerable<MilestoneProgress>> GetTimeSeriesAsync(
        Guid clientId, Guid grantMilestoneId, DateTime from, DateTime to);
    Task AddAsync(MilestoneProgress progress);
}

/// <summary>
/// Repositório de acelerações de vesting — ledger imutável.
/// </summary>
public interface IVestingAccelerationRepository
{
    Task<IEnumerable<VestingAcceleration>> GetByGrantAsync(Guid clientId, Guid vestingGrantId);
    Task<VestingAcceleration?> GetByMilestoneAsync(Guid clientId, Guid grantMilestoneId);
    Task<decimal> GetTotalAccelerationForGrantAsync(Guid clientId, Guid vestingGrantId);
    Task AddAsync(VestingAcceleration acceleration);
}

/// <summary>
/// Repositório de transações de exercício de vesting (append-only ledger)
/// </summary>
public interface IVestingTransactionRepository
{
    Task<IEnumerable<VestingTransaction>> GetByGrantAsync(Guid vestingGrantId);
    Task<IEnumerable<VestingTransaction>> GetByShareholderAsync(Guid clientId, Guid shareholderId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<(IEnumerable<VestingTransaction> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid companyId,
        int page,
        int pageSize,
        Guid? shareholderId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task AddAsync(VestingTransaction transaction);
    Task UpdateShareTransactionLinkAsync(Guid id, Guid shareTransactionId);
}

// =====================================================
// UNIT OF WORK
// =====================================================

/// <summary>
/// Unit of Work para transações
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ICompanyRepository Companies { get; }
    IUserRepository Users { get; }
    IUserRoleRepository UserRoles { get; }
    IAuditLogRepository AuditLogs { get; }
    IVestingPlanRepository VestingPlans { get; }
    IVestingGrantRepository VestingGrants { get; }
    IVestingMilestoneRepository VestingMilestones { get; }
    IVestingTransactionRepository VestingTransactions { get; }

    // Grant Milestones module
    IMilestoneTemplateRepository MilestoneTemplates { get; }
    IGrantMilestoneRepository GrantMilestones { get; }
    IMilestoneProgressRepository MilestoneProgress { get; }
    IVestingAccelerationRepository VestingAccelerations { get; }

    // Fase 5 — Valuation module
    IValuationRepository Valuations { get; }
    IValuationMethodRepository ValuationMethods { get; }
    IValuationDocumentRepository ValuationDocuments { get; }

    // Fase 5 — Financial module
    IFinancialPeriodRepository FinancialPeriods { get; }
    IFinancialMetricRepository FinancialMetrics { get; }

    // Fase 5 — Documents module
    IDocumentRepository Documents { get; }

    // Fase 5 — Custom Formula module
    ICustomFormulaRepository CustomFormulas { get; }
    IFormulaVersionRepository FormulaVersions { get; }
    IFormulaExecutionRepository FormulaExecutions { get; }

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

// =====================================================
// SERVIÇOS
// =====================================================

/// <summary>
/// Serviço de cache
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class;
}

// =====================================================
// FASE 5 — VALUATION MODULE
// =====================================================

/// <summary>Repository for Valuation aggregate root.</summary>
public interface IValuationRepository
{
    Task<(IEnumerable<Valuation> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid? companyId, int page, int pageSize,
        string? status = null, string? eventType = null);
    Task<Valuation?> GetByIdAsync(Guid id, Guid clientId);
    Task<Valuation?> GetLastApprovedAsync(Guid clientId, Guid companyId);
    Task AddAsync(Valuation valuation);
    Task UpdateAsync(Valuation valuation);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid id, Guid clientId);
}

/// <summary>Repository for ValuationMethod (calculation methodologies per valuation).</summary>
public interface IValuationMethodRepository
{
    Task<IEnumerable<ValuationMethod>> GetByValuationAsync(Guid valuationId, Guid clientId);
    Task<ValuationMethod?> GetByIdAsync(Guid id, Guid clientId);
    Task<ValuationMethod?> GetSelectedAsync(Guid valuationId, Guid clientId);
    Task AddAsync(ValuationMethod method);
    Task UpdateAsync(ValuationMethod method);
    Task DeleteAsync(Guid id, Guid clientId);
    /// <summary>Sets is_selected = false for all methods of this valuation, then true for the given methodId.</summary>
    Task SetSelectedAsync(Guid valuationId, Guid methodId, Guid clientId, Guid updatedBy);
}

/// <summary>Repository for ValuationDocument (supporting docs per valuation).</summary>
public interface IValuationDocumentRepository
{
    Task<IEnumerable<ValuationDocument>> GetByValuationAsync(Guid valuationId, Guid clientId);
    Task<ValuationDocument?> GetByIdAsync(Guid id, Guid clientId);
    Task AddAsync(ValuationDocument doc);
    Task UpdateAsync(ValuationDocument doc);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

// =====================================================
// FASE 5 — FINANCIAL MODULE
// =====================================================

/// <summary>Repository for FinancialPeriod (monthly container).</summary>
public interface IFinancialPeriodRepository
{
    Task<(IEnumerable<FinancialPeriod> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid companyId, int page, int pageSize,
        int? year = null, string? status = null);
    Task<IEnumerable<FinancialPeriod>> GetByYearAsync(Guid clientId, Guid companyId, short year);
    Task<FinancialPeriod?> GetByIdAsync(Guid id, Guid clientId);
    Task<FinancialPeriod?> GetByYearMonthAsync(Guid clientId, Guid companyId, short year, byte month);
    Task<FinancialPeriod?> GetPreviousPeriodAsync(Guid clientId, Guid companyId, short year, byte month);
    Task AddAsync(FinancialPeriod period);
    Task UpdateAsync(FinancialPeriod period);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
    Task<bool> ExistsAsync(Guid clientId, Guid companyId, short year, byte month);
}

/// <summary>Repository for FinancialMetric (KPIs per period — one row per period).</summary>
public interface IFinancialMetricRepository
{
    Task<FinancialMetric?> GetByPeriodAsync(Guid periodId, Guid clientId);
    Task<IEnumerable<FinancialMetric>> GetByCompanyAsync(Guid clientId, Guid companyId, int lastNPeriods = 12);
    Task AddAsync(FinancialMetric metric);
    Task UpdateAsync(FinancialMetric metric);
}

// =====================================================
// FASE 5 — DOCUMENTS MODULE
// =====================================================

/// <summary>Repository for central polymorphic Document store.</summary>
public interface IDocumentRepository
{
    Task<(IEnumerable<Document> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid companyId, int page, int pageSize,
        string? documentType = null, string? visibility = null, string? search = null);
    Task<IEnumerable<Document>> GetByEntityAsync(Guid clientId, Guid companyId, string entityType, Guid entityId);
    Task<Document?> GetByIdAsync(Guid id, Guid clientId);
    Task AddAsync(Document document);
    Task UpdateAsync(Document document);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

// =====================================================
// FASE 5 — CUSTOM FORMULA MODULE
// =====================================================

/// <summary>Repository for ValuationCustomFormula (formula definition container).</summary>
public interface ICustomFormulaRepository
{
    Task<(IEnumerable<ValuationCustomFormula> Items, int Total)> GetPagedAsync(
        Guid clientId, Guid companyId, int page, int pageSize,
        bool? isActive = null, string? sectorTag = null);
    Task<IEnumerable<ValuationCustomFormula>> GetActiveByCompanyAsync(Guid clientId, Guid companyId);
    Task<ValuationCustomFormula?> GetByIdAsync(Guid id, Guid clientId);
    Task AddAsync(ValuationCustomFormula formula);
    Task UpdateAsync(ValuationCustomFormula formula);
    Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null);
}

/// <summary>Repository for ValuationFormulaVersion (immutable versioned snapshots).</summary>
public interface IFormulaVersionRepository
{
    Task<IEnumerable<ValuationFormulaVersion>> GetByFormulaAsync(Guid formulaId, Guid clientId);
    Task<ValuationFormulaVersion?> GetByIdAsync(Guid id, Guid clientId);
    Task<ValuationFormulaVersion?> GetCurrentVersionAsync(Guid formulaId, Guid clientId);
    Task<int> GetNextVersionNumberAsync(Guid formulaId);
    Task AddAsync(ValuationFormulaVersion version);
    // Note: no Update — versions are immutable
}

/// <summary>Repository for ValuationFormulaExecution (immutable audit log).</summary>
public interface IFormulaExecutionRepository
{
    Task<IEnumerable<ValuationFormulaExecution>> GetByMethodAsync(Guid valuationMethodId, Guid clientId);
    Task<IEnumerable<ValuationFormulaExecution>> GetByVersionAsync(Guid formulaVersionId, Guid clientId);
    Task AddAsync(ValuationFormulaExecution execution);
    // Note: no Update or Delete — executions are immutable
}

/// <summary>
/// Serviço de data/hora (para facilitar testes)
/// </summary>
public interface IDateTimeService
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

/// <summary>
/// Serviço de usuário atual
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? ClientId { get; }
    Guid? CompanyId { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}

// =====================================================
// ENTIDADES ADICIONAIS
// =====================================================

/// <summary>
/// Log de auditoria
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        AuditAction action,
        Guid? userId = null,
        Guid? companyId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// =====================================================
// FASE 6 — COMMUNICATION / DATA ROOM / NOTIFICATION / WORKFLOW
// =====================================================

/// <summary>
/// Repositório de comunicações
/// </summary>
public interface ICommunicationRepository
{
    Task<(IEnumerable<Communication> Items, int Total)> GetByCompanyAsync(Guid companyId, int page, int pageSize, string? search = null, string? commType = null, bool? isPublished = null);
    Task<Communication?> GetByIdAsync(Guid id, Guid companyId);
    Task<Guid> CreateAsync(Communication communication);
    Task UpdateAsync(Communication communication);
    Task SoftDeleteAsync(Guid id, Guid companyId);
    Task PublishAsync(Guid id, Guid companyId);
    Task TrackViewAsync(Guid communicationId, Guid userId, int? durationSecs);
    Task<bool> HasViewedAsync(Guid communicationId, Guid userId);
    Task<IEnumerable<Communication>> GetForRoleAsync(Guid companyId, string role, int limit);
}

/// <summary>
/// Repositório de Data Room
/// </summary>
public interface IDataRoomRepository
{
    Task<DataRoom?> GetByCompanyAsync(Guid companyId);
    Task<Guid> CreateDataRoomAsync(DataRoom dataRoom);
    Task<IEnumerable<DataRoomFolder>> GetFoldersAsync(Guid dataRoomId, Guid? parentId = null);
    Task<DataRoomFolder?> GetFolderByIdAsync(Guid folderId);
    Task<Guid> CreateFolderAsync(DataRoomFolder folder);
    Task UpdateFolderAsync(DataRoomFolder folder);
    Task SoftDeleteFolderAsync(Guid folderId);
    Task<IEnumerable<Document>> GetDocumentsInFolderAsync(Guid folderId);
    Task AddDocumentToFolderAsync(Guid folderId, Guid documentId, Guid addedBy);
    Task RemoveDocumentFromFolderAsync(Guid folderId, Guid documentId);
    Task<IEnumerable<DataRoomFolder>> GetAllFoldersAsync(Guid dataRoomId);
    Task<IEnumerable<DataRoomFolderRole>> GetFolderRolesByDataRoomAsync(Guid dataRoomId);
    Task<bool> FinancialPeriodExistsAsync(Guid periodId, Guid clientId, Guid companyId);
    Task AddFinancialMetricDocumentAsync(FinancialMetricDocument link);
    Task<IEnumerable<FinancialMetricDocument>> GetFinancialMetricDocumentsAsync(Guid periodId);
    Task<FinancialMetricDocument?> GetFinancialMetricDocumentAsync(Guid documentId);
    Task DeleteFinancialMetricDocumentAsync(Guid documentId);
    Task SetFolderRolesAsync(Guid folderId, IEnumerable<string> roles, Guid grantedBy);
    Task<string?> GetDocumentNameAsync(Guid documentId);
    Task AddActivityLogAsync(DataRoomActivityLog log);
    Task<IEnumerable<DataRoomActivityLog>> GetActivityLogsAsync(Guid dataRoomId, Guid? folderId, int limit);
}

/// <summary>
/// Repositório de notificações
/// </summary>
public interface INotificationRepository
{
    Task<Guid> CreateAsync(Notification notification);
    Task<(IEnumerable<Notification> Items, int Total)> GetByUserAsync(Guid userId, Guid? companyId, int page, int pageSize);
    Task<IEnumerable<Notification>> GetRecentByUserAsync(Guid userId, Guid? companyId, int limit = 10);
    Task<int> GetUnreadCountAsync(Guid userId, Guid? companyId);
    Task MarkAsReadAsync(Guid id, Guid userId);
    Task MarkAllAsReadAsync(Guid userId, Guid companyId);
    Task<NotificationPreference?> GetPreferenceAsync(Guid userId, string notificationType);
    Task UpsertPreferenceAsync(NotificationPreference preference);
    Task<IEnumerable<NotificationPreference>> GetAllPreferencesAsync(Guid userId);
}

/// <summary>
/// Repositório de emails
/// </summary>
public interface IEmailLogRepository
{
    Task<Guid> CreateAsync(EmailLog emailLog);
    Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, string? resendMessageId = null);
}

/// <summary>
/// Repositório de workflows
/// </summary>
public interface IWorkflowRepository
{
    Task<Guid> CreateAsync(Workflow workflow, IEnumerable<WorkflowStep> steps);
    Task<Workflow?> GetByIdAsync(Guid id, Guid companyId);
    Task<(IEnumerable<Workflow> Items, int Total)> GetByCompanyAsync(Guid companyId, int page, int pageSize, string? status = null, string? workflowType = null);
    Task<IEnumerable<Workflow>> GetPendingByUserAsync(Guid userId, Guid companyId);
    Task<WorkflowStep?> GetCurrentStepAsync(Guid workflowId);
    Task<IEnumerable<WorkflowStep>> GetStepsAsync(Guid workflowId);
    Task RecordApprovalAsync(WorkflowApproval approval);
    Task AdvanceStepAsync(Guid workflowId, int nextStep);
    Task CompleteWorkflowAsync(Guid workflowId, string finalStatus);
    Task CancelWorkflowAsync(Guid workflowId, Guid cancelledBy, string reason);
    Task UpdateStepStatusAsync(Guid stepId, string status, Guid? completedBy = null);
}

public interface IApprovalGroupRepository
{
    Task<IEnumerable<ApprovalGroup>> GetByCompanyAsync(Guid companyId);
    Task<ApprovalGroup?> GetByIdAsync(Guid id, Guid companyId);
    Task<Guid> CreateAsync(ApprovalGroup group, IEnumerable<Guid> memberIds);
    Task UpdateAsync(ApprovalGroup group, IEnumerable<Guid> memberIds);
    Task DeleteAsync(Guid id, Guid companyId);
}

public interface IWorkflowTemplateRepository
{
    Task<IEnumerable<WorkflowTemplate>> GetByCompanyAsync(Guid companyId);
    Task<WorkflowTemplate?> GetByIdAsync(Guid id, Guid companyId);
    Task<Guid> CreateAsync(WorkflowTemplate template);
    Task UpdateAsync(WorkflowTemplate template);
    Task DeleteAsync(Guid id, Guid companyId);
}
