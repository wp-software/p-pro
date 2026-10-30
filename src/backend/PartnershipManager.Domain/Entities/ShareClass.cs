using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Domain.Entities;

/// <summary>
/// Represents a class of shares with specific rights and preferences.
/// </summary>
public class ShareClass : BaseEntity
{
    public Guid ClientId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    
    // Voting rights
    public bool HasVotingRights { get; private set; }
    public decimal VotesPerShare { get; private set; }
    
    // Liquidation preferences
    public decimal LiquidationPreference { get; private set; }
    public bool Participating { get; private set; }
    public decimal? DividendPreference { get; private set; }
    
    // Conversion options
    public bool IsConvertible { get; private set; }
    public Guid? ConvertsToClassId { get; private set; }
    public decimal? ConversionRatio { get; private set; }
    
    // Anti-dilution
    public AntiDilutionType? AntiDilutionType { get; private set; }
    
    // Additional rights (stored as JSON)
    public string? Rights { get; private set; }
    
    // Status and ordering
    public ShareClassStatus Status { get; private set; }
    public int DisplayOrder { get; private set; }
    
    // Navigation properties (populated by repository)
    public string? CompanyName { get; private set; }
    public string? ConvertsToClassName { get; private set; }

    private ShareClass() { }

    public static ShareClass Create(
        Guid clientId,
        Guid? companyId,
        string name,
        string code,
        string? description = null,
        bool hasVotingRights = true,
        decimal votesPerShare = 1m,
        decimal liquidationPreference = 1m,
        bool participating = false,
        decimal? dividendPreference = null,
        bool isConvertible = false,
        Guid? convertsToClassId = null,
        decimal? conversionRatio = null,
        AntiDilutionType? antiDilutionType = null,
        string? rights = null,
        int displayOrder = 0,
        Guid? createdBy = null)
    {
        ValidateCode(code);
        ValidateLiquidationPreference(liquidationPreference);
        ValidateConversion(isConvertible, conversionRatio);

        return new ShareClass
        {
            ClientId = clientId,
            CompanyId = companyId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            HasVotingRights = hasVotingRights,
            VotesPerShare = hasVotingRights ? votesPerShare : 0,
            LiquidationPreference = liquidationPreference,
            Participating = participating,
            DividendPreference = dividendPreference,
            IsConvertible = isConvertible,
            ConvertsToClassId = isConvertible ? convertsToClassId : null,
            ConversionRatio = isConvertible ? conversionRatio : null,
            AntiDilutionType = antiDilutionType,
            Rights = rights,
            Status = ShareClassStatus.Active,
            DisplayOrder = displayOrder,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string code,
        string? description,
        bool hasVotingRights,
        decimal votesPerShare,
        decimal liquidationPreference,
        bool participating,
        decimal? dividendPreference,
        bool isConvertible,
        Guid? convertsToClassId,
        decimal? conversionRatio,
        AntiDilutionType? antiDilutionType,
        string? rights,
        int displayOrder,
        Guid? updatedBy = null)
    {
        ValidateCode(code);
        ValidateLiquidationPreference(liquidationPreference);
        ValidateConversion(isConvertible, conversionRatio);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        HasVotingRights = hasVotingRights;
        VotesPerShare = hasVotingRights ? votesPerShare : 0;
        LiquidationPreference = liquidationPreference;
        Participating = participating;
        DividendPreference = dividendPreference;
        IsConvertible = isConvertible;
        ConvertsToClassId = isConvertible ? convertsToClassId : null;
        ConversionRatio = isConvertible ? conversionRatio : null;
        AntiDilutionType = antiDilutionType;
        Rights = rights;
        DisplayOrder = displayOrder;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate(Guid? updatedBy = null)
    {
        Status = ShareClassStatus.Active;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        Status = ShareClassStatus.Inactive;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDisplayOrder(int order, Guid? updatedBy = null)
    {
        DisplayOrder = order;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código da classe não pode ser vazio", nameof(code));

        if (code.Length > 20)
            throw new ArgumentException("Código da classe não pode ter mais de 20 caracteres", nameof(code));
    }

    private static void ValidateLiquidationPreference(decimal preference)
    {
        if (preference < 0)
            throw new ArgumentException("Preferência de liquidação não pode ser negativa", nameof(preference));
    }

    private static void ValidateConversion(bool isConvertible, decimal? conversionRatio)
    {
        if (isConvertible && conversionRatio == null)
            throw new ArgumentException("Classes conversíveis devem ter uma razão de conversão definida", nameof(conversionRatio));

        if (isConvertible && conversionRatio <= 0)
            throw new ArgumentException("Razão de conversão deve ser maior que zero", nameof(conversionRatio));
    }
}
