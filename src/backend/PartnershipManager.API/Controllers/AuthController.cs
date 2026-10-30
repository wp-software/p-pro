using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Auth.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Controller de autenticação
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// Realiza login no sistema
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, request.CompanyId);
        
        if (user == null)
        {
            _logger.LogWarning("Tentativa de login com email não encontrado: {Email}", request.Email);
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }
        
        if (user.IsLockedOut)
        {
            var minutes = user.GetLockoutRemainingMinutes();
            throw new UnauthorizedException(string.Format(ErrorMessages.UserBlocked, minutes));
        }
        
        if (user.Status == UserStatus.Inactive)
        {
            throw new UnauthorizedException(ErrorMessages.UserInactive);
        }
        
        if (user.Status == UserStatus.Pending)
        {
            throw new UnauthorizedException(ErrorMessages.UserPending);
        }
        
        if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await _unitOfWork.Users.UpdateLoginInfoAsync(user.Id, false);
            _logger.LogWarning("Senha inválida para usuário: {Email}", request.Email);
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }
        
        // Login bem-sucedido
        await _unitOfWork.Users.UpdateLoginInfoAsync(user.Id, true);
        
        var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
        var companyId = user.CompanyId;
        var company = companyId.HasValue ? await _unitOfWork.Companies.GetByIdAsync(companyId.Value) : null;
        
        var accessToken = _authService.GenerateJwtToken(user, roles);
        var refreshToken = _authService.GenerateRefreshToken();
        
        await _unitOfWork.Users.UpdateRefreshTokenAsync(
            user.Id, 
            HashRefreshToken(refreshToken),
            DateTime.UtcNow.AddMinutes(SystemConstants.SessionIdleTimeoutMinutes));
        
        _logger.LogInformation("Login realizado com sucesso para: {Email}", request.Email);
        
        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            User = new UserInfo
            {
                Id = user.Id,
                ClientId = user.ClientId,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                CompanyId = companyId ?? Guid.Empty,
                CompanyName = company?.Name ?? string.Empty,
                ShareholderId = user.ShareholderId,
                Roles = roles.ToList(),
                Language = user.Language.ToString().ToLower()
            }
        };
        
        return Ok(ApiResponse<AuthResponse>.Ok(response, SuccessMessages.LoginSuccess));
    }
    
    /// <summary>
    /// Realiza logout do sistema
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (Guid.TryParse(userId, out var id))
        {
            await _unitOfWork.Users.UpdateRefreshTokenAsync(id, null, null);
            _logger.LogInformation("Logout realizado para usuário: {UserId}", id);
        }
        
        return Ok(ApiResponse.Ok(SuccessMessages.LogoutSuccess));
    }
    
    /// <summary>
    /// Renova o token de acesso usando refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }
        
        // Buscar usuário pelo refresh token
        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(request.RefreshToken);
        
        if (user == null ||
            user.Status != UserStatus.Active ||
            user.IsLockedOut)
        {
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }
        
        // Gerar novos tokens
        var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
        var newAccessToken = _authService.GenerateJwtToken(user, roles);
        var newRefreshToken = _authService.GenerateRefreshToken();
        
        // Atualizar refresh token no banco
        await _unitOfWork.Users.UpdateRefreshTokenAsync(
            user.Id, 
            HashRefreshToken(newRefreshToken),
            DateTime.UtcNow.AddMinutes(SystemConstants.SessionIdleTimeoutMinutes));
        
        var companyId = user.CompanyId;
        var company = companyId.HasValue ? await _unitOfWork.Companies.GetByIdAsync(companyId.Value) : null;
        
        var response = new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            User = new UserInfo
            {
                Id = user.Id,
                ClientId = user.ClientId,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                CompanyId = companyId ?? Guid.Empty,
                CompanyName = company?.Name ?? string.Empty,
                ShareholderId = user.ShareholderId,
                Roles = roles.ToList(),
                Language = user.Language.ToString().ToLower()
            }
        };
        
        _logger.LogInformation("Token renovado para usuário: {UserId}", user.Id);
        
        return Ok(ApiResponse<AuthResponse>.Ok(response));
    }
    
    /// <summary>
    /// Retorna informações do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedException();
        }
        
        
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException("Usuário", id);
        }
        
        var roles = await _unitOfWork.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
        var companyId = user.CompanyId;
        var company = companyId.HasValue ? await _unitOfWork.Companies.GetByIdAsync(companyId.Value) : null;
        
        var userInfo = new UserInfo
        {
            Id = user.Id,
            ClientId = user.ClientId,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            CompanyId = companyId ?? Guid.Empty,
            CompanyName = company?.Name ?? string.Empty,
            ShareholderId = user.ShareholderId,
            Roles = roles.ToList(),
            Language = user.Language.ToString().ToLower()
        };
        
        return Ok(ApiResponse<UserInfo>.Ok(userInfo));
    }
    
    /// <summary>
    /// Altera a senha do usuário autenticado
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedException();
        }
        
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException("Usuário", id);
        }
        
        if (!_authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainException(ErrorMessages.CurrentPasswordInvalid);
        }
        
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new ValidationException("ConfirmNewPassword", ErrorMessages.PasswordMismatch);
        }
        
        user.ChangePassword(_authService.HashPassword(request.NewPassword));
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.Users.UpdateRefreshTokenAsync(id, null, null);
        
        _logger.LogInformation("Senha alterada para usuário: {UserId}", id);
        
        return Ok(ApiResponse.Ok(SuccessMessages.PasswordChanged));
    }
    
    /// <summary>
    /// Solicita recuperação de senha via e-mail.
    /// Sempre retorna 200 OK para evitar enumeração de usuários (OWASP A01).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

            if (user != null && user.Status == UserStatus.Active)
            {
                // Gerar token seguro (base64url de 32 bytes aleatórios)
                var tokenBytes = RandomNumberGenerator.GetBytes(32);
                var plainToken = Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");

                // Armazenar apenas o hash SHA-256 no banco
                var hashedToken = HashToken(plainToken);
                var expiry = DateTime.UtcNow.AddHours(1);

                await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, hashedToken, expiry);

                var frontendUrl = (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
                var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(plainToken)}";

                await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    user.Name,
                    resetLink,
                    user.Language.ToString().ToLower());

                _logger.LogInformation("Link de recuperação de senha gerado para {Email}", user.Email);
            }
            else
            {
                _logger.LogInformation("Recuperação de senha solicitada para e-mail não encontrado/inativo: {Email}", request.Email);
            }
        }
        catch (Exception ex)
        {
            // Nunca expor erros internos — retornar 200 mesmo assim
            _logger.LogError(ex, "Erro ao processar forgot-password para {Email}", request.Email);
        }

        return Ok(ApiResponse.Ok("Se o e-mail existir em nossa base, você receberá as instruções em instantes."));
    }

    /// <summary>
    /// Redefine a senha usando o token recebido por e-mail.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ValidationException("Token", "Token de recuperação é obrigatório.");

        if (request.NewPassword != request.ConfirmNewPassword)
            throw new ValidationException("ConfirmNewPassword", ErrorMessages.PasswordMismatch);

        var hashedToken = HashToken(request.Token);
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(hashedToken);

        if (user == null || !user.IsPasswordResetTokenValid())
        {
            _logger.LogWarning("Tentativa de reset com token inválido ou expirado");
            throw new ValidationException("Token", "O link de recuperação é inválido ou já expirou.");
        }

        var newHash = _authService.HashPassword(request.NewPassword);
        await _unitOfWork.Users.UpdatePasswordHashAsync(user.Id, newHash);
        await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, null, null);
        await _unitOfWork.Users.UpdateRefreshTokenAsync(user.Id, null, null);

        _logger.LogInformation("Senha redefinida com sucesso para o usuário {UserId}", user.Id);

        return Ok(ApiResponse.Ok("Senha redefinida com sucesso. Você já pode fazer login com a nova senha."));
    }

    /// <summary>
    /// Ativa a conta de um novo usuário definindo sua senha pela primeira vez.
    /// </summary>
    [HttpPost("activate-account")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateAccount([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ValidationException("Token", "Token de ativação é obrigatório.");

        if (request.NewPassword != request.ConfirmNewPassword)
            throw new ValidationException("ConfirmNewPassword", ErrorMessages.PasswordMismatch);

        var hashedToken = HashToken(request.Token);
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(hashedToken);

        if (user == null || !user.IsPasswordResetTokenValid())
        {
            _logger.LogWarning("Tentativa de ativação com token inválido ou expirado");
            throw new ValidationException("Token", "O link de ativação é inválido ou já expirou.");
        }

        var newHash = _authService.HashPassword(request.NewPassword);

        // Atualizar hash no objeto em memória antes de persistir via UpdateAsync
        // (evita que UpdateAsync sobrescreva o hash com o valor vazio anterior)
        user.ChangePassword(newHash);

        // Ativar o usuário caso ainda esteja Pending
        if (user.Status == UserStatus.Pending)
        {
            user.Activate();
        }

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.Users.UpdatePasswordResetTokenAsync(user.Id, null, null);

        _logger.LogInformation("Conta ativada com sucesso para o usuário {UserId}", user.Id);

        return Ok(ApiResponse.Ok("Conta ativada com sucesso. Você já pode fazer login."));
    }

    /// <summary>
    /// Renova o prazo da sessão após atividade explícita do usuário.
    /// </summary>
    [HttpPost("activity")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordActivity()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedException();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null ||
            user.Status != UserStatus.Active ||
            user.IsLockedOut ||
            string.IsNullOrWhiteSpace(user.RefreshToken) ||
            user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }

        await _unitOfWork.Users.UpdateRefreshTokenAsync(
            id,
            user.RefreshToken,
            DateTime.UtcNow.AddMinutes(SystemConstants.SessionIdleTimeoutMinutes));

        return Ok(ApiResponse.Ok());
    }

    // Gera o hash SHA-256 do token (base64 hex)
    private static string HashToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashRefreshToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        return int.Parse(
            _configuration["Jwt:AccessTokenMinutes"] ??
            SystemConstants.SessionIdleTimeoutMinutes.ToString());
    }
}
