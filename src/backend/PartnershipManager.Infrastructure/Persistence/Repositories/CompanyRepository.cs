using System.Data;
using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório base com operações comuns usando Dapper
/// </summary>
public abstract class BaseRepository<T> where T : BaseEntity
{
    protected readonly DapperContext _context;
    protected abstract string TableName { get; }

    protected BaseRepository(DapperContext context)
    {
        _context = context;
    }

    protected IDbConnection Connection => _context.Connection;
    protected IDbTransaction? Transaction => _context.Transaction;

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        var sql = $"SELECT * FROM {TableName} WHERE id = @Id AND is_deleted = 0";
        return await Connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id.ToString() }, Transaction);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = $"SELECT * FROM {TableName} WHERE is_deleted = 0";
        return await Connection.QueryAsync<T>(sql, transaction: Transaction);
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        var sql = $"SELECT COUNT(1) FROM {TableName} WHERE id = @Id AND is_deleted = 0";
        var count = await Connection.ExecuteScalarAsync<int>(sql, new { Id = id.ToString() }, Transaction);
        return count > 0;
    }

    public virtual async Task<int> CountAsync()
    {
        var sql = $"SELECT COUNT(*) FROM {TableName} WHERE is_deleted = 0";
        return await Connection.ExecuteScalarAsync<int>(sql, transaction: Transaction);
    }

    public virtual async Task SoftDeleteAsync(Guid id, Guid? deletedBy = null)
    {
        var sql = $@"UPDATE {TableName} 
                     SET is_deleted = 1, deleted_at = @DeletedAt, updated_by = @DeletedBy 
                     WHERE id = @Id";
        await Connection.ExecuteAsync(sql, new 
        { 
            Id = id.ToString(), 
            DeletedAt = DateTime.UtcNow, 
            DeletedBy = deletedBy?.ToString() 
        }, Transaction);
    }
}

/// <summary>
/// Repositório de empresas
/// </summary>
public class CompanyRepository : BaseRepository<Company>, ICompanyRepository
{
    protected override string TableName => "companies";

    public CompanyRepository(DapperContext context) : base(context) { }

    public override async Task<Company?> GetByIdAsync(Guid id)
    {
        var sql = @"SELECT 
                        id AS Id,
                        client_id AS ClientId,
                        name AS Name,
                        trading_name AS TradingName,
                        cnpj AS Cnpj,
                        legal_form AS LegalForm,
                        foundation_date AS FoundationDate,
                        total_shares AS TotalShares,
                        share_price AS SharePrice,
                        currency AS Currency,
                        logo_url AS LogoUrl,
                        settings AS Settings,
                        status AS Status,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt,
                        created_by AS CreatedBy,
                        updated_by AS UpdatedBy,
                        is_deleted AS IsDeleted,
                        deleted_at AS DeletedAt
                    FROM companies 
                    WHERE id = @Id AND is_deleted = 0";
        
        return await Connection.QueryFirstOrDefaultAsync<Company>(sql, new { Id = id.ToString() }, Transaction);
    }

    public async Task<Company?> GetByCnpjAsync(string cnpj)
    {
        var normalizedCnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        
        var sql = @"SELECT 
                        id AS Id,
                        client_id AS ClientId,
                        name AS Name,
                        trading_name AS TradingName,
                        cnpj AS Cnpj,
                        legal_form AS LegalForm,
                        foundation_date AS FoundationDate,
                        total_shares AS TotalShares,
                        share_price AS SharePrice,
                        currency AS Currency,
                        logo_url AS LogoUrl,
                        settings AS Settings,
                        status AS Status,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM companies 
                    WHERE cnpj = @Cnpj AND is_deleted = 0";
        
        return await Connection.QueryFirstOrDefaultAsync<Company>(sql, new { Cnpj = normalizedCnpj }, Transaction);
    }

