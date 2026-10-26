using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Shareholders.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Services;

public interface IShareholderService
{
    Task<ShareholderListResponse> GetPagedAsync(Guid clientId, Guid? companyId, int page, int pageSize, string? search, string? type, string? status, string? sortDirection = null);
    Task<ShareholderResponse> GetByIdAsync(Guid id, Guid clientId);
    Task<ShareholderResponse> CreateAsync(Guid clientId, CreateShareholderRequest request, Guid? userId = null);
    Task<ShareholderResponse> UpdateAsync(Guid id, Guid clientId, UpdateShareholderRequest request, Guid? userId = null);
    Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null);
}

public class ShareholderService : IShareholderService
{
    private readonly IShareholderRepository _shareholderRepository;
    private readonly ICompanyRepository _companyRepository;

    public ShareholderService(
        IShareholderRepository shareholderRepository,
        ICompanyRepository companyRepository)
    {
        _shareholderRepository = shareholderRepository;
        _companyRepository = companyRepository;
    }

    public async Task<ShareholderListResponse> GetPagedAsync(Guid clientId, Guid? companyId, int page, int pageSize, string? search, string? type, string? status, string? sortDirection = null)
    {
        var (items, total) = await _shareholderRepository.GetPagedAsync(clientId, companyId, page, pageSize, search, type, status, sortDirection);
        var itemList = items.ToList();
        var links = await _shareholderRepository.GetCompanyLinksAsync(itemList.Select(i => i.Id));
        var responseItems = itemList.Select(i => MapToResponse(i, links.GetValueOrDefault(i.Id)));
        return new ShareholderListResponse(responseItems, total, page, pageSize);
    }

    public async Task<ShareholderResponse> GetByIdAsync(Guid id, Guid clientId)
    {
        var shareholder = await _shareholderRepository.GetByIdAsync(id, clientId);
        if (shareholder == null)
        {
            throw new NotFoundException("Shareholder", id);
        }
        return await MapWithCompaniesAsync(shareholder);
    }

    private async Task<ShareholderResponse> MapWithCompaniesAsync(Shareholder shareholder)
    {
        var links = await _shareholderRepository.GetCompanyLinksAsync(new[] { shareholder.Id });
        return MapToResponse(shareholder, links.GetValueOrDefault(shareholder.Id));
    }

