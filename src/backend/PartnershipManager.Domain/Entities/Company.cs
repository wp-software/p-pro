using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;

namespace PartnershipManager.Domain.Entities;

public class Company : BaseEntity
{
    public Guid ClientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TradingName { get; private set; }
    public string Cnpj { get; private set; } = string.Empty;
    public LegalForm LegalForm { get; private set; }
    public DateTime FoundationDate { get; private set; }
    public decimal TotalShares { get; private set; }
    public decimal SharePrice { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public string? LogoUrl { get; private set; }
    public string? Settings { get; private set; }
    public CompanyStatus Status { get; private set; }
    
    // Address fields
    public string? Cep { get; private set; }
    public string? Street { get; private set; }
    public string? Number { get; private set; }
    public string? Complement { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    
    // Navigation properties
    public Client? Client { get; private set; }
    
    // Calculated
    public decimal Valuation => TotalShares * SharePrice;
    public string CnpjFormatted => FormatCnpj(Cnpj);
    
    private Company() { }
    
    public static Company Create(
        Guid clientId,
        string name,
        string cnpj,
        LegalForm legalForm,
        DateTime foundationDate,
        decimal totalShares,
        decimal sharePrice,
        string? tradingName = null,
        string currency = "BRL")
    {
        if (clientId == Guid.Empty)
            throw new DomainException("ClientId is required");
            
        ValidateName(name);
        ValidateCnpj(cnpj);
        ValidateFoundationDate(foundationDate);
        ValidateShares(totalShares, sharePrice);
        
        return new Company
        {
            ClientId = clientId,
            Name = name.Trim(),
            TradingName = tradingName?.Trim(),
            Cnpj = NormalizeCnpj(cnpj),
            LegalForm = legalForm,
            FoundationDate = foundationDate.Date,
            TotalShares = totalShares,
            SharePrice = sharePrice,
            Currency = currency.ToUpperInvariant(),
            Status = CompanyStatus.Active
        };
    }
    
    public void UpdateBasicInfo(string name, string? tradingName, string? logoUrl)
    {
        ValidateName(name);
        Name = name.Trim();
        TradingName = tradingName?.Trim();
        LogoUrl = logoUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateAddress(string? cep, string? street, string? number, string? complement, string? neighborhood, string? city, string? state)
    {
        Cep = cep?.Trim();
        Street = street?.Trim();
        Number = number?.Trim();
        Complement = complement?.Trim();
        Neighborhood = neighborhood?.Trim();
        City = city?.Trim();
        State = state?.Trim()?.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateShareInfo(decimal totalShares, decimal sharePrice)
    {
        ValidateShares(totalShares, sharePrice);
        TotalShares = totalShares;
        SharePrice = sharePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCurrency(string currency)
    {
        Currency = currency.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        Status = CompanyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        Status = CompanyStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Validations
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ErrorMessages.CompanyNameRequired);
        if (name.Length > SystemConstants.MaxNameLength)
            throw new DomainException(string.Format(ErrorMessages.MaxLength, "Nome", SystemConstants.MaxNameLength));
    }
    
    private static void ValidateCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            throw new DomainException(ErrorMessages.CnpjRequired);
        
        var normalized = NormalizeCnpj(cnpj);
        if (normalized.Length != SystemConstants.CnpjLength || !IsValidCnpj(normalized))
            throw new DomainException(ErrorMessages.InvalidCnpj);
    }
    
    private static void ValidateFoundationDate(DateTime date)
    {
        if (date > DateTime.Today)
            throw new DomainException(ErrorMessages.InvalidFoundationDate);
    }
    
    private static void ValidateShares(decimal total, decimal price)
    {
        if (total <= 0)
            throw new DomainException(ErrorMessages.InvalidTotalShares);
        if (price <= 0)
            throw new DomainException(ErrorMessages.InvalidSharePrice);
    }
    
    private static string NormalizeCnpj(string cnpj) 
        => new string(cnpj.Where(char.IsDigit).ToArray());
    
    private static string FormatCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return cnpj;
        return Convert.ToUInt64(cnpj).ToString(@"00\.000\.000\/0000\-00");
    }
    
    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1) return false;
        
        int[] m1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] m2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        
        var temp = cnpj[..12];
        var sum = temp.Select((c, i) => (c - '0') * m1[i]).Sum();
        var rem = sum % 11;
        var d1 = rem < 2 ? 0 : 11 - rem;
        
        temp += d1;
        sum = temp.Select((c, i) => (c - '0') * m2[i]).Sum();
        rem = sum % 11;
        var d2 = rem < 2 ? 0 : 11 - rem;
        
        return cnpj.EndsWith($"{d1}{d2}");
    }
}
