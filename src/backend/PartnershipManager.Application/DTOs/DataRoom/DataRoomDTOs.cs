namespace PartnershipManager.Application.DTOs.DataRoom;

public class DataRoomResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DataRoomFolderResponse
{
    public Guid Id { get; set; }
    public Guid DataRoomId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> AllowedRoles { get; set; } = new();
}

public class SetFolderRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

public class DataRoomActivityLogResponse
{
    public Guid Id { get; set; }
    public Guid? FolderId { get; set; }
    public string? FolderName { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFolderRequest
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Visibility { get; set; } = "internal";
}

public class AddDocumentToFolderRequest
{
    public Guid DocumentId { get; set; }
}

public record DataRoomUser(Guid UserId, string? UserName, IReadOnlyCollection<string> Roles);

public class FinancialUploadResult
{
    public Guid DocumentId { get; set; }
    public Guid FolderId { get; set; }
    public string FolderPath { get; set; } = string.Empty;
}

public class FinancialDocumentResponse
{
    public Guid DocumentId { get; set; }
    public Guid FolderId { get; set; }
    public string? MetricKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