    public async Task<bool> CnpjExistsAsync(string cnpj, Guid? excludeId = null)
    {
        var normalizedCnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM companies WHERE cnpj = @Cnpj AND id != @ExcludeId AND is_deleted = 0"
            : "SELECT COUNT(1) FROM companies WHERE cnpj = @Cnpj AND is_deleted = 0";
        
        var count = await Connection.ExecuteScalarAsync<int>(sql, 
            new { Cnpj = normalizedCnpj, ExcludeId = excludeId?.ToString() }, Transaction);
        return count > 0;
    }

    public async Task<IEnumerable<Company>> GetActiveCompaniesAsync()
    {
        var sql = @"SELECT * FROM companies WHERE status = 'Active' AND is_deleted = 0 ORDER BY name";
        return await Connection.QueryAsync<Company>(sql, transaction: Transaction);
    }

    public async Task<(IEnumerable<Company> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null)
    {
        var offset = (page - 1) * pageSize;
        
        var whereClause = "WHERE is_deleted = 0";
        if (clientId.HasValue)
        {
            whereClause += " AND client_id = @ClientId";
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (name LIKE @Search OR trading_name LIKE @Search OR cnpj LIKE @Search)";
        }

        var countSql = $"SELECT COUNT(*) FROM companies {whereClause}";
        var dataSql = $@"SELECT 
                            id AS Id,
                            client_id AS ClientId,
                            name AS Name,
                            trading_name AS TradingName,
                            cnpj AS Cnpj,
                            legal_form AS LegalForm,
                            foundation_date AS FoundationDate,
                            total_shares AS TotalShares,
                            share_price AS SharePrice,
                            currency AS Currency,
                            logo_url AS LogoUrl,
                            status AS Status,
                            created_at AS CreatedAt,
                            updated_at AS UpdatedAt
                        FROM companies {whereClause}
                        ORDER BY name
                        LIMIT @PageSize OFFSET @Offset";

        var parameters = new { Search = $"%{search}%", ClientId = clientId, PageSize = pageSize, Offset = offset };

        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);
        var items = await Connection.QueryAsync<Company>(dataSql, parameters, Transaction);

        return (items, total);
    }

    public async Task AddAsync(Company company)
    {
        var sql = @"INSERT INTO companies 
                    (id, client_id, name, trading_name, cnpj, legal_form, foundation_date, 
                     total_shares, share_price, currency, logo_url, settings, status,
                     created_at, updated_at, created_by)
                    VALUES 
                    (@Id, @ClientId, @Name, @TradingName, @Cnpj, @LegalForm, @FoundationDate,
                     @TotalShares, @SharePrice, @Currency, @LogoUrl, @Settings, @Status,
                     @CreatedAt, @UpdatedAt, @CreatedBy)";

        await Connection.ExecuteAsync(sql, new
        {
            Id = company.Id.ToString(),
            ClientId = company.ClientId.ToString(),
            company.Name,
            company.TradingName,
            company.Cnpj,
            LegalForm = company.LegalForm.ToString(),
            company.FoundationDate,
            company.TotalShares,
            company.SharePrice,
            company.Currency,
            company.LogoUrl,
            company.Settings,
            Status = company.Status.ToString(),
            company.CreatedAt,
            company.UpdatedAt,
            CreatedBy = company.CreatedBy?.ToString()
        }, Transaction);
    }

    public async Task UpdateAsync(Company company)
    {
        var sql = @"UPDATE companies SET
                        name = @Name,
                        trading_name = @TradingName,
                        logo_url = @LogoUrl,
                        total_shares = @TotalShares,
                        share_price = @SharePrice,
                        status = @Status,
                        settings = @Settings,
                        updated_at = @UpdatedAt,
                        updated_by = @UpdatedBy
                    WHERE id = @Id";

        await Connection.ExecuteAsync(sql, new
        {
            Id = company.Id.ToString(),
            company.Name,
            company.TradingName,
            company.LogoUrl,
            company.TotalShares,
            company.SharePrice,
            Status = company.Status.ToString(),
            company.Settings,
            company.UpdatedAt,
            UpdatedBy = company.UpdatedBy?.ToString()
        }, Transaction);
    }

    public async Task DeleteAsync(Guid id)
    {
        await SoftDeleteAsync(id);
    }
}
