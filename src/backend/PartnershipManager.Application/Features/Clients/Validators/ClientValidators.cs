using FluentValidation;
using PartnershipManager.Application.Features.Clients.DTOs;
using PartnershipManager.Application.Features.Companies.Validators;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Application.Features.Clients.Validators;

public class OnboardClientValidator : AbstractValidator<OnboardClientRequest>
{
    public OnboardClientValidator()
    {
        RuleFor(x => x.Client).NotNull().SetValidator(new CreateClientValidator());
        RuleFor(x => x.Company).NotNull().SetValidator(new CreateCompanyValidator());
        RuleFor(x => x.AdministratorName).NotEmpty().MaximumLength(SystemConstants.MaxNameLength);
        RuleFor(x => x.AdministratorEmail).NotEmpty().EmailAddress().MaximumLength(SystemConstants.MaxEmailLength);
    }
}

/// <summary>
/// Validador para CreateClientRequest
/// </summary>
public class CreateClientValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do cliente é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome não pode exceder 200 caracteres");
        
        RuleFor(x => x.TradingName)
            .MaximumLength(200).WithMessage("Nome fantasia não pode exceder 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TradingName));
        
        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Documento é obrigatório")
            .Must((request, document) => BeValidDocument(document, request.DocumentType))
                .WithMessage("Documento inválido");
        
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Tipo de documento inválido");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email não pode exceder 255 caracteres");
        
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefone não pode exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
    
    private static bool BeValidDocument(string document, DocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(document))
            return false;
        
        // Remove caracteres não numéricos
        var normalized = new string(document.Where(char.IsDigit).ToArray());
        
        if (documentType == DocumentType.Cnpj)
            return IsValidCnpj(normalized);
        else if (documentType == DocumentType.Cpf)
            return IsValidCpf(normalized);
        
        return false;
    }
    
    private static bool IsValidCnpj(string cnpj)
    {
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
        
        return cnpj.EndsWith(tempCnpj.Substring(12) + remainder.ToString());
    }
    
    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11)
            return false;
        
        // Verifica se todos os dígitos são iguais
        if (cpf.Distinct().Count() == 1)
            return false;
        
        // Validação dos dígitos verificadores
        var tempCpf = cpf.Substring(0, 9);
        var sum = 0;
        
        for (int i = 0; i < 9; i++)
            sum += int.Parse(tempCpf[i].ToString()) * (10 - i);
        
        var remainder = sum % 11;
        var digit = remainder < 2 ? 0 : 11 - remainder;
        
        tempCpf += digit;
        sum = 0;
        
        for (int i = 0; i < 10; i++)
            sum += int.Parse(tempCpf[i].ToString()) * (11 - i);
        
        remainder = sum % 11;
        digit = remainder < 2 ? 0 : 11 - remainder;
        
        return cpf.EndsWith(tempCpf.Substring(9) + digit.ToString());
    }
}

/// <summary>
/// Validador para UpdateClientRequest
/// </summary>
public class UpdateClientValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do cliente é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome não pode exceder 200 caracteres");
        
        RuleFor(x => x.TradingName)
            .MaximumLength(200).WithMessage("Nome fantasia não pode exceder 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TradingName));
        
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefone não pode exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Phone));
        
        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("URL do logo não pode exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));
    }
}

/// <summary>
/// Validador para UpdateClientEmailRequest
/// </summary>
public class UpdateClientEmailValidator : AbstractValidator<UpdateClientEmailRequest>
{
    public UpdateClientEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255).WithMessage("Email não pode exceder 255 caracteres");
    }
}

/// <summary>
/// Validador para UpdateClientSettingsRequest
/// </summary>
public class UpdateClientSettingsValidator : AbstractValidator<UpdateClientSettingsRequest>
{
    public UpdateClientSettingsValidator()
    {
        RuleFor(x => x.Settings)
            .NotEmpty().WithMessage("Configurações são obrigatórias")
            .Must(BeValidJson).WithMessage("Configurações devem ser um JSON válido");
    }
    
    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
        
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
