using PartnershipManager.Application.Common.Models;
using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Application.Features.ShareClasses.DTOs;

public record ShareClassResponse
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    
    // Voting rights
    public bool HasVotingRights { get; init; }
    public decimal VotesPerShare { get; init; }
    
    // Liquidation preferences
    public decimal LiquidationPreference { get; init; }
    public bool Participating { get; init; }
    public decimal? DividendPreference { get; init; }
    
    // Conversion options
    public bool IsConvertible { get; init; }
    public Guid? ConvertsToClassId { get; init; }
    public string? ConvertsToClassName { get; init; }
    public decimal? ConversionRatio { get; init; }
    
    // Anti-dilution
    public AntiDilutionType? AntiDilutionType { get; init; }
    public string? AntiDilutionTypeDescription { get; init; }
    
    // Additional rights
    public string? Rights { get; init; }
    
    // Status and metadata
    public ShareClassStatus Status { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class ShareClassListResponse : PagedResult<ShareClassResponse>
{
    public ShareClassListResponse(IEnumerable<ShareClassResponse> items, int totalCount, int pageNumber, int pageSize)
        : base(items, totalCount, pageNumber, pageSize)
    {
    }
}

public record CreateShareClassRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    
    // Voting rights
    public bool HasVotingRights { get; init; } = true;
    public decimal VotesPerShare { get; init; } = 1m;
    
    // Liquidation preferences
    public decimal LiquidationPreference { get; init; } = 1m;
    public bool Participating { get; init; } = false;
    public decimal? DividendPreference { get; init; }
    
    // Conversion options
    public bool IsConvertible { get; init; } = false;
    public Guid? ConvertsToClassId { get; init; }
    public decimal? ConversionRatio { get; init; }
    
    // Anti-dilution
    public AntiDilutionType? AntiDilutionType { get; init; }
    
    // Additional rights
    public string? Rights { get; init; }
    
    // Display order
    public int DisplayOrder { get; init; } = 0;
}

public record UpdateShareClassRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    
    // Voting rights
    public bool HasVotingRights { get; init; }
    public decimal VotesPerShare { get; init; }
    
    // Liquidation preferences
    public decimal LiquidationPreference { get; init; }
    public bool Participating { get; init; }
    public decimal? DividendPreference { get; init; }
    
    // Conversion options
    public bool IsConvertible { get; init; }
    public Guid? ConvertsToClassId { get; init; }
    public decimal? ConversionRatio { get; init; }
    
    // Anti-dilution
    public AntiDilutionType? AntiDilutionType { get; init; }
    
    // Additional rights
    public string? Rights { get; init; }
    
    // Display order
    public int DisplayOrder { get; init; }
}

public record ShareClassSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool HasVotingRights { get; init; }
    public decimal LiquidationPreference { get; init; }
    public ShareClassStatus Status { get; init; }
}
