using System.Data;
using System.Text;
using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

public class ShareRepository : IShareRepository
{
    private readonly DapperContext _context;

    public ShareRepository(DapperContext context)
    {
        _context = context;
    }

    private IDbConnection Connection => _context.Connection;
    private IDbTransaction? Transaction => _context.Transaction;

    private const string SelectColumns = @"
        s.id AS Id,
        s.client_id AS ClientId,
        s.company_id AS CompanyId,
        s.shareholder_id AS ShareholderId,
        COALESCE(shc.type, sh.type) AS ShareholderType,
        s.share_class_id AS ShareClassId,
        s.certificate_number AS CertificateNumber,
        s.quantity AS Quantity,
        s.acquisition_price AS AcquisitionPrice,
        s.acquisition_date AS AcquisitionDate,
        s.origin AS Origin,
        s.origin_transaction_id AS OriginTransactionId,
        s.status AS Status,
        s.notes AS Notes,
        s.created_at AS CreatedAt,
        s.updated_at AS UpdatedAt,
        s.created_by AS CreatedBy,
        s.updated_by AS UpdatedBy,
        s.is_deleted AS IsDeleted,
        s.deleted_at AS DeletedAt,
        sh.name AS ShareholderName,
        sc.name AS ShareClassName,
        sc.code AS ShareClassCode,
        c.name AS CompanyName";

