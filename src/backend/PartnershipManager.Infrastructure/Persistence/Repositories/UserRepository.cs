using System.Data;
using Dapper;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de usuários
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    protected override string TableName => "users";

    private const string SelectColumns = @"
        id AS Id,
        client_id AS ClientId,
        company_id AS CompanyId,
        shareholder_id AS ShareholderId,
        email AS Email,
        name AS Name,
        password_hash AS PasswordHash,
        avatar_url AS AvatarUrl,
        phone AS Phone,
        status AS Status,
        language AS Language,
        timezone AS Timezone,
        preferences AS Preferences,
        two_factor_enabled AS TwoFactorEnabled,
        two_factor_secret AS TwoFactorSecret,
        last_login_at AS LastLoginAt,
        failed_login_attempts AS FailedLoginAttempts,
        lockout_end AS LockoutEnd,
        refresh_token AS RefreshToken,
        refresh_token_expiry AS RefreshTokenExpiry,
        password_reset_token AS PasswordResetToken,
        password_reset_token_expiry AS PasswordResetTokenExpiry,
        created_at AS CreatedAt,
        updated_at AS UpdatedAt,
        created_by AS CreatedBy,
        updated_by AS UpdatedBy,
        is_deleted AS IsDeleted,
        deleted_at AS DeletedAt";

    public UserRepository(DapperContext context) : base(context) { }

    public override async Task<User?> GetByIdAsync(Guid id)
    {
        var sql = $"SELECT {SelectColumns} FROM users WHERE id = @Id AND is_deleted = 0";
        return await Connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id.ToString() }, Transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, Guid? companyId = null)
    {
        string sql;
        object parameters;
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            sql = $@"SELECT {SelectColumns} 
                     FROM users 
                     WHERE LOWER(email) = LOWER(@Email) AND company_id = @CompanyId AND is_deleted = 0";
            parameters = new { Email = email, CompanyId = companyId.Value.ToString() };
        }
        else
        {
            sql = $@"SELECT {SelectColumns} 
                     FROM users 
                     WHERE LOWER(email) = LOWER(@Email) AND is_deleted = 0";
            parameters = new { Email = email };
        }
        
        return await Connection.QueryFirstOrDefaultAsync<User>(sql, parameters, Transaction);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid companyId, Guid? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM users WHERE LOWER(email) = LOWER(@Email) AND company_id = @CompanyId AND id != @ExcludeId AND is_deleted = 0"
            : "SELECT COUNT(1) FROM users WHERE LOWER(email) = LOWER(@Email) AND company_id = @CompanyId AND is_deleted = 0";
        
        var count = await Connection.ExecuteScalarAsync<int>(sql, 
            new { Email = email, CompanyId = companyId.ToString(), ExcludeId = excludeId?.ToString() }, Transaction);
        return count > 0;
    }

    public async Task<IEnumerable<User>> GetByCompanyAsync(Guid companyId)
    {
        var sql = $"SELECT {SelectColumns} FROM users WHERE company_id = @CompanyId AND is_deleted = 0 ORDER BY name";
        return await Connection.QueryAsync<User>(sql, new { CompanyId = companyId.ToString() }, Transaction);
    }

    public async Task<IEnumerable<User>> GetActiveUsersByCompanyAsync(Guid companyId)
    {
        var sql = $@"SELECT {SelectColumns} 
                     FROM users 
                     WHERE company_id = @CompanyId AND status = 'Active' AND is_deleted = 0 
                     ORDER BY name";
        return await Connection.QueryAsync<User>(sql, new { CompanyId = companyId.ToString() }, Transaction);
    }

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search = null)
    {
        var offset = (page - 1) * pageSize;
        
        var whereClause = "WHERE company_id = @CompanyId AND is_deleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (name LIKE @Search OR email LIKE @Search)";
        }

        var countSql = $"SELECT COUNT(*) FROM users {whereClause}";
        var dataSql = $@"SELECT {SelectColumns}
                         FROM users {whereClause}
                         ORDER BY name
                         LIMIT @PageSize OFFSET @Offset";

        var parameters = new 
        { 
            CompanyId = companyId.ToString(), 
            Search = $"%{search}%", 
            PageSize = pageSize, 
            Offset = offset 
        };

        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);
        var items = await Connection.QueryAsync<User>(dataSql, parameters, Transaction);

        return (items, total);
    }

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedByClientAsync(
        Guid clientId, int page, int pageSize, string? search = null)
    {
        var whereClause = "WHERE client_id = @ClientId AND is_deleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (name LIKE @Search OR email LIKE @Search)";
        }

        var countSql = $"SELECT COUNT(*) FROM users {whereClause}";
        var dataSql = $@"SELECT {SelectColumns}
                         FROM users {whereClause}
                         ORDER BY name
                         LIMIT @PageSize OFFSET @Offset";

        var parameters = new
        {
            ClientId = clientId.ToString(),
            Search = $"%{search}%",
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        };

        var total = await Connection.ExecuteScalarAsync<int>(countSql, parameters, Transaction);
        var items = await Connection.QueryAsync<User>(dataSql, parameters, Transaction);

        return (items, total);
    }

    public async Task AddAsync(User user)
    {
        var sql = @"INSERT INTO users 
                    (id, client_id, company_id, shareholder_id, email, name, password_hash, avatar_url, phone,
                     status, language, timezone, preferences, two_factor_enabled,
                     created_at, updated_at, created_by)
                    VALUES 
                    (@Id, @ClientId, @CompanyId, @ShareholderId, @Email, @Name, @PasswordHash, @AvatarUrl, @Phone,
                     @Status, @Language, @Timezone, @Preferences, @TwoFactorEnabled,
                     @CreatedAt, @UpdatedAt, @CreatedBy)";

        await Connection.ExecuteAsync(sql, new
        {
            Id = user.Id.ToString(),
            ClientId = user.ClientId.ToString(),
            CompanyId = user.CompanyId?.ToString(),
            ShareholderId = user.ShareholderId?.ToString(),
            user.Email,
            user.Name,
            user.PasswordHash,
            user.AvatarUrl,
            user.Phone,
            Status = user.Status.ToString(),
            Language = user.Language.ToString(),
            user.Timezone,
            user.Preferences,
            user.TwoFactorEnabled,
            user.CreatedAt,
            user.UpdatedAt,
            CreatedBy = user.CreatedBy?.ToString()
        }, Transaction);
    }

    public async Task UpdateAsync(User user)
    {
        var sql = @"UPDATE users SET
                        name = @Name,
                        shareholder_id = @ShareholderId,
                        avatar_url = @AvatarUrl,
                        phone = @Phone,
                        status = @Status,
                        language = @Language,
                        timezone = @Timezone,
                        preferences = @Preferences,
                        password_hash = @PasswordHash,
                        two_factor_enabled = @TwoFactorEnabled,
                        two_factor_secret = @TwoFactorSecret,
                        updated_at = @UpdatedAt,
                        updated_by = @UpdatedBy
                    WHERE id = @Id";

        await Connection.ExecuteAsync(sql, new
        {
            Id = user.Id.ToString(),
            ShareholderId = user.ShareholderId?.ToString(),
            user.Name,
            user.AvatarUrl,
            user.Phone,
            Status = user.Status.ToString(),
            Language = user.Language.ToString(),
            user.Timezone,
            user.Preferences,
            user.PasswordHash,
            user.TwoFactorEnabled,
            user.TwoFactorSecret,
            user.UpdatedAt,
            UpdatedBy = user.UpdatedBy?.ToString()
        }, Transaction);
    }

    public async Task UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiry)
    {
        var sql = @"UPDATE users SET
                        refresh_token = @RefreshToken,
                        refresh_token_expiry = @RefreshTokenExpiry,
                        updated_at = @UpdatedAt
                    WHERE id = @Id";

        await Connection.ExecuteAsync(sql, new
        {
            Id = userId.ToString(),
            RefreshToken = refreshToken,
            RefreshTokenExpiry = expiry,
            UpdatedAt = DateTime.UtcNow
        }, Transaction);
    }

    public async Task UpdateLoginInfoAsync(Guid userId, bool success)
    {
        if (success)
        {
            var sql = @"UPDATE users SET
                            last_login_at = @LastLoginAt,
                            failed_login_attempts = 0,
                            lockout_end = NULL,
                            updated_at = @UpdatedAt
                        WHERE id = @Id";

            await Connection.ExecuteAsync(sql, new
            {
                Id = userId.ToString(),
                LastLoginAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, Transaction);
        }
        else
        {
            var sql = @"UPDATE users SET
                            failed_login_attempts = failed_login_attempts + 1,
                            lockout_end = CASE 
                                WHEN failed_login_attempts + 1 >= @MaxAttempts 
                                THEN DATE_ADD(NOW(), INTERVAL @LockoutMinutes MINUTE)
                                ELSE lockout_end 
                            END,
                            updated_at = @UpdatedAt
                        WHERE id = @Id";

            await Connection.ExecuteAsync(sql, new
            {
                Id = userId.ToString(),
                MaxAttempts = SystemConstants.MaxLoginAttempts,
                LockoutMinutes = SystemConstants.LockoutMinutes,
                UpdatedAt = DateTime.UtcNow
            }, Transaction);
        }
    }

    public async Task<User?> GetByShareholderIdAsync(Guid shareholderId)
    {
        var sql = $"SELECT {SelectColumns} FROM users WHERE shareholder_id = @ShareholderId AND is_deleted = 0";
        return await Connection.QueryFirstOrDefaultAsync<User>(
            sql, new { ShareholderId = shareholderId.ToString() }, Transaction);
    }

    // Libera o vínculo para que o sócio possa ser associado a outro usuário
    public override async Task SoftDeleteAsync(Guid id, Guid? deletedBy = null)
    {
        await base.SoftDeleteAsync(id, deletedBy);
        await Connection.ExecuteAsync(
            "UPDATE users SET shareholder_id = NULL WHERE id = @Id",
            new { Id = id.ToString() }, Transaction);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        var sql = $@"SELECT {SelectColumns} 
                     FROM users 
                     WHERE refresh_token = @RefreshToken 
                       AND refresh_token_expiry > UTC_TIMESTAMP() 
                       AND is_deleted = 0";
        
        return await Connection.QueryFirstOrDefaultAsync<User>(sql, 
            new { RefreshToken = HashRefreshToken(refreshToken) }, Transaction);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(refreshToken)))
            .ToLowerInvariant();
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string hashedToken)
    {
        var sql = $@"SELECT {SelectColumns}
                     FROM users
                     WHERE password_reset_token = @Token
                       AND is_deleted = 0";

        return await Connection.QueryFirstOrDefaultAsync<User>(sql,
            new { Token = hashedToken }, Transaction);
    }

    public async Task UpdatePasswordResetTokenAsync(Guid userId, string? hashedToken, DateTime? expiry)
    {
        var sql = @"UPDATE users SET
                        password_reset_token        = @Token,
                        password_reset_token_expiry = @Expiry,
                        updated_at                  = @UpdatedAt
                    WHERE id = @Id";

        await Connection.ExecuteAsync(sql, new
        {
            Id        = userId.ToString(),
            Token     = hashedToken,
            Expiry    = expiry,
            UpdatedAt = DateTime.UtcNow
        }, Transaction);
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash)
    {
        var sql = @"UPDATE users SET
                        password_hash = @PasswordHash,
                        updated_at    = @UpdatedAt
                    WHERE id = @Id";

        await Connection.ExecuteAsync(sql, new
        {
            Id           = userId.ToString(),
            PasswordHash = passwordHash,
            UpdatedAt    = DateTime.UtcNow
        }, Transaction);
    }

    public async Task DeleteAsync(Guid id)
    {
        await SoftDeleteAsync(id);
    }
}
