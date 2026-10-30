namespace PartnershipManager.Application.Features.Auth.DTOs;

/// <summary>
/// DTO para login
/// </summary>
public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public Guid? CompanyId { get; init; }
}

/// <summary>
/// DTO para resposta de autenticação
/// </summary>
public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// DTO para informações do usuário logado
/// </summary>
public record UserInfo
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public Guid? ShareholderId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string Language { get; init; } = "pt";
}

/// <summary>
/// DTO para refresh token
/// </summary>
public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// DTO para registro de novo usuário
/// </summary>
public record RegisterRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }
}

/// <summary>
/// DTO para alteração de senha
/// </summary>
public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

/// <summary>
/// DTO para recuperação de senha
/// </summary>
public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public Guid? CompanyId { get; init; }
}

/// <summary>
/// DTO para reset de senha
/// </summary>
public record ResetPasswordRequest
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