    public async Task<(IEnumerable<Share> Items, int Total, decimal TotalShares, decimal TotalValue)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        Guid? shareholderId = null,
        Guid? shareClassId = null,
        string? status = null)
    {
        var baseQuery = new StringBuilder(@"FROM shares s
                    JOIN shareholders sh ON sh.id = s.shareholder_id
                    LEFT JOIN shareholder_companies shc
                        ON shc.shareholder_id = sh.id AND shc.company_id = s.company_id
                    JOIN share_classes sc ON sc.id = s.share_class_id
                    JOIN companies c ON c.id = s.company_id
                    WHERE s.is_deleted = 0
                      AND s.client_id = @ClientId");

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", clientId.ToString());

        if (companyId.HasValue)
        {
            baseQuery.Append(" AND s.company_id = @CompanyId");
            parameters.Add("CompanyId", companyId.Value.ToString());
        }

        if (shareholderId.HasValue)
        {
            baseQuery.Append(" AND s.shareholder_id = @ShareholderId");
            parameters.Add("ShareholderId", shareholderId.Value.ToString());
        }

        if (shareClassId.HasValue)
        {
            baseQuery.Append(" AND s.share_class_id = @ShareClassId");
            parameters.Add("ShareClassId", shareClassId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery.Append(" AND s.status = @Status");
            parameters.Add("Status", status);
        }

        // Count and aggregates
        var countSql = $@"SELECT COUNT(1) {baseQuery}";
        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);

        var aggregateSql = $@"SELECT COALESCE(SUM(s.quantity), 0) AS TotalShares, 
                               COALESCE(SUM(s.quantity * s.acquisition_price), 0) AS TotalValue 
                               {baseQuery}";
        var aggregates = await Connection.QueryFirstOrDefaultAsync<dynamic>(aggregateSql, parameters, Transaction);
        var totalShares = (decimal)(aggregates?.TotalShares ?? 0m);
        var totalValue = (decimal)(aggregates?.TotalValue ?? 0m);

        // Paged results
        baseQuery.Append(" ORDER BY s.acquisition_date DESC, s.created_at DESC LIMIT @Offset, @PageSize");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var sql = $@"SELECT {SelectColumns} {baseQuery}";

        var rows = await Connection.QueryAsync<dynamic>(sql, parameters, Transaction);
        var items = rows.Select(MapToShare);

        return (items, total, totalShares, totalValue);
    }

    public async Task<IEnumerable<Share>> GetByShareholderAsync(Guid clientId, Guid shareholderId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM shares s
                    JOIN shareholders sh ON sh.id = s.shareholder_id
                    LEFT JOIN shareholder_companies shc
                        ON shc.shareholder_id = sh.id AND shc.company_id = s.company_id
                    JOIN share_classes sc ON sc.id = s.share_class_id
                    JOIN companies c ON c.id = s.company_id
                    WHERE s.client_id = @ClientId
                      AND s.shareholder_id = @ShareholderId
                      AND s.is_deleted = 0
                      AND s.status = 'Active'
                    ORDER BY s.acquisition_date DESC";

        var rows = await Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            ShareholderId = shareholderId.ToString()
        }, Transaction);

        return rows.Select(MapToShare);
    }

    public async Task<IEnumerable<Share>> GetByShareClassAsync(Guid clientId, Guid shareClassId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM shares s
                    JOIN shareholders sh ON sh.id = s.shareholder_id
                    LEFT JOIN shareholder_companies shc
                        ON shc.shareholder_id = sh.id AND shc.company_id = s.company_id
                    JOIN share_classes sc ON sc.id = s.share_class_id
                    JOIN companies c ON c.id = s.company_id
                    WHERE s.client_id = @ClientId
                      AND s.share_class_id = @ShareClassId
                      AND s.is_deleted = 0
                      AND s.status = 'Active'
                    ORDER BY s.quantity DESC";

        var rows = await Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            ShareClassId = shareClassId.ToString()
        }, Transaction);

        return rows.Select(MapToShare);
    }

    public async Task<IEnumerable<Share>> GetActiveByCompanyAsync(Guid clientId, Guid companyId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM shares s
                    JOIN shareholders sh ON sh.id = s.shareholder_id
                    LEFT JOIN shareholder_companies shc
                        ON shc.shareholder_id = sh.id AND shc.company_id = s.company_id
                    JOIN share_classes sc ON sc.id = s.share_class_id
                    JOIN companies c ON c.id = s.company_id
                    WHERE s.client_id = @ClientId
                      AND s.company_id = @CompanyId
                      AND s.is_deleted = 0
                      AND s.status = 'Active'
                    ORDER BY sh.name, sc.code";

        var rows = await Connection.QueryAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, Transaction);

        return rows.Select(MapToShare);
    }

    public async Task<Share?> GetByIdAsync(Guid id, Guid clientId)
    {
        var sql = $@"SELECT {SelectColumns}
                    FROM shares s
                    JOIN shareholders sh ON sh.id = s.shareholder_id
                    LEFT JOIN shareholder_companies shc
                        ON shc.shareholder_id = sh.id AND shc.company_id = s.company_id
                    JOIN share_classes sc ON sc.id = s.share_class_id
                    JOIN companies c ON c.id = s.company_id
                    WHERE s.id = @Id AND s.client_id = @ClientId AND s.is_deleted = 0";

        var share = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);

        return share == null ? null : MapToShare(share);
    }

    public async Task<decimal> GetShareholderBalanceAsync(Guid clientId, Guid shareholderId, Guid shareClassId)
    {
        var sql = @"SELECT COALESCE(SUM(quantity), 0) 
                    FROM shares 
                    WHERE client_id = @ClientId 
                      AND shareholder_id = @ShareholderId 
                      AND share_class_id = @ShareClassId
                      AND is_deleted = 0
                      AND status = 'Active'";

        var balance = await Connection.ExecuteScalarAsync<decimal>(sql, new
        {
            ClientId = clientId.ToString(),
            ShareholderId = shareholderId.ToString(),
            ShareClassId = shareClassId.ToString()
        }, Transaction);

        return balance;
    }

    public async Task<decimal> GetTotalSharesByCompanyAsync(Guid clientId, Guid companyId)
    {
        var sql = @"SELECT COALESCE(SUM(quantity), 0) 
                    FROM shares 
                    WHERE client_id = @ClientId 
                      AND company_id = @CompanyId
                      AND is_deleted = 0
                      AND status = 'Active'";

        var total = await Connection.ExecuteScalarAsync<decimal>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString()
        }, Transaction);

        return total;
    }

    public async Task<decimal> GetTotalSharesByClassAsync(Guid clientId, Guid shareClassId)
    {
        var sql = @"SELECT COALESCE(SUM(quantity), 0) 
                    FROM shares 
                    WHERE client_id = @ClientId 
                      AND share_class_id = @ShareClassId
                      AND is_deleted = 0
                      AND status = 'Active'";

        var total = await Connection.ExecuteScalarAsync<decimal>(sql, new
        {
            ClientId = clientId.ToString(),
            ShareClassId = shareClassId.ToString()
        }, Transaction);

        return total;
    }

    public async Task AddAsync(Share share)
    {
        var sql = @"INSERT INTO shares
                    (id, client_id, company_id, shareholder_id, share_class_id, certificate_number,
                     quantity, acquisition_price, acquisition_date, origin, origin_transaction_id,
                     status, notes, created_at, updated_at, created_by, updated_by, is_deleted)
                    VALUES
                    (@Id, @ClientId, @CompanyId, @ShareholderId, @ShareClassId, @CertificateNumber,
                     @Quantity, @AcquisitionPrice, @AcquisitionDate, @Origin, @OriginTransactionId,
                     @Status, @Notes, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted)";

        await Connection.ExecuteAsync(sql, new
        {
            Id = share.Id.ToString(),
            ClientId = share.ClientId.ToString(),
            CompanyId = share.CompanyId.ToString(),
            ShareholderId = share.ShareholderId.ToString(),
            ShareClassId = share.ShareClassId.ToString(),
            share.CertificateNumber,
            share.Quantity,
            share.AcquisitionPrice,
            share.AcquisitionDate,
            Origin = share.Origin.ToString(),
            OriginTransactionId = share.OriginTransactionId?.ToString(),
            Status = share.Status.ToString(),
            share.Notes,
            share.CreatedAt,
            share.UpdatedAt,
            CreatedBy = share.CreatedBy?.ToString(),
            UpdatedBy = share.UpdatedBy?.ToString(),
            share.IsDeleted
        }, Transaction);
    }

    public async Task UpdateAsync(Share share)
    {
        var sql = @"UPDATE shares SET
                    certificate_number = @CertificateNumber,
                    origin_transaction_id = @OriginTransactionId,
                    status = @Status,
                    notes = @Notes,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                    WHERE id = @Id AND client_id = @ClientId";

        await Connection.ExecuteAsync(sql, new
        {
            Id = share.Id.ToString(),
            ClientId = share.ClientId.ToString(),
            share.CertificateNumber,
            OriginTransactionId = share.OriginTransactionId?.ToString(),
            Status = share.Status.ToString(),
            share.Notes,
            share.UpdatedAt,
            UpdatedBy = share.UpdatedBy?.ToString()
        }, Transaction);
    }

    public async Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null)
    {
        var sql = @"UPDATE shares SET 
                    is_deleted = 1, 
                    deleted_at = @DeletedAt, 
                    updated_by = @DeletedBy
                    WHERE id = @Id AND client_id = @ClientId";

        await Connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString(),
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy?.ToString()
        }, Transaction);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid clientId)
    {
        var sql = @"SELECT COUNT(1) FROM shares 
                    WHERE id = @Id AND client_id = @ClientId AND is_deleted = 0";

        var count = await Connection.ExecuteScalarAsync<int>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);

        return count > 0;
    }

    private static Share MapToShare(dynamic row)
    {
        var share = Share.Create(
            ParseGuid(row.ClientId),
            ParseGuid(row.CompanyId),
            ParseGuid(row.ShareholderId),
            ParseGuid(row.ShareClassId),
            row.Quantity,
            row.AcquisitionPrice,
            row.AcquisitionDate,
            Enum.Parse<ShareOrigin>(row.Origin?.ToString() ?? "Issue"),
            row.CertificateNumber,
            row.OriginTransactionId != null ? ParseGuid(row.OriginTransactionId) : null,
            row.Notes,
            row.CreatedBy != null ? ParseGuid(row.CreatedBy) : null
        );

        // Use reflection to set readonly properties
        SetProperty(share, "Id", ParseGuid(row.Id));
        SetProperty(share, "Status", Enum.Parse<ShareStatus>(row.Status?.ToString() ?? "Active"));
        SetProperty(share, "CreatedAt", row.CreatedAt);
        SetProperty(share, "UpdatedAt", row.UpdatedAt);
        SetProperty(share, "UpdatedBy", row.UpdatedBy != null ? ParseGuid(row.UpdatedBy) : null);
        SetProperty(share, "IsDeleted", ParseBool(row.IsDeleted));
        SetProperty(share, "DeletedAt", row.DeletedAt);
        SetProperty(share, "ShareholderType", Enum.Parse<ShareholderType>(row.ShareholderType.ToString(), true));
        
        // Navigation properties
        SetProperty(share, "ShareholderName", row.ShareholderName);
        SetProperty(share, "ShareClassName", row.ShareClassName);
        SetProperty(share, "ShareClassCode", row.ShareClassCode);
        SetProperty(share, "CompanyName", row.CompanyName);

        return share;
    }

    private static Guid ParseGuid(object value)
    {
        if (value is Guid guid) return guid;
        if (value is string str && Guid.TryParse(str, out var parsed)) return parsed;
        return Guid.Empty;
    }

    private static bool ParseBool(object value)
    {
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is sbyte sb) return sb != 0;
        if (value is long l) return l != 0;
        return false;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property?.SetValue(obj, value);
    }
}
