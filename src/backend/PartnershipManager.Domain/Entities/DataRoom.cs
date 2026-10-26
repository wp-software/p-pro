namespace PartnershipManager.Domain.Entities;

public class DataRoom : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<DataRoomFolder> Folders { get; set; } = new List<DataRoomFolder>();
}

public class DataRoomFolder : BaseEntity
{
    public Guid DataRoomId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Visibility { get; set; } = "internal";
    public ICollection<DataRoomFolder> SubFolders { get; set; } = new List<DataRoomFolder>();
}

public class DataRoomDocument : BaseEntity
{
    public Guid FolderId { get; set; }
    public Guid DocumentId { get; set; }
    public int DisplayOrder { get; set; }
    public Guid AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
}

public class DataRoomFolderRole
{
    public Guid FolderId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class DataRoomActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DataRoomId { get; set; }
    public Guid? FolderId { get; set; }
    public string? FolderName { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FinancialMetricDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PeriodId { get; set; }
    public string? MetricKey { get; set; }
    public Guid DocumentId { get; set; }
    public Guid FolderId { get; set; }
    public string? DocumentName { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? DocumentType { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
