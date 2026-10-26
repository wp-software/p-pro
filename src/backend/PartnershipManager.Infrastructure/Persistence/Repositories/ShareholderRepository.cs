using System.Data;
using System.Text;
using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

public class ShareholderRepository : IShareholderRepository
{
    private readonly DapperContext _context;

    public ShareholderRepository(DapperContext context)
    {
        _context = context;
    }

    private IDbConnection Connection => _context.Connection;
    private IDbTransaction? Transaction => _context.Transaction;

    public async Task<(IEnumerable<Shareholder> Items, int Total)> GetPagedAsync(
        Guid clientId,
        Guid? companyId,
        int page,
        int pageSize,
        string? search = null,
        string? type = null,
        string? status = null)
    {
        var baseQuery = new StringBuilder(@"FROM shareholders sh
                    JOIN companies c ON c.id = sh.company_id
                    WHERE sh.is_deleted = 0
                      AND sh.client_id = @ClientId");

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", clientId.ToString());

        if (companyId.HasValue)
        {
            baseQuery.Append(" AND EXISTS (SELECT 1 FROM shareholder_companies sc WHERE sc.shareholder_id = sh.id AND sc.company_id = @CompanyId)");
            parameters.Add("CompanyId", companyId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery.Append(" AND (LOWER(sh.name) LIKE @Search OR sh.document LIKE @Search OR LOWER(sh.email) LIKE @Search)");
            parameters.Add("Search", $"%{search.ToLower()}%");
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            baseQuery.Append(" AND sh.type = @Type");
            parameters.Add("Type", type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery.Append(" AND sh.status = @Status");
            parameters.Add("Status", status);
        }

        var countSql = $"SELECT COUNT(1) {baseQuery}";
        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);

        baseQuery.Append(" ORDER BY sh.created_at DESC LIMIT @Offset, @PageSize");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var sql = $@"SELECT 
                        sh.id AS Id,
                        sh.client_id AS ClientId,
                        sh.company_id AS CompanyId,
                        sh.name AS Name,
                        sh.document AS Document,
                        sh.document_type AS DocumentType,
                        sh.email AS Email,
                        sh.phone AS Phone,
                        sh.type AS Type,
                        sh.status AS Status,
                        sh.notes AS Notes,
                        sh.address_street AS AddressStreet,
                        sh.address_number AS AddressNumber,
                        sh.address_complement AS AddressComplement,
                        sh.address_zip_code AS AddressZipCode,
                        sh.address_city AS AddressCity,
                        sh.address_state AS AddressState,
                        sh.marital_status AS MaritalStatus,
                        sh.gender AS Gender,
                        sh.birth_date AS BirthDate,
                        sh.earn_out AS EarnOut,
                        sh.tag_along AS TagAlong,
                        sh.drag_along AS DragAlong,
                        sh.shareholders_agreement AS ShareholdersAgreement,
                        sh.right_of_first_refusal AS RightOfFirstRefusal,
                        sh.liquidation_preference_right AS LiquidationPreferenceRight,
                        sh.anti_dilution AS AntiDilution,
                        sh.created_at AS CreatedAt,
                        sh.updated_at AS UpdatedAt,
                        sh.created_by AS CreatedBy,
                        sh.updated_by AS UpdatedBy,
                        sh.is_deleted AS IsDeleted,
                        sh.deleted_at AS DeletedAt,
                        c.name AS CompanyName
                    {baseQuery}";

        var rows = await Connection.QueryAsync<dynamic>(sql, parameters, Transaction);
        var items = rows.Select(MapToShareholder);

        return (items, total);
    }

    public async Task<Shareholder?> GetByIdAsync(Guid id, Guid clientId)
    {
        var sql = @"SELECT 
                        sh.id AS Id,
                        sh.client_id AS ClientId,
                        sh.company_id AS CompanyId,
                        sh.name AS Name,
                        sh.document AS Document,
                        sh.document_type AS DocumentType,
                        sh.email AS Email,
                        sh.phone AS Phone,
                        sh.type AS Type,
                        sh.status AS Status,
                        sh.notes AS Notes,
                        sh.address_street AS AddressStreet,
                        sh.address_number AS AddressNumber,
                        sh.address_complement AS AddressComplement,
                        sh.address_zip_code AS AddressZipCode,
                        sh.address_city AS AddressCity,
                        sh.address_state AS AddressState,
                        sh.marital_status AS MaritalStatus,
                        sh.gender AS Gender,
                        sh.birth_date AS BirthDate,
                        sh.earn_out AS EarnOut,
                        sh.tag_along AS TagAlong,
                        sh.drag_along AS DragAlong,
                        sh.shareholders_agreement AS ShareholdersAgreement,
                        sh.right_of_first_refusal AS RightOfFirstRefusal,
                        sh.liquidation_preference_right AS LiquidationPreferenceRight,
                        sh.anti_dilution AS AntiDilution,
                        sh.created_at AS CreatedAt,
                        sh.updated_at AS UpdatedAt,
                        sh.created_by AS CreatedBy,
                        sh.updated_by AS UpdatedBy,
                        sh.is_deleted AS IsDeleted,
                        sh.deleted_at AS DeletedAt,
                        c.name AS CompanyName
                    FROM shareholders sh
                    JOIN companies c ON c.id = sh.company_id
                    WHERE sh.id = @Id AND sh.client_id = @ClientId AND sh.is_deleted = 0";

        var shareholder = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);

        return shareholder == null ? null : MapToShareholder(shareholder);
    }

    public async Task<Shareholder?> GetByDocumentAsync(Guid clientId, string document)
    {
        var normalized = new string(document.Where(char.IsDigit).ToArray());

        var sql = @"SELECT 
                        sh.id AS Id,
                        sh.client_id AS ClientId,
                        sh.company_id AS CompanyId,
                        sh.name AS Name,
                        sh.document AS Document,
                        sh.document_type AS DocumentType,
                        sh.email AS Email,
                        sh.phone AS Phone,
                        sh.type AS Type,
                        sh.status AS Status,
                        sh.notes AS Notes,
                        sh.address_street AS AddressStreet,
                        sh.address_number AS AddressNumber,
                        sh.address_complement AS AddressComplement,
                        sh.address_zip_code AS AddressZipCode,
                        sh.address_city AS AddressCity,
                        sh.address_state AS AddressState,
                        sh.marital_status AS MaritalStatus,
                        sh.gender AS Gender,
                        sh.birth_date AS BirthDate,
                        sh.earn_out AS EarnOut,
                        sh.tag_along AS TagAlong,
                        sh.drag_along AS DragAlong,
                        sh.shareholders_agreement AS ShareholdersAgreement,
                        sh.right_of_first_refusal AS RightOfFirstRefusal,
                        sh.liquidation_preference_right AS LiquidationPreferenceRight,
                        sh.anti_dilution AS AntiDilution,
                        sh.created_at AS CreatedAt,
                        sh.updated_at AS UpdatedAt,
                        sh.created_by AS CreatedBy,
                        sh.updated_by AS UpdatedBy,
                        sh.is_deleted AS IsDeleted,
                        sh.deleted_at AS DeletedAt,
                        c.name AS CompanyName
                    FROM shareholders sh
                    JOIN companies c ON c.id = sh.company_id
                    WHERE sh.client_id = @ClientId
                      AND sh.document = @Document
                      AND sh.is_deleted = 0";

        var shareholder = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            Document = normalized
        }, Transaction);

