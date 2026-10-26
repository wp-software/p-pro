using PartnershipManager.Application.Features.Documents.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Services.Documents;

public interface IDocumentService
{
    Task<DocumentListResponse> GetPagedAsync(Guid clientId, Guid companyId, int page, int pageSize,
        string? documentType = null, string? visibility = null, string? search = null);
    Task<IEnumerable<DocumentResponse>> GetByEntityAsync(Guid clientId, Guid companyId, string entityType, Guid entityId);
    Task<DocumentResponse> GetByIdAsync(Guid id, Guid clientId);
    Task<DocumentResponse> CreateAsync(Guid clientId, CreateDocumentRequest request, Guid? userId = null);
    Task<DocumentResponse> UpdateMetadataAsync(Guid id, Guid clientId, UpdateDocumentMetadataRequest request, Guid userId);
    Task<DocumentResponse> VerifyAsync(Guid id, Guid clientId, Guid userId);
    Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null);
}

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DocumentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DocumentListResponse> GetPagedAsync(Guid clientId, Guid companyId, int page, int pageSize,
        string? documentType = null, string? visibility = null, string? search = null)
    {
        var (items, total) = await _unitOfWork.Documents.GetPagedAsync(clientId, companyId, page, pageSize,
            documentType, visibility, search);
        var responses = items.Select(MapToResponse);
        return new DocumentListResponse(responses, total, page, pageSize);
    }

    public async Task<IEnumerable<DocumentResponse>> GetByEntityAsync(Guid clientId, Guid companyId,
        string entityType, Guid entityId)
    {
        var items = await _unitOfWork.Documents.GetByEntityAsync(clientId, companyId, entityType, entityId);
        return items.Select(MapToResponse);
    }

    public async Task<DocumentResponse> GetByIdAsync(Guid id, Guid clientId)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(id, clientId)
            ?? throw new NotFoundException("Document", id);
        return MapToResponse(doc);
    }

    public async Task<DocumentResponse> CreateAsync(Guid clientId, CreateDocumentRequest request, Guid? userId = null)
    {
        var doc = Document.Create(
            clientId,
            request.CompanyId,
            request.Name,
            request.DocumentType,
            request.FileName,
            request.FileSizeBytes,
            request.MimeType,
            request.StoragePath,
            request.Visibility,
            request.Description,
            request.EntityType,
            request.EntityId,
            request.DownloadUrl,
            userId);

        await _unitOfWork.Documents.AddAsync(doc);
        return MapToResponse(doc);
    }

    public async Task<DocumentResponse> UpdateMetadataAsync(Guid id, Guid clientId, UpdateDocumentMetadataRequest request, Guid userId)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(id, clientId)
            ?? throw new NotFoundException("Document", id);

        doc.UpdateMetadata(request.Name, request.Description, request.Visibility, userId);
        await _unitOfWork.Documents.UpdateAsync(doc);
        return MapToResponse(doc);
    }

    public async Task<DocumentResponse> VerifyAsync(Guid id, Guid clientId, Guid userId)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(id, clientId)
            ?? throw new NotFoundException("Document", id);

        doc.Verify(userId);
        await _unitOfWork.Documents.UpdateAsync(doc);
        return MapToResponse(doc);
    }

    public async Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null)
    {
        _ = await _unitOfWork.Documents.GetByIdAsync(id, clientId)
            ?? throw new NotFoundException("Document", id);
        await _unitOfWork.Documents.SoftDeleteAsync(id, clientId, userId);
    }

    // ─── Mapper ──────────────────────────────────────────────────────────────

    private static DocumentResponse MapToResponse(Document d) =>
        new()
        {
            Id = d.Id,
            ClientId = d.ClientId,
            CompanyId = d.CompanyId,
            Name = d.Name,
            DocumentType = d.DocumentType,
            Description = d.Description,
            FileName = d.FileName,
            FileSizeBytes = d.FileSizeBytes,
            FileSizeFormatted = d.FileSizeFormatted,
            MimeType = d.MimeType,
            StoragePath = d.StoragePath,
            DownloadUrl = d.DownloadUrl,
            EntityType = d.EntityType,
            EntityId = d.EntityId,
            Source = GetSource(d),
            Visibility = d.Visibility,
            IsVerified = d.IsVerified,
            VerifiedAt = d.VerifiedAt,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };

    private static string GetSource(Document document)
    {
        if (string.Equals(document.EntityType, DocumentEntityTypes.FinancialPeriod, StringComparison.OrdinalIgnoreCase))
            return "financial";

        var storagePath = document.StoragePath.Replace('\\', '/').TrimStart('/');
        return storagePath.StartsWith("dataroom/", StringComparison.OrdinalIgnoreCase)
            ? "dataroom"
            : "documents";
    }
}
