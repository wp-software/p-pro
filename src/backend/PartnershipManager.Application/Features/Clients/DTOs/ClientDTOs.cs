using PartnershipManager.Domain.Enums;
using PartnershipManager.Application.Features.Companies.DTOs;

namespace PartnershipManager.Application.Features.Clients.DTOs;

/// <summary>
/// DTO para resposta de Client
/// </summary>
public record ClientResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string Document { get; init; } = string.Empty;
    public string DocumentFormatted { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? LogoUrl { get; init; }
    public string? Settings { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalCompanies { get; init; }
    public int TotalUsers { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO para criação de Client
/// </summary>
public record CreateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string Document { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public record OnboardClientRequest
{
    public CreateClientRequest Client { get; init; } = new();
    public CreateCompanyRequest Company { get; init; } = new();
    public string AdministratorName { get; init; } = string.Empty;
    public string AdministratorEmail { get; init; } = string.Empty;
}

public record ClientOnboardingResponse
{
    public Guid ClientId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid AdministratorId { get; init; }
    public bool ActivationEmailSent { get; init; }
}

/// <summary>
/// DTO para atualização de Client
/// </summary>
public record UpdateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string? Phone { get; init; }
    public string? LogoUrl { get; init; }
}

/// <summary>
/// DTO para atualização de email do Client
/// </summary>
public record UpdateClientEmailRequest
{
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// DTO para atualização de configurações do Client
/// </summary>
public record UpdateClientSettingsRequest
{
    public string Settings { get; init; } = string.Empty;
}

/// <summary>
/// DTO resumido de Client (para listas e seleção)
/// </summary>
public record ClientSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TradingName { get; init; }
    public string DocumentFormatted { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalCompanies { get; init; }
    public int TotalUsers { get; init; }
}

/// <summary>
/// DTO para lista de companies do client
/// </summary>
public record ClientCompanyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CnpjFormatted { get; init; } = string.Empty;
    public decimal Valuation { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// DTO para convite de usuário em um cliente (SuperAdmin)
/// </summary>
public record CreateClientUserRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }
    public Role Role { get; init; } = Role.Viewer;
    public string? Phone { get; init; }
}