        return shareholder == null ? null : MapToShareholder(shareholder);
    }

    public async Task<Shareholder?> GetByEmailAsync(Guid clientId, Guid companyId, string email)
    {
        var sql = @"SELECT 
                        sh.id AS Id,
                        sh.client_id AS ClientId,
                        sh.company_id AS CompanyId,
                        sh.name AS Name,
                        sh.document AS Document,
                        sh.document_type AS DocumentType,
                        sh.email AS Email,
                        sh.phone AS Phone,
                        sh.type AS Type,
                        sh.status AS Status,
                        sh.notes AS Notes,
                        sh.address_street AS AddressStreet,
                        sh.address_number AS AddressNumber,
                        sh.address_complement AS AddressComplement,
                        sh.address_zip_code AS AddressZipCode,
                        sh.address_city AS AddressCity,
                        sh.address_state AS AddressState,
                        sh.marital_status AS MaritalStatus,
                        sh.gender AS Gender,
                        sh.birth_date AS BirthDate,
                        sh.earn_out AS EarnOut,
                        sh.tag_along AS TagAlong,
                        sh.drag_along AS DragAlong,
                        sh.shareholders_agreement AS ShareholdersAgreement,
                        sh.right_of_first_refusal AS RightOfFirstRefusal,
                        sh.liquidation_preference_right AS LiquidationPreferenceRight,
                        sh.anti_dilution AS AntiDilution,
                        sh.created_at AS CreatedAt,
                        sh.updated_at AS UpdatedAt,
                        sh.created_by AS CreatedBy,
                        sh.updated_by AS UpdatedBy,
                        sh.is_deleted AS IsDeleted,
                        sh.deleted_at AS DeletedAt,
                        c.name AS CompanyName
                    FROM shareholders sh
                    JOIN companies c ON c.id = sh.company_id
                    WHERE sh.client_id = @ClientId
                      AND sh.company_id = @CompanyId
                      AND LOWER(sh.email) = LOWER(@Email)
                      AND sh.is_deleted = 0
                    LIMIT 1";

        var shareholder = await Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            ClientId = clientId.ToString(),
            CompanyId = companyId.ToString(),
            Email = email
        }, Transaction);

        return shareholder == null ? null : MapToShareholder(shareholder);
    }

    public async Task<bool> DocumentExistsAsync(Guid clientId, string document, Guid? excludeId = null)
    {
        var normalized = new string(document.Where(char.IsDigit).ToArray());

        var sql = @"SELECT COUNT(1) FROM shareholders
                    WHERE client_id = @ClientId
                      AND document = @Document
                      AND is_deleted = 0
                      AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        var count = await Connection.ExecuteScalarAsync<int>(sql, new
        {
            ClientId = clientId.ToString(),
            Document = normalized,
            ExcludeId = excludeId?.ToString()
        }, Transaction);

        return count > 0;
    }

    public async Task AddAsync(Shareholder shareholder)
    {
        var sql = @"INSERT INTO shareholders
                (id, client_id, company_id, name, document, document_type, email, phone, type, status, notes,
                 address_street, address_number, address_complement, address_zip_code, address_city, address_state,
                 marital_status, gender, birth_date,
                 earn_out, tag_along, drag_along, shareholders_agreement,
                 right_of_first_refusal, liquidation_preference_right, anti_dilution,
                 created_at, updated_at, created_by, updated_by, is_deleted)
                VALUES
                (@Id, @ClientId, @CompanyId, @Name, @Document, @DocumentType, @Email, @Phone, @Type, @Status, @Notes,
                 @AddressStreet, @AddressNumber, @AddressComplement, @AddressZipCode, @AddressCity, @AddressState,
                 @MaritalStatus, @Gender, @BirthDate,
                 @EarnOut, @TagAlong, @DragAlong, @ShareholdersAgreement,
                 @RightOfFirstRefusal, @LiquidationPreferenceRight, @AntiDilution,
                 @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @IsDeleted)";

        await Connection.ExecuteAsync(sql, new
        {
            Id = shareholder.Id.ToString(),
            ClientId = shareholder.ClientId.ToString(),
            CompanyId = shareholder.CompanyId.ToString(),
            shareholder.Name,
            shareholder.Document,
            DocumentType = shareholder.DocumentType.ToString().ToLower(),
            shareholder.Email,
            shareholder.Phone,
            Type = shareholder.Type.ToString(),
            Status = shareholder.Status.ToString(),
            shareholder.Notes,
            shareholder.AddressStreet,
            shareholder.AddressNumber,
            shareholder.AddressComplement,
            AddressZipCode = shareholder.AddressZipCode,
            AddressCity = shareholder.AddressCity,
            AddressState = shareholder.AddressState,
            MaritalStatus = shareholder.MaritalStatus?.ToString(),
            Gender = shareholder.Gender?.ToString(),
            BirthDate = shareholder.BirthDate,
            EarnOut = shareholder.EarnOut,
            TagAlong = shareholder.TagAlong,
            DragAlong = shareholder.DragAlong,
            ShareholdersAgreement = shareholder.ShareholdersAgreement,
            RightOfFirstRefusal = shareholder.RightOfFirstRefusal,
            LiquidationPreferenceRight = shareholder.LiquidationPreferenceRight,
            AntiDilution = shareholder.AntiDilution,
            shareholder.CreatedAt,
            shareholder.UpdatedAt,
            CreatedBy = shareholder.CreatedBy?.ToString(),
            UpdatedBy = shareholder.UpdatedBy?.ToString(),
            shareholder.IsDeleted
        }, Transaction);
    }

    public async Task UpdateAsync(Shareholder shareholder)
    {
        var sql = @"UPDATE shareholders
                    SET company_id = @CompanyId,
                        name = @Name,
                        document = @Document,
                        document_type = @DocumentType,
                        email = @Email,
                        phone = @Phone,
                        type = @Type,
                        status = @Status,
                        notes = @Notes,
                        address_street = @AddressStreet,
                        address_number = @AddressNumber,
                        address_complement = @AddressComplement,
                        address_zip_code = @AddressZipCode,
                        address_city = @AddressCity,
                        address_state = @AddressState,
                        marital_status = @MaritalStatus,
                        gender = @Gender,
                        birth_date = @BirthDate,
                        earn_out = @EarnOut,
                        tag_along = @TagAlong,
                        drag_along = @DragAlong,
                        shareholders_agreement = @ShareholdersAgreement,
                        right_of_first_refusal = @RightOfFirstRefusal,
                        liquidation_preference_right = @LiquidationPreferenceRight,
                        anti_dilution = @AntiDilution,
                        updated_at = @UpdatedAt,
                        updated_by = @UpdatedBy
                    WHERE id = @Id AND client_id = @ClientId";

        await Connection.ExecuteAsync(sql, new
        {
            Id = shareholder.Id.ToString(),
            ClientId = shareholder.ClientId.ToString(),
            CompanyId = shareholder.CompanyId.ToString(),
            shareholder.Name,
            shareholder.Document,
            DocumentType = shareholder.DocumentType.ToString().ToLower(),
            shareholder.Email,
            shareholder.Phone,
            Type = shareholder.Type.ToString(),
            Status = shareholder.Status.ToString(),
            shareholder.Notes,
            shareholder.AddressStreet,
            shareholder.AddressNumber,
            shareholder.AddressComplement,
            AddressZipCode = shareholder.AddressZipCode,
            AddressCity = shareholder.AddressCity,
            AddressState = shareholder.AddressState,
            MaritalStatus = shareholder.MaritalStatus?.ToString(),
            Gender = shareholder.Gender?.ToString(),
            BirthDate = shareholder.BirthDate,
            EarnOut = shareholder.EarnOut,
            TagAlong = shareholder.TagAlong,
            DragAlong = shareholder.DragAlong,
            ShareholdersAgreement = shareholder.ShareholdersAgreement,
            RightOfFirstRefusal = shareholder.RightOfFirstRefusal,
            LiquidationPreferenceRight = shareholder.LiquidationPreferenceRight,
            AntiDilution = shareholder.AntiDilution,
            shareholder.UpdatedAt,
            UpdatedBy = shareholder.UpdatedBy?.ToString()
        }, Transaction);
    }

    public async Task SoftDeleteAsync(Guid id, Guid clientId, Guid? deletedBy = null)
    {
        var sql = @"UPDATE shareholders
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
        var sql = @"SELECT COUNT(1) FROM shareholders WHERE id = @Id AND client_id = @ClientId AND is_deleted = 0";
        var count = await Connection.ExecuteScalarAsync<int>(sql, new
        {
            Id = id.ToString(),
            ClientId = clientId.ToString()
        }, Transaction);
        return count > 0;
    }

    public async Task<Dictionary<Guid, List<ShareholderCompanyLink>>> GetCompanyLinksAsync(IEnumerable<Guid> shareholderIds)
    {
        var ids = shareholderIds.Select(i => i.ToString()).ToArray();
        var result = new Dictionary<Guid, List<ShareholderCompanyLink>>();
        if (ids.Length == 0) return result;

        var rows = await Connection.QueryAsync<dynamic>(
            @"SELECT sc.shareholder_id AS ShareholderId, sc.company_id AS CompanyId, c.name AS CompanyName,
                     sc.type AS Type, sc.earn_out AS EarnOut, sc.tag_along AS TagAlong, sc.drag_along AS DragAlong,
                     sc.shareholders_agreement AS ShareholdersAgreement,
                     sc.right_of_first_refusal AS RightOfFirstRefusal,
                     sc.liquidation_preference_right AS LiquidationPreferenceRight,
                     sc.anti_dilution AS AntiDilution
              FROM shareholder_companies sc
              JOIN companies c ON c.id = sc.company_id
              WHERE sc.shareholder_id IN @Ids
              ORDER BY c.name",
            new { Ids = ids }, Transaction);

        foreach (var row in rows)
        {
            var key = Guid.Parse((string)row.ShareholderId.ToString());
            if (!result.TryGetValue(key, out var list))
            {
                list = new List<ShareholderCompanyLink>();
                result[key] = list;
            }
            var companyId = Guid.Parse((string)row.CompanyId.ToString());
            var config = new ShareholderCompanyConfig(
                companyId,
                Enum.TryParse<ShareholderType>((string)row.Type.ToString(), true, out ShareholderType t) ? t : ShareholderType.Other,
                Convert.ToBoolean(row.EarnOut),
                Convert.ToBoolean(row.TagAlong),
                Convert.ToBoolean(row.DragAlong),
                Convert.ToBoolean(row.ShareholdersAgreement),
                Convert.ToBoolean(row.RightOfFirstRefusal),
                Convert.ToBoolean(row.LiquidationPreferenceRight),
                Convert.ToBoolean(row.AntiDilution));
            list.Add(new ShareholderCompanyLink(companyId, (string)row.CompanyName, config));
        }
        return result;
    }

    public async Task UpsertCompanyLinkAsync(Guid shareholderId, Guid clientId, ShareholderCompanyConfig config)
    {
        await Connection.ExecuteAsync(
            @"INSERT INTO shareholder_companies
                (shareholder_id, company_id, client_id, type, earn_out, tag_along, drag_along,
                 shareholders_agreement, right_of_first_refusal, liquidation_preference_right, anti_dilution)
              VALUES
                (@ShareholderId, @CompanyId, @ClientId, @Type, @EarnOut, @TagAlong, @DragAlong,
                 @ShareholdersAgreement, @RightOfFirstRefusal, @LiquidationPreferenceRight, @AntiDilution)
              ON DUPLICATE KEY UPDATE
                type = VALUES(type), earn_out = VALUES(earn_out), tag_along = VALUES(tag_along),
                drag_along = VALUES(drag_along), shareholders_agreement = VALUES(shareholders_agreement),
                right_of_first_refusal = VALUES(right_of_first_refusal),
                liquidation_preference_right = VALUES(liquidation_preference_right),
                anti_dilution = VALUES(anti_dilution)",
            new
            {
                ShareholderId = shareholderId.ToString(),
                CompanyId = config.CompanyId.ToString(),
                ClientId = clientId.ToString(),
                Type = config.Type.ToString(),
                config.EarnOut,
                config.TagAlong,
                config.DragAlong,
                config.ShareholdersAgreement,
                config.RightOfFirstRefusal,
                config.LiquidationPreferenceRight,
                config.AntiDilution
            },
            Transaction);
    }

    public async Task<bool> HasPositionInCompanyAsync(Guid shareholderId, Guid companyId)
    {
        var count = await Connection.ExecuteScalarAsync<int>(
            @"SELECT (SELECT COUNT(1) FROM shares WHERE shareholder_id = @ShareholderId AND company_id = @CompanyId AND is_deleted = 0)
                   + (SELECT COUNT(1) FROM vesting_grants WHERE shareholder_id = @ShareholderId AND company_id = @CompanyId AND is_deleted = 0)",
            new { ShareholderId = shareholderId.ToString(), CompanyId = companyId.ToString() }, Transaction);
        return count > 0;
    }

    public async Task RemoveCompanyLinkAsync(Guid shareholderId, Guid companyId)
    {
        await Connection.ExecuteAsync(
            "DELETE FROM shareholder_companies WHERE shareholder_id = @ShareholderId AND company_id = @CompanyId",
            new { ShareholderId = shareholderId.ToString(), CompanyId = companyId.ToString() },
            Transaction);
    }

    private static Shareholder MapToShareholder(dynamic row)
    {
        // Converter GUIDs - Dapper pode retornar como Guid ou string dependendo do driver
        Guid ParseGuid(object value) => value is Guid g ? g : Guid.Parse(value.ToString()!);
        Guid? ParseNullableGuid(object? value) => value == null ? null : (value is Guid g ? g : Guid.Parse(value.ToString()!));
        
        var shareholder = Shareholder.Create(
            ParseGuid(row.ClientId),
            ParseGuid(row.CompanyId),
            (string)row.Name,
            (string)row.Document,
            Enum.Parse<DocumentType>((string)row.DocumentType, true),
            Enum.Parse<ShareholderType>((string)row.Type, true),
            (string?)row.Email,
            (string?)row.Phone,
            Enum.Parse<ShareholderStatus>((string)row.Status, true),
            (string?)row.Notes,
            (string?)row.AddressStreet,
            (string?)row.AddressNumber,
            (string?)row.AddressComplement,
            (string?)row.AddressZipCode,
            (string?)row.AddressCity,
            (string?)row.AddressState,
            row.MaritalStatus == null ? (MaritalStatus?)null : Enum.Parse<MaritalStatus>((string)row.MaritalStatus, true),
            row.Gender == null ? (Gender?)null : Enum.Parse<Gender>((string)row.Gender, true),
            row.BirthDate as DateTime?,
            row.EarnOut is bool earnOut ? earnOut : Convert.ToInt32(row.EarnOut) != 0,
            row.TagAlong is bool tagAlong ? tagAlong : Convert.ToInt32(row.TagAlong) != 0,
            row.DragAlong is bool dragAlong ? dragAlong : Convert.ToInt32(row.DragAlong) != 0,
            row.ShareholdersAgreement is bool shAgreement ? shAgreement : Convert.ToInt32(row.ShareholdersAgreement) != 0,
            row.RightOfFirstRefusal is bool rofr ? rofr : Convert.ToInt32(row.RightOfFirstRefusal) != 0,
            row.LiquidationPreferenceRight is bool liqPref ? liqPref : Convert.ToInt32(row.LiquidationPreferenceRight) != 0,
            row.AntiDilution is bool antiDil ? antiDil : Convert.ToInt32(row.AntiDilution) != 0,
            ParseNullableGuid(row.CreatedBy));

        shareholder.Id = ParseGuid(row.Id);
        shareholder.CreatedAt = (DateTime)row.CreatedAt;
        shareholder.UpdatedAt = (DateTime)row.UpdatedAt;
        shareholder.IsDeleted = row.IsDeleted is bool b ? b : row.IsDeleted == 1;
        shareholder.DeletedAt = row.DeletedAt as DateTime?;
        shareholder.SetCompanyName((string?)row.CompanyName);

        return shareholder;
    }
}
