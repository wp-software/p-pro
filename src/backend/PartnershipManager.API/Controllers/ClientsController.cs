using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Companies.DTOs;
using PartnershipManager.Application.Features.Clients.DTOs;
using PartnershipManager.Application.Features.Users.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Controller de clientes (entidade raiz do multi-tenancy)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly ICoreClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientsController> _logger;
    
    public ClientsController(
        ICoreClientRepository clientRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ClientsController> logger)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("onboard")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientOnboardingResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Onboard([FromBody] OnboardClientRequest request)
    {
        if (await _clientRepository.DocumentExistsAsync(request.Client.Document))
            throw new ConflictException("Documento já cadastrado");

        if (await _clientRepository.EmailExistsAsync(request.Client.Email))
            throw new ConflictException("Email do cliente já cadastrado");

        if (await _unitOfWork.Companies.CnpjExistsAsync(request.Company.Cnpj))
            throw new ConflictException(ErrorMessages.CnpjAlreadyExists);

        if (await _unitOfWork.Users.GetByEmailAsync(request.AdministratorEmail) is not null)
            throw new ConflictException(ErrorMessages.UserAlreadyExists);

        var client = Client.Create(
            request.Client.Name,
            request.Client.Document,
            request.Client.DocumentType,
            request.Client.Email,
            request.Client.TradingName,
            request.Client.Phone);

        var company = Company.Create(
            client.Id,
            request.Company.Name,
            request.Company.Cnpj,
            request.Company.LegalForm,
            request.Company.FoundationDate,
            request.Company.TotalShares,
            request.Company.SharePrice,
            request.Company.TradingName,
            request.Company.Currency);

        var administrator = PartnershipManager.Domain.Entities.User.Create(
            client.Id,
            request.AdministratorEmail,
            request.AdministratorName,
            string.Empty,
            company.Id);

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var activationToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(activationToken))).ToLowerInvariant();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _clientRepository.AddAsync(client);
            await _unitOfWork.Companies.AddAsync(company);
            await _unitOfWork.Users.AddAsync(administrator);
            await _unitOfWork.UserRoles.AddAsync(UserRole.Create(administrator.Id, Role.Admin));
            await _unitOfWork.Users.UpdatePasswordResetTokenAsync(
                administrator.Id,
                hashedToken,
                DateTime.UtcNow.AddHours(72));
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var activationEmailSent = false;
        try
        {
            var frontendUrl = (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var activationLink = $"{frontendUrl}/activate-account?token={Uri.EscapeDataString(activationToken)}";
            await _emailService.SendAccountActivationEmailAsync(
                administrator.Email,
                administrator.Name,
                activationLink,
                administrator.Language.ToString().ToLowerInvariant());
            activationEmailSent = true;
        }
        catch (Exception emailException)
        {
            _logger.LogError(emailException,
                "Falha ao enviar convite de onboarding para {Email}", administrator.Email);
        }

        _logger.LogInformation(
            "Onboarding concluído para o cliente {ClientId}, empresa {CompanyId} e administrador {AdministratorId}",
            client.Id,
            company.Id,
            administrator.Id);

        var response = new ClientOnboardingResponse
        {
            ClientId = client.Id,
            CompanyId = company.Id,
            AdministratorId = administrator.Id,
            ActivationEmailSent = activationEmailSent
        };

        return CreatedAtAction(nameof(GetById), new { id = client.Id },
            ApiResponse<ClientOnboardingResponse>.Ok(response, "Onboarding concluído"));
    }
    
    /// <summary>
    /// Lista todos os clientes com paginação
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        
        var (clients, total) = await _clientRepository.GetPagedAsync(page, pageSize, search, status);
        
        var items = new List<ClientSummaryResponse>();
        foreach (var client in clients)
        {
            var companiesCount = await _clientRepository.GetClientCompaniesCountAsync(client.Id);
            var usersCount = await _clientRepository.GetClientUsersCountAsync(client.Id);
            
            items.Add(new ClientSummaryResponse
            {
                Id = client.Id,
                Name = client.Name,
                TradingName = client.TradingName,
                DocumentFormatted = client.DocumentFormatted,
                Email = client.Email,
                Status = client.Status.ToString(),
                TotalCompanies = companiesCount,
                TotalUsers = usersCount
            });
        }
        
        var pagedResult = new PagedResult<ClientSummaryResponse>(items, total, page, pageSize);
        
        return Ok(ApiResponse<PagedResult<ClientSummaryResponse>>.Ok(pagedResult));
    }
    
    /// <summary>
    /// Obtém um cliente por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        EnsureClientAccess(id);
        var cacheKey = $"client:{id}";
        
        var client = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _clientRepository.GetByIdAsync(id),
            TimeSpan.FromMinutes(SystemConstants.CacheExpirationMinutes));
        
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        var response = await MapToResponseAsync(client);
        
        return Ok(ApiResponse<ClientResponse>.Ok(response));
    }
    
    /// <summary>
    /// Obtém as companies de um client
    /// </summary>
    [HttpGet("{id:guid}/companies")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClientCompanyResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientCompanies(Guid id)
    {
        EnsureClientAccess(id);
        if (!await _clientRepository.ExistsAsync(id))
        {
            throw new NotFoundException("Cliente", id);
        }
        
        var companies = await _clientRepository.GetClientCompaniesAsync(id);
        
        var response = companies.Select(c => new ClientCompanyResponse
        {
            Id = c.Id,
            Name = c.Name,
            CnpjFormatted = c.CnpjFormatted,
            Valuation = c.Valuation,
            Status = c.Status.ToString()
        });
        
        return Ok(ApiResponse<IEnumerable<ClientCompanyResponse>>.Ok(response));
    }
    
    /// <summary>
    /// Lista os usuários de um cliente
    /// </summary>
    [HttpGet("{id:guid}/users")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientUsers(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (!await _clientRepository.ExistsAsync(id))
        {
            throw new NotFoundException("Cliente", id);
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, SystemConstants.MaxPageSize);

        var (users, total) = await _unitOfWork.Users.GetPagedByClientAsync(id, page, pageSize, search);

        var items = new List<UserSummaryResponse>();
        foreach (var user in users)
        {
            var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
            items.Add(new UserSummaryResponse
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                Roles = roles.ToList()
            });
        }

        return Ok(ApiResponse<PagedResult<UserSummaryResponse>>.Ok(
            new PagedResult<UserSummaryResponse>(items, total, page, pageSize)));
    }

    /// <summary>
    /// Convida um novo usuário para uma empresa do cliente
    /// </summary>
    [HttpPost("{id:guid}/users")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UserSummaryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClientUser(Guid id, [FromBody] CreateClientUserRequest request)
    {
        if (!await _clientRepository.ExistsAsync(id))
            throw new NotFoundException("Cliente", id);

        if (request.Role == Role.SuperAdmin)
            throw new ForbiddenException();

        var company = await _unitOfWork.Companies.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != id)
            throw new NotFoundException("Empresa", request.CompanyId);

        if (await _unitOfWork.Users.GetByEmailAsync(request.Email) is not null)
            throw new ConflictException(ErrorMessages.UserAlreadyExists);

        var user = PartnershipManager.Domain.Entities.User.Create(
            id, request.Email, request.Name, string.Empty, company.Id);

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var activationToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(activationToken))).ToLowerInvariant();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.UserRoles.AddAsync(UserRole.Create(user.Id, request.Role));
            await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, hashedToken, DateTime.UtcNow.AddHours(72));
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var activationEmailSent = false;
        try
        {
            var frontendUrl = (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var activationLink = $"{frontendUrl}/activate-account?token={Uri.EscapeDataString(activationToken)}";
            await _emailService.SendAccountActivationEmailAsync(
                user.Email, user.Name, activationLink, user.Language.ToString().ToLowerInvariant());
            activationEmailSent = true;
        }
        catch (Exception emailException)
        {
            _logger.LogError(emailException, "Falha ao enviar convite para {Email}", user.Email);
        }

        _logger.LogInformation("Usuário {UserId} convidado ao cliente {ClientId}", user.Id, id);

        var response = new UserSummaryResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Status = user.Status.ToString(),
            Roles = new[] { request.Role.ToString() }
        };
        return StatusCode(StatusCodes.Status201Created, ApiResponse<UserSummaryResponse>.Ok(
            response, activationEmailSent ? "Convite enviado" : "Usuário criado, mas o convite falhou"));
    }

    /// <summary>
    /// Cria um novo cliente
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        // Verificar se documento já existe
        if (await _clientRepository.DocumentExistsAsync(request.Document))
        {
            throw new ConflictException("Documento já cadastrado");
        }
        
        // Verificar se email já existe
        if (await _clientRepository.EmailExistsAsync(request.Email))
        {
            throw new ConflictException("Email já cadastrado");
        }
        
        var client = Client.Create(
            request.Name,
            request.Document,
            request.DocumentType,
            request.Email,
            request.TradingName,
            request.Phone);
        
        await _clientRepository.AddAsync(client);
        
        _logger.LogInformation("Cliente criado: {ClientId} - {Name}", client.Id, client.Name);
        
        var response = await MapToResponseAsync(client);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, 
            ApiResponse<ClientResponse>.Ok(response, "Cliente criado com sucesso"));
    }
    
    /// <summary>
    /// Atualiza um cliente
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        EnsureClientAccess(id);
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        client.UpdateBasicInfo(request.Name, request.TradingName, request.Phone, request.LogoUrl);
        
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Cliente atualizado: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Cliente atualizado com sucesso"));
    }
    
    /// <summary>
    /// Atualiza o email do cliente
    /// </summary>
    [HttpPatch("{id:guid}/email")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmail(Guid id, [FromBody] UpdateClientEmailRequest request)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        // Verificar se email já existe
        if (await _clientRepository.EmailExistsAsync(request.Email, id))
        {
            throw new ConflictException("Email já cadastrado");
        }
        
        client.UpdateEmail(request.Email);
        
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Email do cliente atualizado: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Email atualizado com sucesso"));
    }
    
    /// <summary>
    /// Atualiza as configurações do cliente
    /// </summary>
    [HttpPatch("{id:guid}/settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] UpdateClientSettingsRequest request)
    {
        EnsureClientAccess(id);
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        client.UpdateSettings(request.Settings);
        
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Configurações do cliente atualizadas: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Configurações atualizadas com sucesso"));
    }
    
    /// <summary>
    /// Ativa um cliente
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        client.Activate();
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Cliente ativado: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Cliente ativado com sucesso"));
    }
    
    /// <summary>
    /// Suspende um cliente
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(Guid id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        client.Suspend();
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Cliente suspenso: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Cliente suspenso com sucesso"));
    }
    
    /// <summary>
    /// Desativa um cliente
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new NotFoundException("Cliente", id);
        }
        
        client.Deactivate();
        await _clientRepository.UpdateAsync(client);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Cliente desativado: {ClientId}", id);
        
        var response = await MapToResponseAsync(client);
        return Ok(ApiResponse<ClientResponse>.Ok(response, "Cliente desativado com sucesso"));
    }
    
    /// <summary>
    /// Exclui um cliente (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _clientRepository.ExistsAsync(id))
        {
            throw new NotFoundException("Cliente", id);
        }
        
        await _clientRepository.SoftDeleteAsync(id);
        
        // Invalidar cache
        await _cacheService.RemoveAsync($"client:{id}");
        
        _logger.LogInformation("Cliente excluído: {ClientId}", id);
        
        return Ok(ApiResponse<object>.Ok(new { }, "Cliente excluído com sucesso"));
    }
    
    // Admin só acessa o próprio cliente; SuperAdmin acessa qualquer um
    private void EnsureClientAccess(Guid clientId)
    {
        if (User.IsInRole("SuperAdmin")) return;

        var claim = User.FindFirst("clientId") ?? User.FindFirst("ClientId") ?? User.FindFirst("client_id");
        if (!Guid.TryParse(claim?.Value, out var ownClientId) || ownClientId != clientId)
        {
            throw new ForbiddenException();
        }
    }

    // Helper Methods
    private async Task<ClientResponse> MapToResponseAsync(Client client)
    {
        var companiesCount = await _clientRepository.GetClientCompaniesCountAsync(client.Id);
        var usersCount = await _clientRepository.GetClientUsersCountAsync(client.Id);
        
        return new ClientResponse
        {
            Id = client.Id,
            Name = client.Name,
            TradingName = client.TradingName,
            Document = client.Document,
            DocumentFormatted = client.DocumentFormatted,
            DocumentType = client.DocumentType.ToString(),
            Email = client.Email,
            Phone = client.Phone,
            LogoUrl = client.LogoUrl,
            Settings = client.Settings,
            Status = client.Status.ToString(),
            TotalCompanies = companiesCount,
            TotalUsers = usersCount,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}
