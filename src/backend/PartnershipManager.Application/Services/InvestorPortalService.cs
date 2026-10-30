using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.Communication;
using PartnershipManager.Application.DTOs.Portal;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Application.Services;

public interface IInvestorPortalService
{
    Task<InvestorSummaryResponse> GetSummaryAsync(Guid userId, Guid companyId);
    Task<PagedResult<CommunicationListResponse>> GetCommunicationsAsync(Guid userId, Guid companyId, int page, int pageSize);
    Task<IEnumerable<Document>> GetDocumentsAsync(Guid userId, Guid companyId);
}

public class InvestorPortalService : IInvestorPortalService
{
    private readonly ICommunicationRepository _communicationRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IShareholderRepository _shareholderRepo;
    private readonly IShareRepository _shareRepo;
    private readonly IValuationRepository _valuationRepo;
    private readonly ICompanyRepository _companyRepo;

    public InvestorPortalService(
        ICommunicationRepository communicationRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        IShareholderRepository shareholderRepo,
        IShareRepository shareRepo,
        IValuationRepository valuationRepo,
        ICompanyRepository companyRepo)
    {
        _communicationRepo = communicationRepo;
        _documentRepo = documentRepo;
        _userRepo = userRepo;
        _shareholderRepo = shareholderRepo;
        _shareRepo = shareRepo;
        _valuationRepo = valuationRepo;
        _companyRepo = companyRepo;
    }

    public async Task<InvestorSummaryResponse> GetSummaryAsync(Guid userId, Guid companyId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        var clientId = user?.ClientId ?? Guid.Empty;

        var company = clientId != Guid.Empty
            ? await _companyRepo.GetByIdAsync(companyId)
            : null;

        var (_, docsTotal) = await _documentRepo.GetPagedAsync(
            clientId: clientId, companyId: companyId, page: 1, pageSize: 1,
            visibility: "investors");

        var response = new InvestorSummaryResponse
        {
            InvestorName = user?.Name ?? string.Empty,
            CompanyName = company?.Name ?? string.Empty,
            DocumentsCount = docsTotal
        };

        if (user?.ShareholderId == null || clientId == Guid.Empty)
            return response;

        if (!await _shareholderRepo.IsLinkedToCompanyAsync(user.ShareholderId.Value, companyId))
            return response;

        var shareholder = await _shareholderRepo.GetByIdAsync(user.ShareholderId.Value, clientId);
        if (shareholder == null)
            return response;

        var shares = await _shareRepo.GetByShareholderAsync(clientId, shareholder.Id);
        var totalShares = shares.Sum(s => s.Quantity);
        var companyTotalShares = await _shareRepo.GetTotalSharesByCompanyAsync(clientId, companyId);

        var ownershipPercentage = companyTotalShares > 0
            ? Math.Round(totalShares * 100 / companyTotalShares, 4)
            : 0m;

        var latestValuation = await _valuationRepo.GetLastApprovedAsync(clientId, companyId);
        var currentValuation = latestValuation?.ValuationAmount ?? 0m;
        var estimatedValue = currentValuation > 0
            ? Math.Round(ownershipPercentage / 100 * currentValuation, 2)
            : 0m;

        response.TotalShares = totalShares;
        response.OwnershipPercentage = ownershipPercentage;
        response.CurrentValuation = currentValuation;
        response.EstimatedValue = estimatedValue;

        return response;
    }

    public async Task<PagedResult<CommunicationListResponse>> GetCommunicationsAsync(
        Guid userId, Guid companyId, int page, int pageSize)
    {
        var items = await _communicationRepo.GetForRoleAsync(companyId, "investors", pageSize * page);
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize);
        var mapped = paged.Select(c => new CommunicationListResponse
        {
            Id = c.Id,
            Title = c.Title,
            CommType = c.CommType,
            Visibility = c.Visibility,
            IsPinned = c.IsPinned,
            PublishedAt = c.PublishedAt,
            CreatedAt = c.CreatedAt,
            ViewsCount = c.ViewsCount,
            Summary = c.Summary
        });
        return new PagedResult<CommunicationListResponse>(mapped, items.Count(), page, pageSize);
    }

    public async Task<IEnumerable<Document>> GetDocumentsAsync(Guid userId, Guid companyId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        var clientId = user?.ClientId ?? Guid.Empty;

        var (items, _) = await _documentRepo.GetPagedAsync(
            clientId: clientId, companyId: companyId, page: 1, pageSize: 200,
            visibility: "investors");
        return items;
    }
}