    public async Task<ShareholderResponse> CreateAsync(Guid clientId, CreateShareholderRequest request, Guid? userId = null)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != clientId)
        {
            throw new NotFoundException("Company", request.CompanyId);
        }

        // Mesma pessoa em outra empresa do grupo: apenas adiciona o vínculo
        var existing = await _shareholderRepository.GetByDocumentAsync(clientId, request.Document);
        if (existing != null)
        {
            var existingLinks = await _shareholderRepository.GetCompanyLinksAsync(new[] { existing.Id });
            if (existingLinks.GetValueOrDefault(existing.Id)?.Any(l => l.CompanyId == request.CompanyId) == true)
            {
                throw new ConflictException("Sócio já vinculado a esta empresa");
            }

            await UpsertCompaniesAsync(existing, clientId, BuildConfigs(request.CompanyId, request.Type, request.EarnOut,
                request.TagAlong, request.DragAlong, request.ShareholdersAgreement, request.RightOfFirstRefusal,
                request.LiquidationPreferenceRight, request.AntiDilution, request.Companies));
            return await MapWithCompaniesAsync(existing);
        }

        var shareholder = Shareholder.Create(
            clientId,
            request.CompanyId,
            request.Name,
            request.Document,
            request.DocumentType,
            request.Type,
            request.Email,
            request.Phone,
            ShareholderStatus.Active,
            request.Notes,
            request.AddressStreet,
            request.AddressNumber,
            request.AddressComplement,
            request.AddressZipCode,
            request.AddressCity,
            request.AddressState,
            request.MaritalStatus,
            request.Gender,
            request.BirthDate,
            request.EarnOut,
            request.TagAlong,
            request.DragAlong,
            request.ShareholdersAgreement,
            request.RightOfFirstRefusal,
            request.LiquidationPreferenceRight,
            request.AntiDilution,
            userId);

        shareholder.SetCompanyName(company.Name);

        await _shareholderRepository.AddAsync(shareholder);
        await UpsertCompaniesAsync(shareholder, clientId, BuildConfigs(request.CompanyId, request.Type, request.EarnOut,
            request.TagAlong, request.DragAlong, request.ShareholdersAgreement, request.RightOfFirstRefusal,
            request.LiquidationPreferenceRight, request.AntiDilution, request.Companies));

        return await MapWithCompaniesAsync(shareholder);
    }

    public async Task<ShareholderResponse> UpdateAsync(Guid id, Guid clientId, UpdateShareholderRequest request, Guid? userId = null)
    {
        var shareholder = await _shareholderRepository.GetByIdAsync(id, clientId);
        if (shareholder == null)
        {
            throw new NotFoundException("Shareholder", id);
        }

        if (request.CompanyId.HasValue && request.CompanyId.Value != shareholder.CompanyId)
        {
            var company = await _companyRepository.GetByIdAsync(request.CompanyId.Value);
            if (company == null || company.ClientId != clientId)
            {
                throw new NotFoundException("Company", request.CompanyId.Value);
            }
            shareholder.ChangeCompany(request.CompanyId.Value, userId);
            shareholder.SetCompanyName(company.Name);
        }

        if (!string.IsNullOrWhiteSpace(request.Document) && request.DocumentType.HasValue)
        {
            if (await _shareholderRepository.DocumentExistsAsync(clientId, request.Document, id))
            {
                throw new ConflictException("Documento já cadastrado para este cliente");
            }
            shareholder.UpdateDocument(request.Document, request.DocumentType.Value, userId);
        }

        var configs = BuildConfigs(shareholder.CompanyId, request.Type, request.EarnOut, request.TagAlong,
            request.DragAlong, request.ShareholdersAgreement, request.RightOfFirstRefusal,
            request.LiquidationPreferenceRight, request.AntiDilution, request.Companies);
        var toRemove = request.Companies is { Count: > 0 }
            ? await GetLinksToRemoveAsync(shareholder, configs)
            : new List<ShareholderCompanyLink>();
        foreach (var link in toRemove)
        {
            if (await _shareholderRepository.HasPositionInCompanyAsync(shareholder.Id, link.CompanyId))
            {
                throw new ConflictException(
                    $"Não é possível remover a empresa \"{link.CompanyName}\": o sócio possui ações ou vesting nela.");
            }
        }

        shareholder.UpdateInfo(
            request.Name,
            request.Email ?? shareholder.Email,
            request.Phone ?? shareholder.Phone,
            request.Type,
            request.Status,
            request.Notes ?? shareholder.Notes,
            request.AddressStreet ?? shareholder.AddressStreet,
            request.AddressNumber ?? shareholder.AddressNumber,
            request.AddressComplement ?? shareholder.AddressComplement,
            request.AddressZipCode ?? shareholder.AddressZipCode,
            request.AddressCity ?? shareholder.AddressCity,
            request.AddressState ?? shareholder.AddressState,
            request.MaritalStatus ?? shareholder.MaritalStatus,
            request.Gender ?? shareholder.Gender,
            request.BirthDate ?? shareholder.BirthDate,
            request.EarnOut,
            request.TagAlong,
            request.DragAlong,
            request.ShareholdersAgreement,
            request.RightOfFirstRefusal,
            request.LiquidationPreferenceRight,
            request.AntiDilution,
            userId);

        await _shareholderRepository.UpdateAsync(shareholder);

        await UpsertCompaniesAsync(shareholder, clientId, configs);
        foreach (var link in toRemove)
        {
            await _shareholderRepository.RemoveCompanyLinkAsync(shareholder.Id, link.CompanyId);
        }

        return await MapWithCompaniesAsync(shareholder);
    }

    // A empresa principal sempre vem primeiro e usa os dados de nível superior quando não listada
    private static List<ShareholderCompanyConfig> BuildConfigs(
        Guid primaryCompanyId, ShareholderType type, bool earnOut, bool tagAlong, bool dragAlong,
        bool shareholdersAgreement, bool rightOfFirstRefusal, bool liquidationPreferenceRight, bool antiDilution,
        List<ShareholderCompanyConfigRequest>? companies)
    {
        var configs = (companies ?? new List<ShareholderCompanyConfigRequest>())
            .GroupBy(c => c.CompanyId)
            .Select(g => g.Last())
            .Select(c => new ShareholderCompanyConfig(c.CompanyId, c.Type, c.EarnOut, c.TagAlong, c.DragAlong,
                c.ShareholdersAgreement, c.RightOfFirstRefusal, c.LiquidationPreferenceRight, c.AntiDilution))
            .ToList();

        if (configs.All(c => c.CompanyId != primaryCompanyId))
        {
            configs.Insert(0, new ShareholderCompanyConfig(primaryCompanyId, type, earnOut, tagAlong, dragAlong,
                shareholdersAgreement, rightOfFirstRefusal, liquidationPreferenceRight, antiDilution));
        }
        return configs;
    }

    private async Task UpsertCompaniesAsync(Shareholder shareholder, Guid clientId, List<ShareholderCompanyConfig> configs)
    {
        foreach (var config in configs)
        {
            var company = await _companyRepository.GetByIdAsync(config.CompanyId);
            if (company == null || company.ClientId != clientId)
            {
                throw new NotFoundException("Company", config.CompanyId);
            }
        }

        foreach (var config in configs)
        {
            await _shareholderRepository.UpsertCompanyLinkAsync(shareholder.Id, clientId, config);
        }
    }

    private async Task<List<ShareholderCompanyLink>> GetLinksToRemoveAsync(Shareholder shareholder, List<ShareholderCompanyConfig> configs)
    {
        var current = (await _shareholderRepository.GetCompanyLinksAsync(new[] { shareholder.Id }))
            .GetValueOrDefault(shareholder.Id) ?? new List<ShareholderCompanyLink>();

        return current
            .Where(c => c.CompanyId != shareholder.CompanyId && configs.All(d => d.CompanyId != c.CompanyId))
            .ToList();
    }

    public async Task DeleteAsync(Guid id, Guid clientId, Guid? userId = null)
    {
        if (!await _shareholderRepository.ExistsAsync(id, clientId))
        {
            throw new NotFoundException("Shareholder", id);
        }

        await _shareholderRepository.SoftDeleteAsync(id, clientId, userId);
    }

    private static ShareholderResponse MapToResponse(Shareholder shareholder, List<ShareholderCompanyLink>? links = null)
    {
        return new ShareholderResponse
        {
            Id = shareholder.Id,
            ClientId = shareholder.ClientId,
            CompanyId = shareholder.CompanyId,
            CompanyName = shareholder.CompanyName ?? string.Empty,
            Companies = (links ?? new List<ShareholderCompanyLink>())
                .Select(l => new ShareholderCompanyDto(l.CompanyId, l.CompanyName, l.Config.Type, l.Config.EarnOut,
                    l.Config.TagAlong, l.Config.DragAlong, l.Config.ShareholdersAgreement,
                    l.Config.RightOfFirstRefusal, l.Config.LiquidationPreferenceRight, l.Config.AntiDilution)).ToList(),
            Name = shareholder.Name,
            Document = shareholder.Document,
            DocumentFormatted = shareholder.DocumentFormatted,
            DocumentType = shareholder.DocumentType,
            Email = shareholder.Email,
            Phone = shareholder.Phone,
            Type = shareholder.Type,
            Status = shareholder.Status,
            Notes = shareholder.Notes,
            AddressStreet = shareholder.AddressStreet,
            AddressNumber = shareholder.AddressNumber,
            AddressComplement = shareholder.AddressComplement,
            AddressZipCode = shareholder.AddressZipCode,
            AddressCity = shareholder.AddressCity,
            AddressState = shareholder.AddressState,
            MaritalStatus = shareholder.MaritalStatus,
            Gender = shareholder.Gender,
            BirthDate = shareholder.BirthDate,
            EarnOut = shareholder.EarnOut,
            TagAlong = shareholder.TagAlong,
            DragAlong = shareholder.DragAlong,
            ShareholdersAgreement = shareholder.ShareholdersAgreement,
            RightOfFirstRefusal = shareholder.RightOfFirstRefusal,
            LiquidationPreferenceRight = shareholder.LiquidationPreferenceRight,
            AntiDilution = shareholder.AntiDilution,
            CreatedAt = shareholder.CreatedAt,
            UpdatedAt = shareholder.UpdatedAt
        };
    }
}
