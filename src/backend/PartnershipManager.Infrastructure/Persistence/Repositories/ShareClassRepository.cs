using System.Data;
using System.Text;
using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

public class ShareClassRepository : IShareClassRepository
{
    private readonly DapperContext _context;

    public ShareClassRepository(DapperContext context)
    {
        _context = context;
    }

    private IDbConnection Connection => _context.Connection;
    private IDbTransaction? Transaction => _context.Transaction;

    private const string SelectColumns = @"
        sc.id AS Id,
        sc.client_id AS ClientId,
        sc.company_id AS CompanyId,
        sc.name AS Name,
        sc.code AS Code,
        sc.description AS Description,
        sc.has_voting_rights AS HasVotingRights,
        sc.votes_per_share AS VotesPerShare,
        sc.liquidation_preference AS LiquidationPreference,
        sc.participating AS Participating,
        sc.dividend_preference AS DividendPreference,
        sc.is_convertible AS IsConvertible,
        sc.converts_to_class_id AS ConvertsToClassId,
        sc.conversion_ratio AS ConversionRatio,
        sc.anti_dilution_type AS AntiDilutionType,
        sc.rights AS Rights,
        sc.status AS Status,
        sc.display_order AS DisplayOrder,
        sc.created_at AS CreatedAt,
        sc.updated_at AS UpdatedAt,
        sc.created_by AS CreatedBy,
        sc.updated_by AS UpdatedBy,
        sc.is_deleted AS IsDeleted,
        sc.deleted_at AS DeletedAt,
        c.name AS CompanyName,
        sct.name AS ConvertsToClassName";

    public async Task<(IEnumerable<ShareClass> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        string? search = null,
        string? status = null)
    {
        var baseQuery = new StringBuilder(@"FROM share_classes sc
                    LEFT JOIN companies c ON c.id = sc.company_id
                    LEFT JOIN share_classes sct ON sct.id = sc.converts_to_class_id
                    WHERE sc.is_deleted = 0
                      AND sc.client_id = @ClientId");

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", clientId.ToString());

        if (companyId.HasValue)
        {
            baseQuery.Append(@" AND (sc.company_id = @CompanyId OR sc.company_id IS NULL)
                                AND (sc.company_id IS NOT NULL OR NOT EXISTS (
                                    SELECT 1 FROM share_classes local
                                    WHERE local.client_id = sc.client_id
                                      AND local.company_id = @CompanyId
                                      AND local.code = sc.code
                                      AND local.is_deleted = 0))");
            parameters.Add("CompanyId", companyId.Value.ToString());
        }
        else
        {
            baseQuery.Append(" AND sc.company_id IS NULL");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery.Append(" AND (LOWER(sc.name) LIKE @Search OR LOWER(sc.code) LIKE @Search)");
            parameters.Add("Search", $"%{search.ToLower()}%");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery.Append(" AND sc.status = @Status");
            parameters.Add("Status", status);
        }

        var countSql = $"SELECT COUNT(1) {baseQuery}";
        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);

        baseQuery.Append(" ORDER BY sc.display_order ASC, sc.created_at DESC LIMIT @Offset, @PageSize");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var sql = $@"SELECT {SelectColumns} {baseQuery}";

        var rows = await Connection.QueryAsync<dynamic>(sql, parameters, Transaction);
        var items = rows.Select(MapToShareClass);

        return (items, total);
    }

    public async Task<IEnumerable<ShareClass>> GetByCompanyAsync(Guid clientId, Guid companyId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM share_classes sc
                    LEFT JOIN companies c ON c.id = sc.company_id
                    LEFT JOIN share_classes sct ON sct.id = sc.converts_to_class_id
                    WHERE sc.client_id = @ClientId
                      AND (sc.company_id = @CompanyId OR sc.company_id IS NULL)
                      AND (sc.company_id IS NOT NULL OR NOT EXISTS (
                          SELECT 1 FROM share_classes local
                          WHERE local.client_id = sc.client_id
                            AND local.company_id = @CompanyId
                            AND local.code = sc.code
                            AND local.is_deleted = 0))
                      AND sc.is_deleted = 0
                    ORDER BY sc.display_order ASC, sc.name ASC";

        var rows = await Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, Transaction);

        return rows.Select(MapToShareClass);
    }

    public async Task<ShareClass?> GetByIdAsync(Guid id, Guid clientId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM share_classes sc
                    LEFT JOIN companies c ON c.id = sc.company_id
                    LEFT JOIN share_classes sct ON sct.id = sc.converts_to_class_id
                    WHERE sc.id = @Id AND sc.client_id = @ClientId AND sc.is_deleted = 0";

        var shareClass = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);

