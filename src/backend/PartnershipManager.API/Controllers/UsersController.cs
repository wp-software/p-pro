using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Users.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Controller de usuários
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShareholderRepository _shareholderRepository;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(
        IUnitOfWork unitOfWork,
        IShareholderRepository shareholderRepository,
        IAuthService authService,
        ICacheService cacheService,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _shareholderRepository = shareholderRepository;
        _authService = authService;
        _cacheService = cacheService;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// Lista usuários da empresa com paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var companyId = _currentUserService.CompanyId 
            ?? throw new UnauthorizedException();
        
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        
        var (users, total) = await _unitOfWork.Users.GetPagedByCompanyAsync(
            companyId, page, pageSize, search);
        
        var items = new List<UserSummaryResponse>();
        foreach (var user in users)
        {
            var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
            items.Add(MapToSummary(user, roles));
        }
        
        var pagedResult = new PagedResult<UserSummaryResponse>(items, total, page, pageSize);
        
        return Ok(ApiResponse<PagedResult<UserSummaryResponse>>.Ok(pagedResult));
    }
    
    /// <summary>
    /// Obtém um usuário por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
        
        return Ok(ApiResponse<UserResponse>.Ok(MapToResponse(user, roles)));
    }
    
    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var companyId = _currentUserService.CompanyId 
            ?? throw new UnauthorizedException();
        
        var clientId = _currentUserService.ClientId
            ?? throw new UnauthorizedException();

        EnsureCanAssignRole(request.InitialRole);
        
        // Verificar se email já existe
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, companyId))
        {
            throw new ConflictException(ErrorMessages.UserAlreadyExists);
        }
        
        // Usuário é criado sem senha — ela será definida pelo próprio usuário via link de ativação
        var user = Domain.Entities.User.Create(
            clientId,
            request.Email,
            request.Name,
            passwordHash: string.Empty,
            companyId);

        if (request.ShareholderId.HasValue)
        {
            await EnsureShareholderLinkableAsync(user, request.ShareholderId.Value);
            user.LinkShareholder(request.ShareholderId);
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            await _unitOfWork.Users.AddAsync(user);
            
            // Adicionar papel inicial
            var userRole = UserRole.Create(user.Id, request.InitialRole, _currentUserService.UserId);
            await _unitOfWork.UserRoles.AddAsync(userRole);
            
            // Gerar token de ativação seguro (base64url de 32 bytes aleatórios, hash SHA-256 no banco)
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var plainToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
            var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();
            var expiry = DateTime.UtcNow.AddHours(72);
            
            await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, hashedToken, expiry);
            
            await _unitOfWork.CommitTransactionAsync();
            
            // Enviar e-mail de ativação (fora da transação — falha no e-mail não reverte o usuário)
            try
            {
                var frontendUrl = (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
                var activationLink = $"{frontendUrl}/activate-account?token={Uri.EscapeDataString(plainToken)}";
                await _emailService.SendAccountActivationEmailAsync(
                    user.Email,
                    user.Name,
                    activationLink,
                    user.Language.ToString().ToLower());
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Falha ao enviar e-mail de ativação para {Email} — usuário criado mesmo assim", user.Email);
            }
            
            _logger.LogInformation("Usuário criado: {UserId} - {Email}", user.Id, user.Email);
            
            var roles = new[] { request.InitialRole.ToString() };
            var response = MapToResponse(user, roles);
            
            return CreatedAtAction(nameof(GetById), new { id = user.Id },
                ApiResponse<UserResponse>.Ok(response, SuccessMessages.UserCreated));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    /// <summary>
    /// Atualiza um usuário
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        // Verificar permissão (próprio usuário ou admin)
        if (user.Id != _currentUserService.UserId && 
            !_currentUserService.Roles.Any(r => r == "Admin" || r == "SuperAdmin" || r == "HR"))
        {
            throw new ForbiddenException();
        }
        
        user.UpdateProfile(request.Name, request.Phone, request.AvatarUrl);

        if (CanManageShareholderLink())
        {
            if (request.ShareholderId.HasValue)
            {
                await EnsureShareholderLinkableAsync(user, request.ShareholderId.Value);
            }

            user.LinkShareholder(request.ShareholderId);
        }

        await _unitOfWork.Users.UpdateAsync(user);
        
        await _cacheService.RemoveAsync(CacheKeys.User(id));
        
        _logger.LogInformation("Usuário atualizado: {UserId}", id);
        
        var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
        
        return Ok(ApiResponse<UserResponse>.Ok(MapToResponse(user, roles), SuccessMessages.UserUpdated));
    }
    
    /// <summary>
    /// Atualiza preferências do usuário
    /// </summary>
    [HttpPatch("{id:guid}/preferences")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdateUserPreferencesRequest request)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        // Apenas o próprio usuário pode alterar preferências
        if (user.Id != _currentUserService.UserId)
        {
            throw new ForbiddenException();
        }
        
        user.UpdatePreferences(request.Language, request.Timezone, null);
        await _unitOfWork.Users.UpdateAsync(user);
        
        await _cacheService.RemoveAsync(CacheKeys.User(id));
        
        return Ok(ApiResponse.Ok(SuccessMessages.UserUpdated));
    }
    
    /// <summary>
    /// Adiciona papel ao usuário
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddRole(Guid id, [FromBody] ManageUserRoleRequest request)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        EnsureCanManageRolesForUser(user);
        EnsureCanAssignRole(request.Role);
        
        // Verificar se já tem o papel
        if (await _unitOfWork.UserRoles.ExistsAsync(id, request.Role.ToString()))
        {
            throw new ConflictException(ErrorMessages.UserAlreadyHasRole);
        }
        
        var userRole = UserRole.Create(id, request.Role, _currentUserService.UserId);
        if (request.ExpiresAt.HasValue)
        {
            // Definir expiração no UserRole - seria necessário adicionar método
        }
        
        await _unitOfWork.UserRoles.AddAsync(userRole);
        
        await _cacheService.RemoveAsync(CacheKeys.UserRoles(id));
        
        _logger.LogInformation("Papel {Role} adicionado ao usuário {UserId}", request.Role, id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.RoleAdded));
    }
    
    /// <summary>
    /// Remove papel do usuário
    /// </summary>
    [HttpDelete("{id:guid}/roles/{role}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        EnsureCanManageRolesForUser(user);

        if (!Enum.TryParse<Role>(role, true, out var parsedRole) || !Enum.IsDefined(parsedRole))
        {
            throw new ValidationException("Role", "Papel inválido.");
        }

        EnsureCanAssignRole(parsedRole);
        
        // Verificar se tem o papel
        if (!await _unitOfWork.UserRoles.ExistsAsync(id, role))
        {
            throw new NotFoundException("Papel", role);
        }
        
        // Não permitir remover o último admin
        if (role == "Admin" && user.CompanyId.HasValue)
        {
            var admins = await _unitOfWork.Users.GetActiveUsersByCompanyAsync(user.CompanyId.Value);
            var adminCount = 0;
            foreach (var admin in admins)
            {
                if (await _unitOfWork.UserRoles.ExistsAsync(admin.Id, "Admin"))
                    adminCount++;
            }
            
            if (adminCount <= 1)
            {
                throw new BusinessRuleException("LastAdmin", ErrorMessages.CannotRemoveLastAdmin);
            }
        }
        
        await _unitOfWork.UserRoles.DeactivateAsync(id, role);
        
        await _cacheService.RemoveAsync(CacheKeys.UserRoles(id));
        
        _logger.LogInformation("Papel {Role} removido do usuário {UserId}", role, id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.RoleRemoved));
    }
    
    /// <summary>
    /// Reenvia o e-mail de ativação para um usuário pendente
    /// </summary>
    [HttpPost("{id:guid}/resend-activation")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendActivation(Guid id)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        if (user.Status != Domain.Enums.UserStatus.Pending)
        {
            throw new ValidationException("Status", "Usuário não está pendente de ativação.");
        }
        
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();
        var expiry = DateTime.UtcNow.AddHours(72);
        
        await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, hashedToken, expiry);
        
        var frontendUrl = (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var activationLink = $"{frontendUrl}/activate-account?token={Uri.EscapeDataString(plainToken)}";
        
        await _emailService.SendAccountActivationEmailAsync(
            user.Email,
            user.Name,
            activationLink,
            user.Language.ToString().ToLower());
        
        _logger.LogInformation("E-mail de ativação reenviado para: {UserId} - {Email}", user.Id, user.Email);
        
        return Ok(ApiResponse.Ok("E-mail de ativação reenviado com sucesso."));
    }
    
    /// <summary>
    /// Ativa um usuário
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        user.Activate();
        await _unitOfWork.Users.UpdateAsync(user);
        
        await _cacheService.RemoveAsync(CacheKeys.User(id));
        
        _logger.LogInformation("Usuário ativado: {UserId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.UserActivated));
    }
    
    /// <summary>
    /// Desativa um usuário
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var user = await GetUserInCurrentTenantAsync(id);
        
        // Não pode desativar a si mesmo
        if (user.Id == _currentUserService.UserId)
        {
            throw new BusinessRuleException("SelfDeactivation", ErrorMessages.CannotDeactivateYourself);
        }
        
        user.Deactivate();
        await _unitOfWork.Users.UpdateAsync(user);
        
        await _cacheService.RemoveAsync(CacheKeys.User(id));
        
        _logger.LogInformation("Usuário desativado: {UserId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.UserDeactivated));
    }
    
    /// <summary>
    /// Exclui um usuário (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await GetUserInCurrentTenantAsync(id);
        
        // Não pode excluir a si mesmo
        if (id == _currentUserService.UserId)
        {
            throw new BusinessRuleException("SelfDeletion", "Não é possível excluir sua própria conta.");
        }
        
        await _unitOfWork.Users.SoftDeleteAsync(id, _currentUserService.UserId);
        
        await _cacheService.RemoveAsync(CacheKeys.User(id));
        
        _logger.LogInformation("Usuário excluído: {UserId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.UserDeleted));
    }

    private async Task<User> GetUserInCurrentTenantAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);

        if (user == null)
        {
            throw new NotFoundException("Usuário", id);
        }

        if (_currentUserService.Roles.Contains(Role.SuperAdmin.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            return user;
        }

        var clientId = _currentUserService.ClientId ?? throw new UnauthorizedException();
        var companyId = _currentUserService.CompanyId ?? throw new UnauthorizedException();

        if (user.ClientId != clientId || user.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        return user;
    }

    private bool CanManageShareholderLink()
        => _currentUserService.Roles.Any(r =>
            r.Equals(Role.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase) ||
            r.Equals(Role.Admin.ToString(), StringComparison.OrdinalIgnoreCase) ||
            r.Equals(Role.HR.ToString(), StringComparison.OrdinalIgnoreCase));

    private async Task EnsureShareholderLinkableAsync(User user, Guid shareholderId)
    {
        var shareholder = await _shareholderRepository.GetByIdAsync(shareholderId, user.ClientId)
            ?? throw new NotFoundException("Sócio", shareholderId);

        if (user.CompanyId.HasValue &&
            !await _shareholderRepository.IsLinkedToCompanyAsync(shareholder.Id, user.CompanyId.Value))
        {
            throw new ValidationException("ShareholderId", "O sócio não está vinculado à empresa do usuário.");
        }

        var linkedUser = await _unitOfWork.Users.GetByShareholderIdAsync(shareholder.Id);
        if (linkedUser != null && linkedUser.Id != user.Id)
        {
            throw new ConflictException("Este sócio já está vinculado a outro usuário.");
        }
    }

    private void EnsureCanAssignRole(Role role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ValidationException("Role", "Papel inválido.");
        }

        var roles = _currentUserService.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roles.Contains(Role.SuperAdmin.ToString()))
        {
            return;
        }

        if (role == Role.SuperAdmin)
        {
            throw new ForbiddenException();
        }

        if (roles.Contains(Role.HR.ToString()) &&
            !roles.Contains(Role.Admin.ToString()) &&
            role is not (Role.Employee or Role.Investor or Role.Viewer))
        {
            throw new ForbiddenException();
        }
    }

    private void EnsureCanManageRolesForUser(User user)
    {
        if (user.Id == _currentUserService.UserId &&
            !_currentUserService.Roles.Contains(Role.SuperAdmin.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException();
        }
    }
    
    private static UserResponse MapToResponse(User user, IEnumerable<string> roles)
    {
        return new UserResponse
        {
            Id = user.Id,
            CompanyId = user.CompanyId ?? Guid.Empty,
            ShareholderId = user.ShareholderId,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Phone = user.Phone,
            Status = user.Status.ToString(),
            Language = user.Language.ToString(),
            Timezone = user.Timezone,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
    
    private static UserSummaryResponse MapToSummary(User user, IEnumerable<string> roles)
    {
        return new UserSummaryResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString(),
            ShareholderId = user.ShareholderId,
            Roles = roles.ToList()
        };
    }
}
