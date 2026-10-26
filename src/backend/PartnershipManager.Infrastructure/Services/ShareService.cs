using PartnershipManager.Application.Features.Shares.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Persistence;

namespace PartnershipManager.Infrastructure.Services;

public interface IShareService
{
    // Shares
    Task<ShareListResponse> GetSharesPagedAsync(Guid clientId, Guid? companyId, int page, int pageSize, 
        Guid? shareholderId = null, Guid? shareClassId = null, string? status = null);
    Task<ShareResponse> GetShareByIdAsync(Guid id, Guid clientId);
    Task<IEnumerable<ShareResponse>> GetSharesByShareholderAsync(Guid clientId, Guid shareholderId);
    
    // Transactions
    Task<TransactionListResponse> GetTransactionsPagedAsync(Guid clientId, Guid? companyId, int page, int pageSize,
        string? transactionType = null, Guid? shareholderId = null, Guid? shareClassId = null,
        DateTime? fromDate = null, DateTime? toDate = null);
    Task<ShareTransactionResponse> GetTransactionByIdAsync(Guid id, Guid clientId);
    Task<IEnumerable<ShareTransactionResponse>> GetTransactionsByShareholderAsync(Guid clientId, Guid shareholderId);
    
    // Operations
    Task<ShareResponse> IssueSharesAsync(Guid clientId, IssueSharesRequest request, Guid? userId = null);
    Task<IReadOnlyList<ShareResponse>> IssueSharesBatchAsync(Guid clientId, IssueSharesBatchRequest request, Guid? userId = null);
    Task<ShareResponse> TransferSharesAsync(Guid clientId, TransferSharesRequest request, Guid? userId = null);
    Task CancelSharesAsync(Guid clientId, CancelSharesRequest request, Guid? userId = null);
    
    // Cap Table
    Task<CapTableResponse> GetCapTableAsync(Guid clientId, Guid companyId);
    Task<decimal> GetShareholderBalanceAsync(Guid clientId, Guid shareholderId, Guid shareClassId);
}

public class ShareService : IShareService
{
    private readonly IShareRepository _shareRepository;
    private readonly IShareTransactionRepository _transactionRepository;
    private readonly IShareClassRepository _shareClassRepository;
    private readonly IShareholderRepository _shareholderRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly DapperContext _dapperContext;

    public ShareService(
        IShareRepository shareRepository,
        IShareTransactionRepository transactionRepository,
        IShareClassRepository shareClassRepository,
        IShareholderRepository shareholderRepository,
        ICompanyRepository companyRepository,
        DapperContext dapperContext)
    {
        _shareRepository = shareRepository;
        _transactionRepository = transactionRepository;
        _shareClassRepository = shareClassRepository;
        _shareholderRepository = shareholderRepository;
        _companyRepository = companyRepository;
        _dapperContext = dapperContext;
    }

    #region Shares

    public async Task<ShareListResponse> GetSharesPagedAsync(
        Guid clientId, Guid? companyId, int page, int pageSize,
        Guid? shareholderId = null, Guid? shareClassId = null, string? status = null)
    {
        var (items, total, totalShares, totalValue) = await _shareRepository.GetPagedAsync(
            clientId, companyId, page, pageSize, shareholderId, shareClassId, status);
        
        var response = new ShareListResponse(items.Select(MapToShareResponse), total, page, pageSize)
        {
            TotalShares = totalShares,
            TotalValue = totalValue
        };
        
        return response;
    }

    public async Task<ShareResponse> GetShareByIdAsync(Guid id, Guid clientId)
    {
        var share = await _shareRepository.GetByIdAsync(id, clientId);
        if (share == null)
        {
            throw new NotFoundException("Share", id);
        }
        return MapToShareResponse(share);
    }

    public async Task<IEnumerable<ShareResponse>> GetSharesByShareholderAsync(Guid clientId, Guid shareholderId)
    {
        var shares = await _shareRepository.GetByShareholderAsync(clientId, shareholderId);
        return shares.Select(MapToShareResponse);
    }

    #endregion

    #region Transactions