        return shareClass == null ? null : MapToShareClass(shareClass);
    }

    public async Task<ShareClass?> GetByCodeAsync(Guid clientId, Guid companyId, string code)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM share_classes sc
                    LEFT JOIN companies c ON c.id = sc.company_id
                    LEFT JOIN share_classes sct ON sct.id = sc.converts_to_class_id
                    WHERE sc.client_id = @ClientId
                      AND (sc.company_id = @CompanyId OR sc.company_id IS NULL)
                      AND (sc.company_id = @CompanyId OR NOT EXISTS (
                          SELECT 1 FROM share_classes local
                          WHERE local.client_id = sc.client_id
                            AND local.company_id = @CompanyId
                            AND local.code = sc.code
                            AND local.is_deleted = 0))
                      AND UPPER(sc.code) = UPPER(@Code)
                      AND sc.is_deleted = 0
                    ORDER BY (sc.company_id = @CompanyId) DESC
                    LIMIT 1";

        var shareClass = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString(),
            Code = code.ToUpperInvariant()
        }, Transaction);

        return shareClass == null ? null : MapToShareClass(shareClass);
    }

    public async Task<bool> CodeExistsAsync(Guid clientId, Guid? companyId, string code, Guid? excludeId = null)
    {
        var sql = @"SELECT COUNT(1) FROM share_classes
                    WHERE client_id = @ClientId
                      AND company_id <=> @CompanyId
                      AND UPPER(code) = UPPER(@Code)
                      AND is_deleted = 0
                      AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        var count = await Connection.ExecuteScalarAsync<int>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId?.ToString(),
            Code = code.ToUpperInvariant(),
            ExcludeId = excludeId?.ToString()
        }, Transaction);

        return count > 0;
    }

    public async Task AddAsync(ShareClass shareClass)
    {
        var sql = @"INSERT INTO share_classes
                    (id, client_id, company_id, name, code, description, has_voting_rights, votes_per_share,
                     liquidation_preference, participating, dividend_preference, is_convertible, converts_to_class_id,
                     conversion_ratio, anti_dilution_type, rights, status, display_order, created_at, updated_at,
                     created_by, updated_by, is_deleted)
                    VALUES
                    (@Id, @ClientId, @CompanyId, @Name, @Code, @Description, @HasVotingRights, @VotesPerShare,
                     @LiquidationPreference, @Participating, @DividendPreference, @IsConvertible, @ConvertsToClassId,
                     @ConversionRatio, @AntiDilutionType, @Rights, @Status, @DisplayOrder, @CreatedAt, @UpdatedAt,
                     @CreatedBy, @UpdatedBy, @IsDeleted)";

        await Connection.ExecuteAsync(sql, new
        {
            Id = shareClass.Id.ToString(),
            ClientId = shareClass.ClientId.ToString(),
            CompanyId = shareClass.CompanyId?.ToString(),
            shareClass.Name,
            shareClass.Code,
            shareClass.Description,
            shareClass.HasVotingRights,
            shareClass.VotesPerShare,
            shareClass.LiquidationPreference,
            shareClass.Participating,
            shareClass.DividendPreference,
            shareClass.IsConvertible,
            ConvertsToClassId = shareClass.ConvertsToClassId?.ToString(),
            shareClass.ConversionRatio,
            AntiDilutionType = shareClass.AntiDilutionType?.ToString(),
            shareClass.Rights,
            Status = shareClass.Status.ToString(),
            shareClass.DisplayOrder,
            shareClass.CreatedAt,
            shareClass.UpdatedAt,
            CreatedBy = shareClass.CreatedBy?.ToString(),
            UpdatedBy = shareClass.UpdatedBy?.ToString(),
            shareClass.IsDeleted
        }, Transaction);
    }

    public async Task UpdateAsync(ShareClass shareClass)
    {
        var sql = @"UPDATE share_classes
                    SET name = @Name,
                        code = @Code,
                        description = @Description,
                        has_voting_rights = @HasVotingRights,
                        votes_per_share = @VotesPerShare,
                        liquidation_preference = @LiquidationPreference,
                        participating = @Participating,
                        dividend_preference = @DividendPreference,
                        is_convertible = @IsConvertible,
                        converts_to_class_id = @ConvertsToClassId,
                        conversion_ratio = @ConversionRatio,
                        anti_dilution_type = @AntiDilutionType,
                        rights = @Rights,
                        status = @Status,
                        display_order = @DisplayOrder,
                        updated_at = @UpdatedAt,
                        updated_by = @UpdatedBy
                    WHERE id = @Id AND client_id = @ClientId";

        await Connection.ExecuteAsync(sql, new
        {
            Id = shareClass.Id.ToString(),
            ClientId = shareClass.ClientId.ToString(),
            shareClass.Name,
            shareClass.Code,
            shareClass.Description,
            shareClass.HasVotingRights,
            shareClass.VotesPerShare,
            shareClass.LiquidationPreference,
            shareClass.Participating,
            shareClass.DividendPreference,
            shareClass.IsConvertible,
            ConvertsToClassId = shareClass.ConvertsToClassId?.ToString(),
            shareClass.ConversionRatio,
            AntiDilutionType = shareClass.AntiDilutionType?.ToString(),
            shareClass.Rights,
            Status = shareClass.Status.ToString(),
            shareClass.DisplayOrder,
            shareClass.UpdatedAt,
            UpdatedBy = shareClass.UpdatedBy?.ToString()
        }, Transaction);
    }

    public async Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null)
    {
        var sql = @"UPDATE share_classes
                    SET is_deleted = 1,
                        deleted_at = @DeletedAt,
                        updated_at = @DeletedAt,
                        updated_by = @DeletedBy
                    WHERE id = @Id AND client_id = @ClientId";

        var deletedAt = DateTime.UtcNow;

        await Connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString(),
            DeletedAt = deletedAt,
            DeletedBy = deletedBy?.ToString()
        }, Transaction);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid clientId)
    {
        var sql = @"SELECT COUNT(1) FROM share_classes WHERE id = @Id AND client_id = @ClientId AND is_deleted = 0";
        var count = await Connection.ExecuteScalarAsync<int>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);
        return count > 0;
    }

    public async Task<bool> HasSharesAsync(Guid id)
    {
        // This will be used when shares table exists
        // For now, return false (no shares associated)
        var sql = @"SELECT COUNT(1) FROM shares WHERE share_class_id = @Id AND is_deleted = 0";
        try
        {
            var count = await Connection.ExecuteScalarAsync<int>(sql, new { Id = id.ToString() }, Transaction);
            return count > 0;
        }
        catch
        {
            // Table doesn't exist yet
            return false;
        }
    }

    private static ShareClass MapToShareClass(dynamic row)
    {
        Guid ParseGuid(object value) => value is Guid g ? g : Guid.Parse(value.ToString()!);
        Guid? ParseNullableGuid(object? value) => value == null ? null : (value is Guid g ? g : Guid.Parse(value.ToString()!));

        AntiDilutionType? ParseAntiDilutionType(object? value)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return null;
            return Enum.TryParse<AntiDilutionType>(value.ToString(), true, out var result) ? result : null;
        }

        var shareClass = ShareClass.Create(
            ParseGuid(row.ClientId),
            ParseNullableGuid(row.CompanyId),
            (string)row.Name,
            (string)row.Code,
            (string?)row.Description,
            row.HasVotingRights is bool hvr ? hvr : row.HasVotingRights == 1,
            (decimal)row.VotesPerShare,
            (decimal)row.LiquidationPreference,
            row.Participating is bool p ? p : row.Participating == 1,
            row.DividendPreference as decimal?,
            row.IsConvertible is bool ic ? ic : row.IsConvertible == 1,
            ParseNullableGuid(row.ConvertsToClassId),
            row.ConversionRatio as decimal?,
            ParseAntiDilutionType(row.AntiDilutionType),
            (string?)row.Rights,
            (int)row.DisplayOrder,
            ParseNullableGuid(row.CreatedBy));

        shareClass.Id = ParseGuid(row.Id);
        shareClass.CreatedAt = (DateTime)row.CreatedAt;
        shareClass.UpdatedAt = (DateTime)row.UpdatedAt;
        shareClass.IsDeleted = row.IsDeleted is bool b ? b : row.IsDeleted == 1;
        shareClass.DeletedAt = row.DeletedAt as DateTime?;

        // Set navigation properties via reflection (private setters)
        var type = typeof(ShareClass);
        type.GetProperty("CompanyName")?.SetValue(shareClass, (string?)row.CompanyName);
        type.GetProperty("ConvertsToClassName")?.SetValue(shareClass, (string?)row.ConvertsToClassName);
        
        // Set status from DB
        if (row.Status is string statusStr)
        {
            var status = Enum.Parse<ShareClassStatus>(statusStr, true);
            if (status == ShareClassStatus.Inactive)
                shareClass.Deactivate();
        }

        return shareClass;
    }
}
