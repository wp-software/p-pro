using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Companies.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.API.Middlewares;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Controller de empresas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CompaniesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CompaniesController> _logger;
    
    public CompaniesController(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CompaniesController> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// Lista todas as empresas com paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CompanyResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        
        var (companies, total) = await _unitOfWork.Companies.GetPagedAsync(page, pageSize, search, GetTenantFilter());
        
        var items = companies.Select(MapToResponse).ToList();
        var pagedResult = new PagedResult<CompanyResponse>(items, total, page, pageSize);
        
        return Ok(ApiResponse<PagedResult<CompanyResponse>>.Ok(pagedResult));
    }
    
    /// <summary>
    /// Obtém uma empresa por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cacheKey = CacheKeys.Company(id);
        
        var company = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _unitOfWork.Companies.GetByIdAsync(id),
            TimeSpan.FromMinutes(SystemConstants.CacheExpirationMinutes));
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        return Ok(ApiResponse<CompanyResponse>.Ok(MapToResponse(company)));
    }
    
    /// <summary>
    /// Cria uma nova empresa
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        // Verificar se CNPJ já existe
        if (await _unitOfWork.Companies.CnpjExistsAsync(request.Cnpj))
        {
            throw new ConflictException(ErrorMessages.CnpjAlreadyExists);
        }
        
        var company = Company.Create(
            clientId,
            request.Name,
            request.Cnpj,
            request.LegalForm,
            request.FoundationDate,
            request.TotalShares,
            request.SharePrice,
            request.TradingName,
            request.Currency);
        
        await _unitOfWork.Companies.AddAsync(company);
        
        _logger.LogInformation("Empresa criada: {CompanyId} - {Name}", company.Id, company.Name);
        
        var response = MapToResponse(company);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, 
            ApiResponse<CompanyResponse>.Ok(response, SuccessMessages.CompanyCreated));
    }
    
    /// <summary>
    /// Atualiza uma empresa
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Founder")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        company.UpdateBasicInfo(request.Name, request.TradingName, request.LogoUrl);
        company.UpdateAddress(request.Cep, request.Street, request.Number, request.Complement, request.Neighborhood, request.City, request.State);
        await _unitOfWork.Companies.UpdateAsync(company);
        
        // Invalidar cache
        await _cacheService.RemoveAsync(CacheKeys.Company(id));
        
        _logger.LogInformation("Empresa atualizada: {CompanyId}", id);
        
        return Ok(ApiResponse<CompanyResponse>.Ok(MapToResponse(company), SuccessMessages.CompanyUpdated));
    }
    
    /// <summary>
    /// Atualiza informações de ações da empresa
    /// </summary>
    [HttpPatch("{id:guid}/shares")]
    [Authorize(Roles = "SuperAdmin,Admin,Founder")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShareInfo(Guid id, [FromBody] UpdateShareInfoRequest request)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        company.UpdateShareInfo(request.TotalShares, request.SharePrice);
        await _unitOfWork.Companies.UpdateAsync(company);
        
        // Invalidar cache
        await _cacheService.RemoveAsync(CacheKeys.Company(id));
        
        _logger.LogInformation("Informações de ações atualizadas: {CompanyId}", id);
        
        return Ok(ApiResponse<CompanyResponse>.Ok(MapToResponse(company), SuccessMessages.CompanyUpdated));
    }
    
    /// <summary>
    /// Desativa uma empresa
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        company.Deactivate();
        await _unitOfWork.Companies.UpdateAsync(company);
        
        await _cacheService.RemoveAsync(CacheKeys.Company(id));
        
        _logger.LogInformation("Empresa desativada: {CompanyId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.CompanyDeactivated));
    }
    
    /// <summary>
    /// Ativa uma empresa
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        company.Activate();
        await _unitOfWork.Companies.UpdateAsync(company);
        
        await _cacheService.RemoveAsync(CacheKeys.Company(id));
        
        _logger.LogInformation("Empresa ativada: {CompanyId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.CompanyActivated));
    }
    
    /// <summary>
    /// Exclui uma empresa (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        
        if (company == null || !BelongsToTenant(company))
        {
            throw new NotFoundException("Empresa", id);
        }
        
        await _unitOfWork.Companies.SoftDeleteAsync(id);
        
        await _cacheService.RemoveAsync(CacheKeys.Company(id));
        
        _logger.LogInformation("Empresa excluída: {CompanyId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.CompanyDeleted));
    }
    
    // SuperAdmin não tem restrição de tenant; demais usuários só enxergam o próprio client
    private Guid? GetTenantFilter()
    {
        return User.IsInRole("SuperAdmin") ? null : HttpContext.GetRequiredClientId();
    }
    
    private bool BelongsToTenant(Company company)
    {
        var tenant = GetTenantFilter();
        return tenant == null || company.ClientId == tenant.Value;
    }
    
    private static CompanyResponse MapToResponse(Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            TradingName = company.TradingName,
            Cnpj = company.Cnpj,
            CnpjFormatted = company.CnpjFormatted,
            LegalForm = company.LegalForm.ToString(),
            FoundationDate = company.FoundationDate,
            TotalShares = company.TotalShares,
            SharePrice = company.SharePrice,
            Currency = company.Currency,
            Valuation = company.Valuation,
            LogoUrl = company.LogoUrl,
            Cep = company.Cep,
            Street = company.Street,
            Number = company.Number,
            Complement = company.Complement,
            Neighborhood = company.Neighborhood,
            City = company.City,
            State = company.State,
            Status = company.Status.ToString(),
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };
    }
}
