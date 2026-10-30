using Dapper;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Persistence;

namespace PartnershipManager.Infrastructure.Repositories;

public class ApprovalGroupRepository : IApprovalGroupRepository
{
    private const string GroupColumns = @"id AS Id, company_id AS CompanyId, name AS Name, description AS Description,
        approval_mode AS ApprovalMode, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt";

    private readonly DapperContext _context;

    public ApprovalGroupRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApprovalGroup>> GetByCompanyAsync(Guid companyId)
    {
        var groups = (await _context.Connection.QueryAsync<ApprovalGroup>(
            $"SELECT {GroupColumns} FROM approval_groups WHERE company_id = @CompanyId ORDER BY name",
            new { CompanyId = companyId })).ToList();
        if (groups.Count == 0) return groups;

        var members = (await _context.Connection.QueryAsync<ApprovalGroupMember>(@"
            SELECT m.group_id AS GroupId, m.user_id AS UserId, u.name AS UserName, u.email AS UserEmail
            FROM approval_group_members m
            INNER JOIN users u ON u.id = m.user_id
            WHERE m.group_id IN @Ids ORDER BY u.name",
            new { Ids = groups.Select(g => g.Id).ToArray() })).ToList();

        foreach (var g in groups)
            g.Members = members.Where(m => m.GroupId == g.Id).ToList();
        return groups;
    }

    public async Task<ApprovalGroup?> GetByIdAsync(Guid id, Guid companyId)
    {
        var group = await _context.Connection.QueryFirstOrDefaultAsync<ApprovalGroup>(
            $"SELECT {GroupColumns} FROM approval_groups WHERE id = @Id AND company_id = @CompanyId",
            new { Id = id, CompanyId = companyId });
        if (group == null) return null;

        group.Members = (await _context.Connection.QueryAsync<ApprovalGroupMember>(@"
            SELECT m.group_id AS GroupId, m.user_id AS UserId, u.name AS UserName, u.email AS UserEmail
            FROM approval_group_members m
            INNER JOIN users u ON u.id = m.user_id
            WHERE m.group_id = @Id ORDER BY u.name", new { Id = id })).ToList();
        return group;
    }

    public async Task<Guid> CreateAsync(ApprovalGroup group, IEnumerable<Guid> memberIds)
    {
        using var tx = await _context.BeginTransactionAsync();
        try
        {
            await _context.Connection.ExecuteAsync(@"
                INSERT INTO approval_groups (id, company_id, name, description, approval_mode, is_active,
                    created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @CompanyId, @Name, @Description, @ApprovalMode, @IsActive,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy)", group, tx);
            await InsertMembersAsync(group.Id, memberIds, tx);
            _context.CommitTransaction();
            return group.Id;
        }
        catch
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public async Task UpdateAsync(ApprovalGroup group, IEnumerable<Guid> memberIds)
    {
        using var tx = await _context.BeginTransactionAsync();
        try
        {
            await _context.Connection.ExecuteAsync(@"
                UPDATE approval_groups
                SET name = @Name, description = @Description, approval_mode = @ApprovalMode,
                    is_active = @IsActive, updated_at = @UpdatedAt, updated_by = @UpdatedBy
                WHERE id = @Id AND company_id = @CompanyId", group, tx);
            await _context.Connection.ExecuteAsync(
                "DELETE FROM approval_group_members WHERE group_id = @Id", new { group.Id }, tx);
            await InsertMembersAsync(group.Id, memberIds, tx);
            _context.CommitTransaction();
        }
        catch
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public Task DeleteAsync(Guid id, Guid companyId)
        => _context.Connection.ExecuteAsync(
            "DELETE FROM approval_groups WHERE id = @Id AND company_id = @CompanyId",
            new { Id = id, CompanyId = companyId });

    private async Task InsertMembersAsync(Guid groupId, IEnumerable<Guid> memberIds, System.Data.IDbTransaction tx)
    {
        foreach (var userId in memberIds.Distinct())
        {
            await _context.Connection.ExecuteAsync(
                "INSERT INTO approval_group_members (group_id, user_id) VALUES (@GroupId, @UserId)",
                new { GroupId = groupId, UserId = userId }, tx);
        }
    }
}
