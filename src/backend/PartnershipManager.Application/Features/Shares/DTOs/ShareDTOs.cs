using PartnershipManager.Application.Common.Models;
using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Application.Features.Shares.DTOs;

#region Share DTOs

public record ShareResponse
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public Guid ShareholderId { get; init; }
    public string ShareholderName { get; init; } = string.Empty;
    public Guid ShareClassId { get; init; }
    public string ShareClassName { get; init; } = string.Empty;
    public string ShareClassCode { get; init; } = string.Empty;
    public string? CertificateNumber { get; init; }
    public decimal Quantity { get; init; }
    public decimal AcquisitionPrice { get; init; }
    public decimal TotalCost { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public ShareOrigin Origin { get; init; }
    public string OriginDescription { get; init; } = string.Empty;
    public Guid? OriginTransactionId { get; init; }
    public ShareStatus Status { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class ShareListResponse : PagedResult<ShareResponse>
{
    public ShareListResponse(IEnumerable<ShareResponse> items, int totalCount, int pageNumber, int pageSize)
        : base(items, totalCount, pageNumber, pageSize)
    {
    }

    public decimal TotalShares { get; set; }
    public decimal TotalValue { get; set; }
}

public record CreateShareRequest
{
    public Guid CompanyId { get; init; }
    public Guid ShareholderId { get; init; }
    public Guid ShareClassId { get; init; }
    public string? CertificateNumber { get; init; }
    public decimal Quantity { get; init; }
    public decimal AcquisitionPrice { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public string? Notes { get; init; }
}

public record UpdateShareRequest
{
    public string? CertificateNumber { get; init; }
    public string? Notes { get; init; }
}

public record ShareSummaryResponse
{
    public Guid Id { get; init; }
    public string ShareholderName { get; init; } = string.Empty;
    public string ShareClassCode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public ShareStatus Status { get; init; }
}

#endregion

#region Transaction DTOs

public record ShareTransactionResponse
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public TransactionType TransactionType { get; init; }
    public string TransactionTypeDescription { get; init; } = string.Empty;
    public string? TransactionNumber { get; init; }
    public DateTime ReferenceDate { get; init; }
    public Guid? ShareId { get; init; }
    public Guid ShareClassId { get; init; }
    public string ShareClassName { get; init; } = string.Empty;
    public string ShareClassCode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal PricePerShare { get; init; }
    public decimal TotalValue { get; init; }
    public Guid? FromShareholderId { get; init; }
    public string? FromShareholderName { get; init; }
    public Guid? ToShareholderId { get; init; }
    public string? ToShareholderName { get; init; }
    public string? Reason { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
    public Guid? ApprovedBy { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class TransactionListResponse : PagedResult<ShareTransactionResponse>
{
    public TransactionListResponse(IEnumerable<ShareTransactionResponse> items, int totalCount, int pageNumber, int pageSize)
        : base(items, totalCount, pageNumber, pageSize)
    {
    }

    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public record IssueSharesRequest
{
    public Guid CompanyId { get; init; }
    public Guid ShareholderId { get; init; }
    public Guid ShareClassId { get; init; }
    public decimal Quantity { get; init; }
    public decimal PricePerShare { get; init; }
    public DateTime ReferenceDate { get; init; }
    public string? CertificateNumber { get; init; }
    public string? TransactionNumber { get; init; }
    public string? Reason { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
}

public record IssueSharesBatchRequest
{
    public Guid CompanyId { get; init; }
    public Guid ShareClassId { get; init; }
    public decimal TotalQuantity { get; init; }
    public decimal PricePerShare { get; init; }
    public DateTime ReferenceDate { get; init; }
    public List<ShareAllocationRequest> Allocations { get; init; } = [];
    public string? Reason { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
}

public record ShareAllocationRequest
{
    public Guid ShareholderId { get; init; }
    public decimal Quantity { get; init; }
}

public record TransferSharesRequest
{
    public Guid CompanyId { get; init; }
    public Guid FromShareholderId { get; init; }
    public Guid ToShareholderId { get; init; }
    public Guid ShareClassId { get; init; }
    public decimal Quantity { get; init; }
    public decimal PricePerShare { get; init; }
    public DateTime ReferenceDate { get; init; }
    public string? TransactionNumber { get; init; }
    public string? Reason { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
}

public record CancelSharesRequest
{
    public Guid CompanyId { get; init; }
    public Guid ShareholderId { get; init; }
    public Guid ShareClassId { get; init; }
    public decimal Quantity { get; init; }
    public DateTime ReferenceDate { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? TransactionNumber { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
}

public record ConvertSharesRequest
{
    public Guid CompanyId { get; init; }
    public Guid ShareholderId { get; init; }
    public Guid FromShareClassId { get; init; }
    public Guid ToShareClassId { get; init; }
    public decimal Quantity { get; init; }
    public DateTime ReferenceDate { get; init; }
    public string? TransactionNumber { get; init; }
    public string? Reason { get; init; }
    public string? DocumentReference { get; init; }
    public string? Notes { get; init; }
}

#endregion

#region Cap Table DTOs

public record CapTableEntryResponse
{
    public Guid ShareholderId { get; init; }
    public string ShareholderName { get; init; } = string.Empty;
    public ShareholderType ShareholderType { get; init; }
    public string ShareholderTypeDescription { get; init; } = string.Empty;
    public Guid ShareClassId { get; init; }
    public string ShareClassName { get; init; } = string.Empty;
    public string ShareClassCode { get; init; } = string.Empty;
    public decimal TotalShares { get; init; }
    public decimal TotalValue { get; init; }
    public decimal OwnershipPercentage { get; init; }
    public decimal VotingPercentage { get; init; }
    public decimal FullyDilutedPercentage { get; init; }
}

public record CapTableResponse
{
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public DateTime AsOfDate { get; init; }
    public decimal TotalShares { get; init; }
    public decimal TotalValue { get; init; }
    public decimal TotalVotingShares { get; init; }
    public List<CapTableEntryResponse> Entries { get; init; } = new();
    public List<CapTableSummaryByType> SummaryByType { get; init; } = new();
    public List<CapTableSummaryByClass> SummaryByClass { get; init; } = new();
}

public record CapTableSummaryByType
{
    public ShareholderType Type { get; init; }
    public string TypeDescription { get; init; } = string.Empty;
    public int ShareholderCount { get; init; }
    public decimal TotalShares { get; init; }
    public decimal OwnershipPercentage { get; init; }
}

public record CapTableSummaryByClass
{
    public Guid ShareClassId { get; init; }
    public string ShareClassName { get; init; } = string.Empty;
    public string ShareClassCode { get; init; } = string.Empty;
    public decimal TotalShares { get; init; }
    public decimal OwnershipPercentage { get; init; }
}

#endregion