    public async Task<TransactionListResponse> GetTransactionsPagedAsync(
        Guid clientId, Guid? companyId, int page, int pageSize,
        string? transactionType = null, Guid? shareholderId = null, Guid? shareClassId = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (items, total, totalQuantity, totalValue) = await _transactionRepository.GetPagedAsync(
            clientId, companyId, page, pageSize, transactionType, shareholderId, shareClassId, fromDate, toDate);
        
        var response = new TransactionListResponse(items.Select(MapToTransactionResponse), total, page, pageSize)
        {
            TotalQuantity = totalQuantity,
            TotalValue = totalValue
        };
        
        return response;
    }

    public async Task<ShareTransactionResponse> GetTransactionByIdAsync(Guid id, Guid clientId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, clientId);
        if (transaction == null)
        {
            throw new NotFoundException("ShareTransaction", id);
        }
        return MapToTransactionResponse(transaction);
    }

    public async Task<IEnumerable<ShareTransactionResponse>> GetTransactionsByShareholderAsync(Guid clientId, Guid shareholderId)
    {
        var transactions = await _transactionRepository.GetByShareholderAsync(clientId, shareholderId);
        return transactions.Select(MapToTransactionResponse);
    }

    #endregion

    #region Operations

    public async Task<ShareResponse> IssueSharesAsync(Guid clientId, IssueSharesRequest request, Guid? userId = null)
    {
        // Validate company
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", request.CompanyId);
        }

        // Validate shareholder
        var shareholder = await _shareholderRepository.GetByIdAsync(request.ShareholderId, clientId);
        if (shareholder == null)
        {
            throw new NotFoundException("Shareholder", request.ShareholderId);
        }

        // Validate share class
        var shareClass = await _shareClassRepository.GetByIdAsync(request.ShareClassId, clientId);
        if (shareClass == null || shareClass.CompanyId != request.CompanyId)
        {
            throw new NotFoundException("ShareClass", request.ShareClassId);
        }

        if (shareClass.Status != ShareClassStatus.Active)
        {
            throw new BusinessException("Não é possível emitir ações de uma classe inativa");
        }

        // Generate transaction number
        var transactionNumber = request.TransactionNumber ?? await _transactionRepository.GetNextTransactionNumberAsync(request.CompanyId);

        // Create transaction first (immutable record)
        var transaction = ShareTransaction.CreateIssuance(
            clientId,
            request.CompanyId,
            request.ShareClassId,
            request.ShareholderId,
            request.Quantity,
            request.PricePerShare,
            request.ReferenceDate,
            transactionNumber,
            request.Reason ?? "Share issuance",
            request.DocumentReference,
            request.Notes,
            userId,
            userId
        );

        await _transactionRepository.AddAsync(transaction);

        // Create share record
        var share = Share.Create(
            clientId,
            request.CompanyId,
            request.ShareholderId,
            request.ShareClassId,
            request.Quantity,
            request.PricePerShare,
            request.ReferenceDate,
            ShareOrigin.Issue,
            request.CertificateNumber,
            transaction.Id,
            request.Notes,
            userId
        );

        await _shareRepository.AddAsync(share);

        // Link transaction to share
        transaction.SetShareId(share.Id);

        // Reload share with navigation properties
        var createdShare = await _shareRepository.GetByIdAsync(share.Id, clientId);
        return MapToShareResponse(createdShare!);
    }

    public async Task<IReadOnlyList<ShareResponse>> IssueSharesBatchAsync(
        Guid clientId,
        IssueSharesBatchRequest request,
        Guid? userId = null)
    {
        if (request.TotalQuantity <= 0)
            throw new BusinessException("A quantidade total da emissão deve ser maior que zero");

        if (request.PricePerShare < 0)
            throw new BusinessException("O preço por ação não pode ser negativo");

        if (request.Allocations is null || request.Allocations.Count == 0)
            throw new BusinessException("Informe a distribuição das ações entre os sócios");

        if (request.Allocations.Any(allocation => allocation.Quantity <= 0))
            throw new BusinessException("A quantidade distribuída para cada sócio deve ser maior que zero");

        if (request.Allocations.Select(allocation => allocation.ShareholderId).Distinct().Count() != request.Allocations.Count)
            throw new BusinessException("Cada sócio deve aparecer apenas uma vez na distribuição");

        if (request.Allocations.Sum(allocation => allocation.Quantity) != request.TotalQuantity)
            throw new BusinessException("A soma das ações distribuídas deve ser igual ao total da emissão");

        await _dapperContext.BeginTransactionAsync();
        try
        {
            var shares = new List<ShareResponse>(request.Allocations.Count);
            foreach (var allocation in request.Allocations)
            {
                var share = await IssueSharesAsync(clientId, new IssueSharesRequest
                {
                    CompanyId = request.CompanyId,
                    ShareholderId = allocation.ShareholderId,
                    ShareClassId = request.ShareClassId,
                    Quantity = allocation.Quantity,
                    PricePerShare = request.PricePerShare,
                    ReferenceDate = request.ReferenceDate,
                    Reason = request.Reason,
                    DocumentReference = request.DocumentReference,
                    Notes = request.Notes
                }, userId);

                shares.Add(share);
            }

            await _dapperContext.CommitTransactionAsync();
            return shares;
        }
        catch
        {
            await _dapperContext.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ShareResponse> TransferSharesAsync(Guid clientId, TransferSharesRequest request, Guid? userId = null)
    {
        // Validate company
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", request.CompanyId);
        }

        // Validate shareholders
        var fromShareholder = await _shareholderRepository.GetByIdAsync(request.FromShareholderId, clientId);
        if (fromShareholder == null)
        {
            throw new NotFoundException("FromShareholder", request.FromShareholderId);
        }

        var toShareholder = await _shareholderRepository.GetByIdAsync(request.ToShareholderId, clientId);
        if (toShareholder == null)
        {
            throw new NotFoundException("ToShareholder", request.ToShareholderId);
        }

        // Validate share class
        var shareClass = await _shareClassRepository.GetByIdAsync(request.ShareClassId, clientId);
        if (shareClass == null || shareClass.CompanyId != request.CompanyId)
        {
            throw new NotFoundException("ShareClass", request.ShareClassId);
        }

        // Check balance
        var balance = await _shareRepository.GetShareholderBalanceAsync(clientId, request.FromShareholderId, request.ShareClassId);
        if (balance < request.Quantity)
        {
            throw new BusinessException($"Saldo insuficiente. Disponível: {balance}, Solicitado: {request.Quantity}");
        }

        // Generate transaction number
        var transactionNumber = request.TransactionNumber ?? await _transactionRepository.GetNextTransactionNumberAsync(request.CompanyId);

        // Create transfer transaction
        var transaction = ShareTransaction.CreateTransfer(
            clientId,
            request.CompanyId,
            request.ShareClassId,
            request.FromShareholderId,
            request.ToShareholderId,
            request.Quantity,
            request.PricePerShare,
            request.ReferenceDate,
            null,
            transactionNumber,
            request.Reason ?? "Share transfer",
            request.DocumentReference,
            request.Notes,
            userId,
            userId
        );

        await _transactionRepository.AddAsync(transaction);

        // Mark original shares as transferred (we need to find and update them)
        // For simplicity, we'll create new share record for the receiver
        var newShare = Share.Create(
            clientId,
            request.CompanyId,
            request.ToShareholderId,
            request.ShareClassId,
            request.Quantity,
            request.PricePerShare,
            request.ReferenceDate,
            ShareOrigin.Transfer,
            null,
            transaction.Id,
            $"Transferred from {fromShareholder.Name}",
            userId
        );

        await _shareRepository.AddAsync(newShare);

        // Update the source shareholder's shares (mark quantity as transferred)
        // This is a simplified approach - in production you might track individual share lots
        var sourceShares = await _shareRepository.GetByShareholderAsync(clientId, request.FromShareholderId);
        var classShares = sourceShares.Where(s => s.ShareClassId == request.ShareClassId && s.Status == ShareStatus.Active).ToList();
        
        decimal remainingToTransfer = request.Quantity;
        foreach (var sourceShare in classShares.OrderBy(s => s.AcquisitionDate))
        {
            if (remainingToTransfer <= 0) break;
            
            if (sourceShare.Quantity <= remainingToTransfer)
            {
                sourceShare.MarkAsTransferred(userId);
                await _shareRepository.UpdateAsync(sourceShare);
                remainingToTransfer -= sourceShare.Quantity;
            }
            // For partial transfers, we'd need to split the share record
        }

        var createdShare = await _shareRepository.GetByIdAsync(newShare.Id, clientId);
        return MapToShareResponse(createdShare!);
    }

    public async Task CancelSharesAsync(Guid clientId, CancelSharesRequest request, Guid? userId = null)
    {
        // Validate company
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", request.CompanyId);
        }

        // Validate shareholder
        var shareholder = await _shareholderRepository.GetByIdAsync(request.ShareholderId, clientId);
        if (shareholder == null)
        {
            throw new NotFoundException("Shareholder", request.ShareholderId);
        }

        // Check balance
        var balance = await _shareRepository.GetShareholderBalanceAsync(clientId, request.ShareholderId, request.ShareClassId);
        if (balance < request.Quantity)
        {
            throw new BusinessException($"Saldo insuficiente para cancelamento. Disponível: {balance}, Solicitado: {request.Quantity}");
        }

        // Generate transaction number
        var transactionNumber = request.TransactionNumber ?? await _transactionRepository.GetNextTransactionNumberAsync(request.CompanyId);

        // Create cancellation transaction
        var transaction = ShareTransaction.CreateCancellation(
            clientId,
            request.CompanyId,
            request.ShareClassId,
            request.ShareholderId,
            request.Quantity,
            0, // Cancelled at 0 value
            request.ReferenceDate,
            null,
            transactionNumber,
            request.Reason,
            request.DocumentReference,
            request.Notes,
            userId,
            userId
        );

        await _transactionRepository.AddAsync(transaction);

        // Cancel share records
        var shares = await _shareRepository.GetByShareholderAsync(clientId, request.ShareholderId);
        var classShares = shares.Where(s => s.ShareClassId == request.ShareClassId && s.Status == ShareStatus.Active).ToList();
        
        decimal remainingToCancel = request.Quantity;
        foreach (var share in classShares.OrderBy(s => s.AcquisitionDate))
        {
            if (remainingToCancel <= 0) break;
            
            if (share.Quantity <= remainingToCancel)
            {
                share.Cancel(request.Reason, userId);
                await _shareRepository.UpdateAsync(share);
                remainingToCancel -= share.Quantity;
            }
        }
    }

    #endregion

    #region Cap Table

    public async Task<CapTableResponse> GetCapTableAsync(Guid clientId, Guid companyId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", companyId);
        }

        var shares = await _shareRepository.GetActiveByCompanyAsync(clientId, companyId);
        var sharesList = shares.ToList();
        
        var totalShares = sharesList.Sum(s => s.Quantity);
        var totalValue = sharesList.Sum(s => s.Quantity * s.AcquisitionPrice);

        // Group by shareholder and share class
        var entries = sharesList
            .GroupBy(s => new { s.ShareholderId, s.ShareClassId })
            .Select(g => new CapTableEntryResponse
            {
                ShareholderId = g.Key.ShareholderId,
                ShareholderName = g.First().ShareholderName ?? "Unknown",
                ShareholderType = ShareholderType.Founder, // Would need to join with shareholders table
                ShareholderTypeDescription = "Founder",
                ShareClassId = g.Key.ShareClassId,
                ShareClassName = g.First().ShareClassName ?? "Unknown",
                ShareClassCode = g.First().ShareClassCode ?? "Unknown",
                TotalShares = g.Sum(s => s.Quantity),
                TotalValue = g.Sum(s => s.Quantity * s.AcquisitionPrice),
                OwnershipPercentage = totalShares > 0 ? Math.Round(g.Sum(s => s.Quantity) / totalShares * 100, 2) : 0,
                VotingPercentage = 0, // Would need share class voting info
                FullyDilutedPercentage = totalShares > 0 ? Math.Round(g.Sum(s => s.Quantity) / totalShares * 100, 2) : 0
            })
            .OrderByDescending(e => e.TotalShares)
            .ToList();

        // Summary by type
        var summaryByType = entries
            .GroupBy(e => e.ShareholderType)
            .Select(g => new CapTableSummaryByType
            {
                Type = g.Key,
                TypeDescription = g.Key.ToString(),
                ShareholderCount = g.Select(e => e.ShareholderId).Distinct().Count(),
                TotalShares = g.Sum(e => e.TotalShares),
                OwnershipPercentage = totalShares > 0 ? Math.Round(g.Sum(e => e.TotalShares) / totalShares * 100, 2) : 0
            })
            .ToList();

        // Summary by class
        var summaryByClass = entries
            .GroupBy(e => e.ShareClassId)
            .Select(g => new CapTableSummaryByClass
            {
                ShareClassId = g.Key,
                ShareClassName = g.First().ShareClassName,
                ShareClassCode = g.First().ShareClassCode,
                TotalShares = g.Sum(e => e.TotalShares),
                OwnershipPercentage = totalShares > 0 ? Math.Round(g.Sum(e => e.TotalShares) / totalShares * 100, 2) : 0
            })
            .ToList();

        return new CapTableResponse
        {
            CompanyId = companyId,
            CompanyName = company.Name,
            AsOfDate = DateTime.UtcNow.Date,
            TotalShares = totalShares,
            TotalValue = totalValue,
            TotalVotingShares = totalShares, // Simplified
            Entries = entries,
            SummaryByType = summaryByType,
            SummaryByClass = summaryByClass
        };
    }

    public async Task<decimal> GetShareholderBalanceAsync(Guid clientId, Guid shareholderId, Guid shareClassId)
    {
        return await _shareRepository.GetShareholderBalanceAsync(clientId, shareholderId, shareClassId);
    }

    #endregion

    #region Mappers

    private static ShareResponse MapToShareResponse(Share share)
    {
        return new ShareResponse
        {
            Id = share.Id,
            ClientId = share.ClientId,
            CompanyId = share.CompanyId,
            CompanyName = share.CompanyName ?? string.Empty,
            ShareholderId = share.ShareholderId,
            ShareholderName = share.ShareholderName ?? string.Empty,
            ShareClassId = share.ShareClassId,
            ShareClassName = share.ShareClassName ?? string.Empty,
            ShareClassCode = share.ShareClassCode ?? string.Empty,
            CertificateNumber = share.CertificateNumber,
            Quantity = share.Quantity,
            AcquisitionPrice = share.AcquisitionPrice,
            TotalCost = share.TotalCost,
            AcquisitionDate = share.AcquisitionDate,
            Origin = share.Origin,
            OriginDescription = share.Origin.ToString(),
            OriginTransactionId = share.OriginTransactionId,
            Status = share.Status,
            StatusDescription = share.Status.ToString(),
            Notes = share.Notes,
            CreatedAt = share.CreatedAt,
            UpdatedAt = share.UpdatedAt
        };
    }

    private static ShareTransactionResponse MapToTransactionResponse(ShareTransaction transaction)
    {
        return new ShareTransactionResponse
        {
            Id = transaction.Id,
            ClientId = transaction.ClientId,
            CompanyId = transaction.CompanyId,
            CompanyName = transaction.CompanyName ?? string.Empty,
            TransactionType = transaction.TransactionType,
            TransactionTypeDescription = transaction.TransactionType.ToString(),
            TransactionNumber = transaction.TransactionNumber,
            ReferenceDate = transaction.ReferenceDate,
            ShareId = transaction.ShareId,
            ShareClassId = transaction.ShareClassId,
            ShareClassName = transaction.ShareClassName ?? string.Empty,
            ShareClassCode = transaction.ShareClassCode ?? string.Empty,
            Quantity = transaction.Quantity,
            PricePerShare = transaction.PricePerShare,
            TotalValue = transaction.TotalValue,
            FromShareholderId = transaction.FromShareholderId,
            FromShareholderName = transaction.FromShareholderName,
            ToShareholderId = transaction.ToShareholderId,
            ToShareholderName = transaction.ToShareholderName,
            Reason = transaction.Reason,
            DocumentReference = transaction.DocumentReference,
            Notes = transaction.Notes,
            ApprovedBy = transaction.ApprovedBy,
            ApprovedByName = transaction.ApprovedByName,
            ApprovedAt = transaction.ApprovedAt,
            CreatedAt = transaction.CreatedAt
        };
    }

    #endregion
}
