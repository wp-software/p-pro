using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Application.Features.Users.DTOs;

/// <summary>
/// DTO para resposta de User
/// </summary>
public record UserResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public Guid? ShareholderId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public bool TwoFactorEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO para criação de User
/// </summary>
public record CreateUserRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public Role InitialRole { get; init; } = Role.Viewer;
    public Guid? ShareholderId { get; init; }
}

/// <summary>
/// DTO para atualização de User
/// </summary>
public record UpdateUserRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Vínculo com o sócio. Só é aplicado quando o chamador é Admin/SuperAdmin/HR;
    /// nesse caso, null remove o vínculo.
    /// </summary>
    public Guid? ShareholderId { get; init; }
}

/// <summary>
/// DTO para atualização de preferências
/// </summary>
public record UpdateUserPreferencesRequest
{
    public Language Language { get; init; }
    public string Timezone { get; init; } = "America/Sao_Paulo";
}

/// <summary>
/// DTO para gerenciamento de papel
/// </summary>
public record ManageUserRoleRequest
{
    public Role Role { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// DTO resumido de User (para listas)
/// </summary>
public record UserSummaryResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ShareholderId { get; init; }
    public string? ShareholderName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
