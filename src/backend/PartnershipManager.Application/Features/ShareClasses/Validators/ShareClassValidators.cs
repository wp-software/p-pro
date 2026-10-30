using FluentValidation;
using PartnershipManager.Application.Features.ShareClasses.DTOs;

namespace PartnershipManager.Application.Features.ShareClasses.Validators;

public class CreateShareClassValidator : AbstractValidator<CreateShareClassRequest>
{
    public CreateShareClassValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome da classe é obrigatório")
            .MaximumLength(100)
            .WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Código da classe é obrigatório")
            .MaximumLength(20)
            .WithMessage("Código deve ter no máximo 20 caracteres")
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("Código deve conter apenas letras e números");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Descrição deve ter no máximo 500 caracteres");

        RuleFor(x => x.VotesPerShare)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Votos por ação não pode ser negativo")
            .When(x => x.HasVotingRights);

        RuleFor(x => x.LiquidationPreference)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preferência de liquidação não pode ser negativa");

        RuleFor(x => x.DividendPreference)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preferência de dividendo não pode ser negativa")
            .LessThanOrEqualTo(100)
            .WithMessage("Preferência de dividendo não pode ser maior que 100%")
            .When(x => x.DividendPreference.HasValue);

        RuleFor(x => x.ConversionRatio)
            .NotNull()
            .WithMessage("Razão de conversão é obrigatória para classes conversíveis")
            .GreaterThan(0)
            .WithMessage("Razão de conversão deve ser maior que zero")
            .When(x => x.IsConvertible);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Ordem de exibição não pode ser negativa");
    }
}

public class UpdateShareClassValidator : AbstractValidator<UpdateShareClassRequest>
{
    public UpdateShareClassValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome da classe é obrigatório")
            .MaximumLength(100)
            .WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Código da classe é obrigatório")
            .MaximumLength(20)
            .WithMessage("Código deve ter no máximo 20 caracteres")
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("Código deve conter apenas letras e números");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Descrição deve ter no máximo 500 caracteres");

        RuleFor(x => x.VotesPerShare)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Votos por ação não pode ser negativo")
            .When(x => x.HasVotingRights);

        RuleFor(x => x.LiquidationPreference)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preferência de liquidação não pode ser negativa");

        RuleFor(x => x.DividendPreference)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preferência de dividendo não pode ser negativa")
            .LessThanOrEqualTo(100)
            .WithMessage("Preferência de dividendo não pode ser maior que 100%")
            .When(x => x.DividendPreference.HasValue);

        RuleFor(x => x.ConversionRatio)
            .NotNull()
            .WithMessage("Razão de conversão é obrigatória para classes conversíveis")
            .GreaterThan(0)
            .WithMessage("Razão de conversão deve ser maior que zero")
            .When(x => x.IsConvertible);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Ordem de exibição não pode ser negativa");
    }
}
