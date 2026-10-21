using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Application.Features.Companies.DTOs;

/// <summary>
/// DTO para resposta de Company
/// </summary>
public record CompanyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string Cnpj { get; init; } = string.Empty;
    public string CnpjFormatted { get; init; } = string.Empty;
    public string LegalForm { get; init; } = string.Empty;
    public DateTime FoundationDate { get; init; }
    public decimal TotalShares { get; init; }
    public decimal SharePrice { get; init; }
    public string Currency { get; init; } = "BRL";
    public decimal Valuation { get; init; }
    public string? LogoUrl { get; init; }
    
    // Address fields
    public string? Cep { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO para criação de Company
/// </summary>
public record CreateCompanyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string Cnpj { get; init; } = string.Empty;
    public LegalForm LegalForm { get; init; }
    public DateTime FoundationDate { get; init; }
    public decimal TotalShares { get; init; }
    public decimal SharePrice { get; init; }
    public string Currency { get; init; } = "BRL";
}

/// <summary>
/// DTO para atualização de Company
/// </summary>
public record UpdateCompanyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string? LogoUrl { get; init; }
    public decimal? TotalShares { get; init; }
    public decimal? SharePrice { get; init; }
    public string? Currency { get; init; }
    
    // Address fields
    public string? Cep { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
}

/// <summary>
/// DTO para atualização de informações de ações
/// </summary>
public record UpdateShareInfoRequest
{
    public decimal TotalShares { get; init; }
    public decimal SharePrice { get; init; }
}

/// <summary>
/// DTO resumido de Company (para listas)
/// </summary>
public record CompanySummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CnpjFormatted { get; init; } = string.Empty;
    public decimal Valuation { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalUsers { get; init; }
}
