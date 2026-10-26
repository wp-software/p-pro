using PartnershipManager.Application.Common.Models;

namespace PartnershipManager.Application.Features.Documents.DTOs;

// ────────────────────────────────────────────────────────────────────────────
// DOCUMENT
// ────────────────────────────────────────────────────────────────────────────

public record DocumentResponse
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public Guid CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string FileSizeFormatted { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
    public string? DownloadUrl { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string Source { get; init; } = "documents";
    public string Visibility { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class DocumentListResponse : PagedResult<DocumentResponse>
{
    public DocumentListResponse(IEnumerable<DocumentResponse> items, int totalCount, int pageNumber, int pageSize)
        : base(items, totalCount, pageNumber, pageSize) { }
}

public record CreateDocumentRequest
{
    public Guid CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
    public string Visibility { get; init; } = "admin";
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? DownloadUrl { get; init; }
}

public record UpdateDocumentMetadataRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = "admin";
}
