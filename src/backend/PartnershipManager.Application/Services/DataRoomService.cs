using PartnershipManager.Application.DTOs.DataRoom;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Application.Services;

public interface IDataRoomService
{
    Task<DataRoomResponse> GetOrCreateDataRoomAsync(Guid companyId, Guid userId);
    Task<IEnumerable<DataRoomFolderResponse>> GetFoldersAsync(Guid companyId, DataRoomUser user, Guid? parentId = null);
    Task<Guid> CreateFolderAsync(Guid companyId, CreateFolderRequest request, DataRoomUser user);
    Task DeleteFolderAsync(Guid companyId, Guid folderId, DataRoomUser user);
    Task<IEnumerable<Document>> GetDocumentsInFolderAsync(Guid companyId, Guid folderId, DataRoomUser user);
    Task AddDocumentToFolderAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user);
    Task<DataRoomUploadResult> UploadDocumentAsync(Guid clientId, Guid companyId, Guid folderId,
        string documentName, string documentType, string fileName, long fileSize, string mimeType,
        Func<CancellationToken, Task<string>> persistFile, DataRoomUser user, CancellationToken cancellationToken);
    Task RemoveDocumentFromFolderAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user);
    Task SetFolderRolesAsync(Guid companyId, Guid folderId, IEnumerable<string> roles, DataRoomUser user);
    Task<IEnumerable<DataRoomActivityLogResponse>> GetActivityLogsAsync(Guid companyId, DataRoomUser user, Guid? folderId, int limit);
    Task<FinancialUploadResult> UploadFinancialDocumentAsync(Guid clientId, Guid companyId, int year, int month, Guid? periodId, string? metricKey,
        string documentName, string documentType, string fileName, long fileSize, string mimeType,
        Func<Task<string>> persistFile, DataRoomUser user);
    Task<IEnumerable<FinancialDocumentResponse>> GetFinancialDocumentsAsync(Guid clientId, Guid companyId, Guid periodId, DataRoomUser user);
    Task RemoveFinancialDocumentAsync(Guid clientId, Guid companyId, Guid documentId, DataRoomUser user);
    Task<Document> GetDocumentForDownloadAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user);
}

public class DataRoomService : IDataRoomService
{
    private static readonly HashSet<string> ValidRoles =
        new(Enum.GetNames<Role>(), StringComparer.OrdinalIgnoreCase);

    public const string FinancialFolderName = "Financeiro";

