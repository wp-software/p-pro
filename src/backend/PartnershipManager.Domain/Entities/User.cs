using System.Text.RegularExpressions;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;

namespace PartnershipManager.Domain.Entities;

public partial class User : BaseEntity
{
    public Guid ClientId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ShareholderId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? Phone { get; private set; }
    public UserStatus Status { get; private set; }
    public Language Language { get; private set; }
    public string Timezone { get; private set; } = "America/Sao_Paulo";
    public string? Preferences { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    
    // Navigation properties
    public Client? Client { get; private set; }
    public Company? Company { get; private set; }
    
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    
    private User() { }
    
    public static User Create(
        Guid clientId,
        string email,
        string name,
        string passwordHash,
        Guid? companyId = null,
        Language language = Language.Portuguese)
    {
        if (clientId == Guid.Empty)
            throw new DomainException("ClientId is required");
            
        ValidateEmail(email);
        ValidateName(name);
        
        return new User
        {
            ClientId = clientId,
            CompanyId = companyId,
            Email = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Pending,
            Language = language,
            FailedLoginAttempts = 0
        };
    }
    
    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Block()
    {
        Status = UserStatus.Blocked;
        LockoutEnd = DateTime.UtcNow.AddDays(30);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unblock()
    {
        Status = UserStatus.Active;
        LockoutEnd = null;
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateProfile(string name, string? phone, string? avatarUrl)
    {
        ValidateName(name);
        Name = name.Trim();
        Phone = phone?.Trim();
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void LinkShareholder(Guid? shareholderId)
    {
        ShareholderId = shareholderId == Guid.Empty ? null : shareholderId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordLoginSuccess()
    {
        FailedLoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordLoginFailure()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= SystemConstants.MaxLoginAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(SystemConstants.LockoutMinutes);
        }
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetRefreshToken(string token, int days = 7)
    {
        RefreshToken = token;
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(days);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetPasswordResetToken(string hashedToken, DateTime expiry)
    {
        PasswordResetToken = hashedToken;
        PasswordResetTokenExpiry = expiry;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool IsPasswordResetTokenValid()
        => PasswordResetToken != null && PasswordResetTokenExpiry > DateTime.UtcNow;
    
    public bool IsRefreshTokenValid(string token) 
        => RefreshToken == token && RefreshTokenExpiry > DateTime.UtcNow;
    
    public int GetLockoutMinutesRemaining() 
        => IsLockedOut ? (int)(LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes : 0;
    
    public int GetLockoutRemainingMinutes() => GetLockoutMinutesRemaining();
    
    public void UpdatePreferences(Language language, string timezone, string? preferences)
    {
        Language = language;
        Timezone = timezone ?? "America/Sao_Paulo";
        Preferences = preferences;
        UpdatedAt = DateTime.UtcNow;
    }
    
    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException(ErrorMessages.EmailRequired);
        if (email.Length > SystemConstants.MaxEmailLength)
            throw new DomainException(string.Format(ErrorMessages.MaxLength, "Email", SystemConstants.MaxEmailLength));
        if (!EmailRegex().IsMatch(email))
            throw new DomainException(ErrorMessages.InvalidEmail);
    }
    
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ErrorMessages.NameRequired);
        if (name.Length > SystemConstants.MaxNameLength)
            throw new DomainException(string.Format(ErrorMessages.MaxLength, "Nome", SystemConstants.MaxNameLength));
    }
    
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public string? Permissions { get; set; }
    public Guid? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public static UserRole Create(Guid userId, Role role, Guid? grantedBy = null)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
