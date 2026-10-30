using PartnershipManager.Application.Features.ShareClasses.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Services;

public interface IShareClassService
{
    Task<ShareClassListResponse> GetPagedAsync(Guid clientId, Guid? companyId, int page, int pageSize, string? search, string? status);
    Task<IEnumerable<ShareClassSummaryResponse>> GetByCompanyAsync(Guid clientId, Guid companyId);
    Task<ShareClassResponse> GetByIdAsync(Guid id, Guid clientId);
    Task<ShareClassResponse> CreateAsync(Guid clientId, CreateShareClassRequest request, Guid? userId = null);
    Task<ShareClassResponse> UpdateAsync(Guid id, Guid clientId, UpdateShareClassRequest request, Guid? userId = null);
    Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null);
}

public class ShareClassService : IShareClassService
{
    private readonly IShareClassRepository _shareClassRepository;
    private readonly ICompanyRepository _companyRepository;

    public ShareClassService(
        IShareClassRepository shareClassRepository,
        ICompanyRepository companyRepository)
    {
        _shareClassRepository = shareClassRepository;
        _companyRepository = companyRepository;
    }

    public async Task<ShareClassListResponse> GetPagedAsync(
        Guid clientId, 
        Guid? companyId, 
        int page, 
        int pageSize, 
        string? search, 
        string? status)
    {
        var (items, total) = await _shareClassRepository.GetPagedAsync(clientId, companyId, page, pageSize, search, status);
        var responseItems = items.Select(MapToResponse);
        return new ShareClassListResponse(responseItems, total, page, pageSize);
    }

    public async Task<IEnumerable<ShareClassSummaryResponse>> GetByCompanyAsync(Guid clientId, Guid companyId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", companyId);
        }

        var items = await _shareClassRepository.GetByCompanyAsync(clientId, companyId);
        return items.Select(sc => new ShareClassSummaryResponse
        {
            Id = sc.Id,
            Name = sc.Name,
            Code = sc.Code,
            HasVotingRights = sc.HasVotingRights,
            LiquidationPreference = sc.LiquidationPreference,
            Status = sc.Status
        });
    }

    public async Task<ShareClassResponse> GetByIdAsync(Guid id, Guid clientId)
    {
        var shareClass = await _shareClassRepository.GetByIdAsync(id, clientId);
        if (shareClass == null)
        {
            throw new NotFoundException("ShareClass", id);
        }
        return MapToResponse(shareClass);
    }

    public async Task<ShareClassResponse> CreateAsync(Guid clientId, CreateShareClassRequest request, Guid? userId = null)
    {
        var codeExists = await _shareClassRepository.CodeExistsAsync(clientId, null, request.Code);
        if (codeExists)
        {
            throw new ConflictException($"Já existe uma classe de ações com o código '{request.Code}' neste tenant");
        }

        if (request.IsConvertible && request.ConvertsToClassId.HasValue)
        {
            var target = await _shareClassRepository.GetByIdAsync(request.ConvertsToClassId.Value, clientId);
            if (target == null || target.CompanyId.HasValue)
            {
                throw new NotFoundException("Classe de destino para conversão", request.ConvertsToClassId.Value);
            }
        }

        var shareClass = ShareClass.Create(
            clientId,
            null,
            request.Name,
            request.Code,
            request.Description,
            request.HasVotingRights,
            request.VotesPerShare,
            request.LiquidationPreference,
            request.Participating,
            request.DividendPreference,
            request.IsConvertible,
            request.ConvertsToClassId,
            request.ConversionRatio,
            request.AntiDilutionType,
            request.Rights,
            request.DisplayOrder,
            userId);

        await _shareClassRepository.AddAsync(shareClass);

        // Reload to get navigation properties
        var created = await _shareClassRepository.GetByIdAsync(shareClass.Id, clientId);
        return MapToResponse(created!);
    }

    public async Task<ShareClassResponse> UpdateAsync(Guid id, Guid clientId, UpdateShareClassRequest request, Guid? userId = null)
    {
        var shareClass = await _shareClassRepository.GetByIdAsync(id, clientId);
        if (shareClass == null)
        {
            throw new NotFoundException("ShareClass", id);
        }

        // A tenant-wide class and a company-specific legacy class use separate code scopes.
        var codeExists = await _shareClassRepository.CodeExistsAsync(clientId, shareClass.CompanyId, request.Code, id);
        if (codeExists)
        {
            var scope = shareClass.CompanyId.HasValue ? "nesta empresa" : "neste tenant";
            throw new ConflictException($"Já existe outra classe de ações com o código '{request.Code}' {scope}");
        }

        // Validate conversion target exists
        if (request.IsConvertible && request.ConvertsToClassId.HasValue)
        {
            if (request.ConvertsToClassId.Value == id)
            {
                throw new BusinessException("Uma classe de ações não pode converter para si mesma");
            }

            var target = await _shareClassRepository.GetByIdAsync(request.ConvertsToClassId.Value, clientId);
            if (target == null ||
                (target.CompanyId.HasValue && target.CompanyId != shareClass.CompanyId))
            {
                throw new NotFoundException("Classe de destino para conversão", request.ConvertsToClassId.Value);
            }
        }

        shareClass.Update(
            request.Name,
            request.Code,
            request.Description,
            request.HasVotingRights,
            request.VotesPerShare,
            request.LiquidationPreference,
            request.Participating,
            request.DividendPreference,
            request.IsConvertible,
            request.ConvertsToClassId,
            request.ConversionRatio,
            request.AntiDilutionType,
            request.Rights,
            request.DisplayOrder,
            userId);

        await _shareClassRepository.UpdateAsync(shareClass);

        // Reload to get navigation properties
        var updated = await _shareClassRepository.GetByIdAsync(id, clientId);
        return MapToResponse(updated!);
    }

    public async Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null)
    {
        var exists = await _shareClassRepository.ExistsAsync(id, clientId);
        if (!exists)
        {
            throw new NotFoundException("ShareClass", id);
        }

        // Check if there are shares using this class
        var hasShares = await _shareClassRepository.HasSharesAsync(id);
        if (hasShares)
        {
            throw new BusinessException("Não é possível excluir uma classe de ações que possui ações emitidas");
        }

        await _shareClassRepository.SoftDeleteAsync(id, clientId, userId);
    }

    private static ShareClassResponse MapToResponse(ShareClass shareClass)
    {
        return new ShareClassResponse
        {
            Id = shareClass.Id,
            ClientId = shareClass.ClientId,
            CompanyId = shareClass.CompanyId,
            CompanyName = shareClass.CompanyName,
            Name = shareClass.Name,
            Code = shareClass.Code,
            Description = shareClass.Description,
            HasVotingRights = shareClass.HasVotingRights,
            VotesPerShare = shareClass.VotesPerShare,
            LiquidationPreference = shareClass.LiquidationPreference,
            Participating = shareClass.Participating,
            DividendPreference = shareClass.DividendPreference,
            IsConvertible = shareClass.IsConvertible,
            ConvertsToClassId = shareClass.ConvertsToClassId,
            ConvertsToClassName = shareClass.ConvertsToClassName,
            ConversionRatio = shareClass.ConversionRatio,
            AntiDilutionType = shareClass.AntiDilutionType,
            AntiDilutionTypeDescription = shareClass.AntiDilutionType?.ToString(),
            Rights = shareClass.Rights,
            Status = shareClass.Status,
            StatusDescription = shareClass.Status.ToString(),
            DisplayOrder = shareClass.DisplayOrder,
            CreatedAt = shareClass.CreatedAt,
            UpdatedAt = shareClass.UpdatedAt
        };
    }
}