    private readonly IDataRoomRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public DataRoomService(IDataRoomRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    private static bool IsAdmin(DataRoomUser user)
        => user.Roles.Any(r => r.Equals(nameof(Role.SuperAdmin), StringComparison.OrdinalIgnoreCase)
                            || r.Equals(nameof(Role.Admin), StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowed(Guid? folderId, IReadOnlyDictionary<Guid, DataRoomFolder> folders,
        ILookup<Guid, string> folderRoles, DataRoomUser user)
    {
        if (IsAdmin(user)) return true;
        // Uma pasta restrita (ou com ancestral restrito) exige que o usuário tenha um dos perfis vinculados
        var visited = new HashSet<Guid>();
        while (folderId.HasValue && folders.TryGetValue(folderId.Value, out var folder) && visited.Add(folder.Id))
        {
            var allowed = folderRoles[folder.Id].ToList();
            if (allowed.Count > 0 && !allowed.Any(a => user.Roles.Contains(a, StringComparer.OrdinalIgnoreCase)))
                return false;
            folderId = folder.ParentId;
        }
        return true;
    }

    private async Task<(DataRoom Room, Dictionary<Guid, DataRoomFolder> Folders, ILookup<Guid, string> Roles)> LoadAsync(Guid companyId)
    {
        var room = await _repo.GetByCompanyAsync(companyId)
            ?? throw new KeyNotFoundException("Data room não encontrado.");
        var folders = (await _repo.GetAllFoldersAsync(room.Id)).ToDictionary(f => f.Id);
        var roles = (await _repo.GetFolderRolesByDataRoomAsync(room.Id)).ToLookup(r => r.FolderId, r => r.Role);
        return (room, folders, roles);
    }

    private async Task<(DataRoom Room, DataRoomFolder Folder)> RequireFolderAccessAsync(Guid companyId, Guid folderId, DataRoomUser user)
    {
        var (room, folders, roles) = await LoadAsync(companyId);
        if (!folders.TryGetValue(folderId, out var folder))
            throw new KeyNotFoundException("Pasta não encontrada.");
        if (!IsAllowed(folderId, folders, roles, user))
            throw new UnauthorizedAccessException("Seu perfil não tem acesso a esta pasta.");
        return (room, folder);
    }

    private Task LogAsync(DataRoom room, DataRoomFolder? folder, Guid? documentId, string? documentName,
        string action, DataRoomUser user, string? details = null)
        => _repo.AddActivityLogAsync(new DataRoomActivityLog
        {
            DataRoomId = room.Id,
            FolderId = folder?.Id,
            FolderName = folder?.Name,
            DocumentId = documentId,
            DocumentName = documentName,
            Action = action,
            UserId = user.UserId,
            UserName = user.UserName,
            Details = details
        });

    public async Task<DataRoomResponse> GetOrCreateDataRoomAsync(Guid companyId, Guid userId)
    {
        var dr = await _repo.GetByCompanyAsync(companyId);
        if (dr == null)
        {
            var newDr = new DataRoom { CompanyId = companyId, Name = "Data Room", IsActive = true };
            newDr.CreatedBy = userId;
            newDr.UpdatedBy = userId;
            var id = await _repo.CreateDataRoomAsync(newDr);
            dr = await _repo.GetByCompanyAsync(companyId)
                ?? new DataRoom { Id = id, CompanyId = companyId, Name = "Data Room", IsActive = true };
        }
        return new DataRoomResponse
        {
            Id = dr.Id,
            CompanyId = dr.CompanyId,
            Name = dr.Name,
            Description = dr.Description,
            IsActive = dr.IsActive,
            CreatedAt = dr.CreatedAt
        };
    }

    public async Task<IEnumerable<DataRoomFolderResponse>> GetFoldersAsync(Guid companyId, DataRoomUser user, Guid? parentId = null)
    {
        var room = await _repo.GetByCompanyAsync(companyId);
        if (room == null) return Enumerable.Empty<DataRoomFolderResponse>();

        var (_, folders, roles) = await LoadAsync(companyId);
        return folders.Values
            .Where(f => f.ParentId == parentId && IsAllowed(f.Id, folders, roles, user))
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new DataRoomFolderResponse
            {
                Id = f.Id,
                DataRoomId = f.DataRoomId,
                ParentId = f.ParentId,
                Name = f.Name,
                Description = f.Description,
                DisplayOrder = f.DisplayOrder,
                Visibility = f.Visibility,
                CreatedAt = f.CreatedAt,
                AllowedRoles = roles[f.Id].ToList()
            })
            .ToList();
    }

    public async Task<Guid> CreateFolderAsync(Guid companyId, CreateFolderRequest request, DataRoomUser user)
    {
        var dr = await GetOrCreateDataRoomAsync(companyId, user.UserId);
        if (request.ParentId.HasValue)
            await RequireFolderAccessAsync(companyId, request.ParentId.Value, user);

        var folder = new DataRoomFolder
        {
            DataRoomId = dr.Id,
            ParentId = request.ParentId,
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            Visibility = request.Visibility
        };
        folder.CreatedBy = user.UserId;
        folder.UpdatedBy = user.UserId;
        var id = await _repo.CreateFolderAsync(folder);
        await LogAsync(new DataRoom { Id = dr.Id }, folder, null, null, DataRoomActions.FolderCreated, user);
        return id;
    }

    public async Task DeleteFolderAsync(Guid companyId, Guid folderId, DataRoomUser user)
    {
        var (room, folder) = await RequireFolderAccessAsync(companyId, folderId, user);
        await _repo.SoftDeleteFolderAsync(folderId);
        await LogAsync(room, folder, null, null, DataRoomActions.FolderDeleted, user);
    }

    public async Task<IEnumerable<Document>> GetDocumentsInFolderAsync(Guid companyId, Guid folderId, DataRoomUser user)
    {
        await RequireFolderAccessAsync(companyId, folderId, user);
        return await _repo.GetDocumentsInFolderAsync(folderId);
    }

    public async Task AddDocumentToFolderAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user)
    {
        var (room, folder) = await RequireFolderAccessAsync(companyId, folderId, user);
        await _repo.AddDocumentToFolderAsync(folderId, documentId, user.UserId);
        var name = await _repo.GetDocumentNameAsync(documentId);
        await LogAsync(room, folder, documentId, name, DataRoomActions.DocumentAdded, user);
    }

    public async Task<DataRoomUploadResult> UploadDocumentAsync(Guid clientId, Guid companyId, Guid folderId,
        string documentName, string documentType, string fileName, long fileSize, string mimeType,
        Func<CancellationToken, Task<string>> persistFile, DataRoomUser user, CancellationToken cancellationToken)
    {
        var (room, folder) = await RequireFolderAccessAsync(companyId, folderId, user);
        var storagePath = await persistFile(cancellationToken);
        var document = Document.Create(clientId, companyId, documentName, documentType, fileName, fileSize,
            mimeType, storagePath, DocumentVisibility.Admin, createdBy: user.UserId);
        await _unitOfWork.Documents.AddAsync(document);
        await _repo.AddDocumentToFolderAsync(folderId, document.Id, user.UserId);
        await LogAsync(room, folder, document.Id, document.Name, DataRoomActions.DocumentAdded, user, "Upload");

        return new DataRoomUploadResult(document.Id);
    }

    public async Task RemoveDocumentFromFolderAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user)
    {
        var (room, folder) = await RequireFolderAccessAsync(companyId, folderId, user);
        var name = await _repo.GetDocumentNameAsync(documentId);
        await _repo.RemoveDocumentFromFolderAsync(folderId, documentId);
        await LogAsync(room, folder, documentId, name, DataRoomActions.DocumentRemoved, user);
    }

