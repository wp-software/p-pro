using FluentValidation;
using PartnershipManager.Application.Features.Companies.DTOs;
using PartnershipManager.Domain.Constants;

namespace PartnershipManager.Application.Features.Companies.Validators;

/// <summary>
/// Validador para CreateCompanyRequest
/// </summary>
public class CreateCompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ErrorMessages.CompanyNameRequired)
            .MaximumLength(SystemConstants.MaxNameLength)
                .WithMessage(string.Format(ErrorMessages.MaxLength, "Nome", SystemConstants.MaxNameLength));
        
        RuleFor(x => x.TradingName)
            .MaximumLength(SystemConstants.MaxNameLength)
                .WithMessage(string.Format(ErrorMessages.MaxLength, "Nome Fantasia", SystemConstants.MaxNameLength))
            .When(x => !string.IsNullOrEmpty(x.TradingName));
        
        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage(ErrorMessages.CnpjRequired)
            .Must(BeValidCnpj).WithMessage(ErrorMessages.InvalidCnpj);
        
        RuleFor(x => x.LegalForm)
            .IsInEnum().WithMessage(ErrorMessages.InvalidLegalForm);
        
        RuleFor(x => x.FoundationDate)
            .NotEmpty().WithMessage(string.Format(ErrorMessages.Required, "Data de Fundação"))
            .LessThanOrEqualTo(DateTime.Today).WithMessage(ErrorMessages.InvalidFoundationDate);
        
        RuleFor(x => x.TotalShares)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidTotalShares);
        
        RuleFor(x => x.SharePrice)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidSharePrice);
        
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage(string.Format(ErrorMessages.Required, "Moeda"))
            .Length(3).WithMessage("Moeda deve ter 3 caracteres (ex: BRL, USD)")
            .Must(BeValidCurrency).WithMessage(ErrorMessages.InvalidCurrency);
    }
    
    private static bool BeValidCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;
        
        // Remove caracteres não numéricos
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        
        if (cnpj.Length != 14)
            return false;
        
        // Verifica se todos os dígitos são iguais
        if (cnpj.Distinct().Count() == 1)
            return false;
        
        // Validação dos dígitos verificadores
        int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        
        var tempCnpj = cnpj.Substring(0, 12);
        var sum = 0;
        
        for (int i = 0; i < 12; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];
        
        var remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;
        
        var digit = remainder.ToString();
        tempCnpj += digit;
        sum = 0;
        
        for (int i = 0; i < 13; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];
        
        remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;
        digit += remainder.ToString();
        
        return cnpj.EndsWith(digit);
    }
    
    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "BRL", "USD", "EUR", "GBP" };
        return validCurrencies.Contains(currency?.ToUpperInvariant());
    }
}

/// <summary>
/// Validador para UpdateCompanyRequest
/// </summary>
public class UpdateCompanyValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ErrorMessages.CompanyNameRequired)
            .MaximumLength(SystemConstants.MaxNameLength)
                .WithMessage(string.Format(ErrorMessages.MaxLength, "Nome", SystemConstants.MaxNameLength));
        
        RuleFor(x => x.TradingName)
            .MaximumLength(SystemConstants.MaxNameLength)
                .WithMessage(string.Format(ErrorMessages.MaxLength, "Nome Fantasia", SystemConstants.MaxNameLength))
            .When(x => !string.IsNullOrEmpty(x.TradingName));
        
        RuleFor(x => x.LogoUrl)
            .MaximumLength(SystemConstants.MaxUrlLength)
                .WithMessage(string.Format(ErrorMessages.MaxLength, "URL do Logo", SystemConstants.MaxUrlLength))
            .Must(BeValidUrl).WithMessage(ErrorMessages.InvalidUrl)
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));

        RuleFor(x => x.TotalShares)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidTotalShares)
            .When(x => x.TotalShares.HasValue);

        RuleFor(x => x.SharePrice)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidSharePrice)
            .When(x => x.SharePrice.HasValue);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage(string.Format(ErrorMessages.Required, "Moeda"))
            .Length(3).WithMessage("Moeda deve ter 3 caracteres (ex: BRL, USD)")
            .Must(currency => new[] { "BRL", "USD", "EUR", "GBP" }.Contains(currency?.ToUpperInvariant()))
            .WithMessage(ErrorMessages.InvalidCurrency)
            .When(x => x.Currency is not null);
    }
    
    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validador para UpdateShareInfoRequest
/// </summary>
public class UpdateShareInfoValidator : AbstractValidator<UpdateShareInfoRequest>
{
    public UpdateShareInfoValidator()
    {
        RuleFor(x => x.TotalShares)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidTotalShares);
        
        RuleFor(x => x.SharePrice)
            .GreaterThan(0).WithMessage(ErrorMessages.InvalidSharePrice);
    }
}
