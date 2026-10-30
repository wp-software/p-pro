using PartnershipManager.Domain.Enums;

namespace PartnershipManager.Domain.Entities;

/// <summary>
/// Represents a shareholding owned by a shareholder in a company.
/// </summary>
public class Share : BaseEntity
{
    public Guid ClientId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ShareholderId { get; private set; }
    public ShareholderType ShareholderType { get; private set; }
    public Guid ShareClassId { get; private set; }
    public string? CertificateNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal AcquisitionPrice { get; private set; }
    public decimal TotalCost => Quantity * AcquisitionPrice;
    public DateTime AcquisitionDate { get; private set; }
    public ShareOrigin Origin { get; private set; }
    public Guid? OriginTransactionId { get; private set; }
    public ShareStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties (populated by repository)
    public string? ShareholderName { get; private set; }
    public string? ShareClassName { get; private set; }
    public string? ShareClassCode { get; private set; }
    public string? CompanyName { get; private set; }

    private Share() { }

    public static Share Create(
        Guid clientId,
        Guid companyId,
        Guid shareholderId,
        Guid shareClassId,
        decimal quantity,
        decimal acquisitionPrice,
        DateTime acquisitionDate,
        ShareOrigin origin,
        string? certificateNumber = null,
        Guid? originTransactionId = null,
        string? notes = null,
        Guid? createdBy = null)
    {
        ValidateQuantity(quantity);
        ValidatePrice(acquisitionPrice);

        return new Share
        {
            ClientId = clientId,
            CompanyId = companyId,
            ShareholderId = shareholderId,
            ShareClassId = shareClassId,
            CertificateNumber = certificateNumber?.Trim(),
            Quantity = quantity,
            AcquisitionPrice = acquisitionPrice,
            AcquisitionDate = acquisitionDate,
            Origin = origin,
            OriginTransactionId = originTransactionId,
            Status = ShareStatus.Active,
            Notes = notes?.Trim(),
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    /// <summary>
    /// Cancels this share holding. Called when shares are cancelled or transferred.
    /// </summary>
    public void Cancel(string? reason = null, Guid? updatedBy = null)
    {
        if (Status != ShareStatus.Active)
            throw new InvalidOperationException($"Cannot cancel share with status {Status}");

        Status = ShareStatus.Cancelled;
        Notes = string.IsNullOrEmpty(Notes)
            ? reason
            : $"{Notes}\n{DateTime.UtcNow:yyyy-MM-dd}: {reason}";
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks shares as transferred (original holding becomes inactive).
    /// </summary>
    public void MarkAsTransferred(Guid? updatedBy = null)
    {
        if (Status != ShareStatus.Active)
            throw new InvalidOperationException($"Cannot transfer share with status {Status}");

        Status = ShareStatus.Transferred;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks shares as converted to another class.
    /// </summary>
    public void MarkAsConverted(Guid? updatedBy = null)
    {
        if (Status != ShareStatus.Active)
            throw new InvalidOperationException($"Cannot convert share with status {Status}");

        Status = ShareStatus.Converted;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the certificate number.
    /// </summary>
    public void SetCertificateNumber(string certificateNumber, Guid? updatedBy = null)
    {
        CertificateNumber = certificateNumber.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates notes on the share.
    /// </summary>
    public void AddNote(string note, Guid? updatedBy = null)
    {
        Notes = string.IsNullOrEmpty(Notes)
            ? note
            : $"{Notes}\n{DateTime.UtcNow:yyyy-MM-dd}: {note}";
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links this share to its origin transaction.
    /// </summary>
    public void SetOriginTransaction(Guid transactionId)
    {
        OriginTransactionId = transactionId;
    }

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));
    }
}