    public async Task SetFolderRolesAsync(Guid companyId, Guid folderId, IEnumerable<string> roles, DataRoomUser user)
    {
        if (!IsAdmin(user))
            throw new UnauthorizedAccessException("Apenas administradores podem alterar permissões de pastas.");

        var (room, folder) = await RequireFolderAccessAsync(companyId, folderId, user);
        var normalized = new List<string>();
        foreach (var r in roles.Select(r => r.Trim()).Where(r => r.Length > 0))
        {
            if (!ValidRoles.Contains(r))
                throw new ArgumentException($"Perfil inválido: {r}");
            normalized.Add(Enum.Parse<Role>(r, true).ToString());
        }

        await _repo.SetFolderRolesAsync(folderId, normalized, user.UserId);
        await LogAsync(room, folder, null, null, DataRoomActions.PermissionsChanged, user,
            normalized.Count == 0 ? "Acesso liberado a todos os perfis" : string.Join(", ", normalized.Distinct()));
    }

    public async Task<IEnumerable<DataRoomActivityLogResponse>> GetActivityLogsAsync(
        Guid companyId, DataRoomUser user, Guid? folderId, int limit)
    {
        if (!IsAdmin(user))
            throw new UnauthorizedAccessException("Apenas administradores podem visualizar o log de atividades.");

        var room = await _repo.GetByCompanyAsync(companyId);
        if (room == null) return Enumerable.Empty<DataRoomActivityLogResponse>();

        var logs = await _repo.GetActivityLogsAsync(room.Id, folderId, Math.Clamp(limit, 1, 500));
        return logs.Select(l => new DataRoomActivityLogResponse
        {
            Id = l.Id, FolderId = l.FolderId, FolderName = l.FolderName,
            DocumentId = l.DocumentId, DocumentName = l.DocumentName, Action = l.Action,
            UserId = l.UserId, UserName = l.UserName, Details = l.Details, CreatedAt = l.CreatedAt
        });
    }

    public async Task<FinancialUploadResult> UploadFinancialDocumentAsync(Guid clientId, Guid companyId, int year, int month,
        Guid? periodId, string? metricKey, string documentName, string documentType, string fileName, long fileSize, string mimeType,
        Func<Task<string>> persistFile, DataRoomUser user)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
            throw new ArgumentException("Período inválido.");

        if (periodId.HasValue && !await _repo.FinancialPeriodExistsAsync(periodId.Value, clientId, companyId))
            throw new KeyNotFoundException("Período financeiro não encontrado.");
        if (!string.IsNullOrWhiteSpace(metricKey) && !periodId.HasValue)
            throw new ArgumentException("Período é obrigatório ao vincular a um indicador.");

        var room = await GetOrCreateDataRoomAsync(companyId, user.UserId);
        var (_, folders, roles) = await LoadAsync(companyId);

        var root = await FindOrCreateFolderAsync(room.Id, null, FinancialFolderName, folders.Values, user);
        if (!IsAllowed(root.Id, folders, roles, user))
            throw new UnauthorizedAccessException("Seu perfil não tem acesso à pasta Financeiro.");

        var monthName = $"{month:00}-{year}";
        var monthFolder = await FindOrCreateFolderAsync(room.Id, root.Id, monthName, folders.Values, user);

        var storagePath = await persistFile();
        var doc = Document.Create(clientId, companyId, documentName, documentType, fileName, fileSize, mimeType,
            storagePath, DocumentVisibility.Admin, $"Métricas financeiras {monthName}",
            periodId.HasValue ? "financial_period" : null, periodId, null, user.UserId);
        await _unitOfWork.Documents.AddAsync(doc);

        await _repo.AddDocumentToFolderAsync(monthFolder.Id, doc.Id, user.UserId);
        if (periodId.HasValue)
        {
            await _repo.AddFinancialMetricDocumentAsync(new FinancialMetricDocument
            {
                PeriodId = periodId.Value,
                MetricKey = string.IsNullOrWhiteSpace(metricKey) ? null : metricKey.Trim(),
                DocumentId = doc.Id,
                FolderId = monthFolder.Id,
                CreatedBy = user.UserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        await LogAsync(new DataRoom { Id = room.Id }, monthFolder, doc.Id, doc.Name, DataRoomActions.DocumentAdded, user,
            string.IsNullOrWhiteSpace(metricKey)
                ? "Upload a partir das métricas financeiras"
                : $"Upload a partir do indicador {metricKey}");

        return new FinancialUploadResult
        {
            DocumentId = doc.Id,
            FolderId = monthFolder.Id,
            FolderPath = $"{FinancialFolderName}/{monthName}"
        };
    }

    private async Task<DataRoomFolder> FindOrCreateFolderAsync(Guid roomId, Guid? parentId, string name,
        IEnumerable<DataRoomFolder> existing, DataRoomUser user)
    {
        var found = existing.FirstOrDefault(f => f.ParentId == parentId
            && f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (found != null) return found;

        var folder = new DataRoomFolder
        {
            DataRoomId = roomId,
            ParentId = parentId,
            Name = name,
            DisplayOrder = 0,
            Visibility = "internal"
        };
        folder.CreatedBy = user.UserId;
        folder.UpdatedBy = user.UserId;
        await _repo.CreateFolderAsync(folder);
        await LogAsync(new DataRoom { Id = roomId }, folder, null, null, DataRoomActions.FolderCreated, user);
        return folder;
    }

    public async Task<IEnumerable<FinancialDocumentResponse>> GetFinancialDocumentsAsync(
        Guid clientId, Guid companyId, Guid periodId, DataRoomUser user)
    {
        if (!await _repo.FinancialPeriodExistsAsync(periodId, clientId, companyId))
            throw new KeyNotFoundException("Período financeiro não encontrado.");

        var (_, folders, roles) = await LoadAsync(companyId);
        var links = await _repo.GetFinancialMetricDocumentsAsync(periodId);
        return links
            .Where(l => IsAllowed(l.FolderId, folders, roles, user))
            .Select(l => new FinancialDocumentResponse
            {
                DocumentId = l.DocumentId, FolderId = l.FolderId, MetricKey = l.MetricKey,
                Name = l.DocumentName ?? string.Empty, FileName = l.FileName ?? string.Empty,
                FileSizeBytes = l.FileSizeBytes, DocumentType = l.DocumentType ?? string.Empty, CreatedAt = l.CreatedAt
            })
            .ToList();
    }

    public async Task RemoveFinancialDocumentAsync(Guid clientId, Guid companyId, Guid documentId, DataRoomUser user)
    {
        var link = await _repo.GetFinancialMetricDocumentAsync(documentId)
            ?? throw new KeyNotFoundException("Documento não encontrado.");
        if (!await _repo.FinancialPeriodExistsAsync(link.PeriodId, clientId, companyId))
            throw new KeyNotFoundException("Documento não encontrado.");

        var (room, folder) = await RequireFolderAccessAsync(companyId, link.FolderId, user);
        await _repo.RemoveDocumentFromFolderAsync(link.FolderId, documentId);
        await _repo.DeleteFinancialMetricDocumentAsync(documentId);
        await LogAsync(room, folder, documentId, link.DocumentName, DataRoomActions.DocumentRemoved, user,
            link.MetricKey == null ? null : $"Indicador {link.MetricKey}");
    }

    public async Task<Document> GetDocumentForDownloadAsync(Guid companyId, Guid folderId, Guid documentId, DataRoomUser user)
    {
        await RequireFolderAccessAsync(companyId, folderId, user);
        var docs = await _repo.GetDocumentsInFolderAsync(folderId);
        return docs.FirstOrDefault(d => d.Id == documentId)
            ?? throw new KeyNotFoundException("Documento não encontrado na pasta.");
    }
}

public static class DataRoomActions
{
    public const string DocumentAdded = "DocumentAdded";
    public const string DocumentRemoved = "DocumentRemoved";
    public const string FolderCreated = "FolderCreated";
    public const string FolderDeleted = "FolderDeleted";
    public const string PermissionsChanged = "PermissionsChanged";
}
