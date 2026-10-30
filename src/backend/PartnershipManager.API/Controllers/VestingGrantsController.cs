using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Vesting.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Gerenciamento de grants (concessões) de vesting
/// </summary>
[ApiController]
[Route("api/vesting-grants")]
[Authorize]
[Produces("application/json")]
public class VestingGrantsController : ControllerBase
{
    private static readonly string[] RestrictedRoles =
        { nameof(Role.Employee), nameof(Role.Investor), nameof(Role.Viewer) };

    private readonly IVestingGrantService _vestingGrantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShareholderRepository _shareholderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VestingGrantsController> _logger;

    public VestingGrantsController(
        IVestingGrantService vestingGrantService,
        IUnitOfWork unitOfWork,
        IShareholderRepository shareholderRepository,
        ICurrentUserService currentUserService,
        ILogger<VestingGrantsController> logger)
    {
        _vestingGrantService = vestingGrantService;
        _unitOfWork = unitOfWork;
        _shareholderRepository = shareholderRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Lista os grants do sócio vinculado ao usuário logado
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VestingGrantResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] Guid? companyId)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var shareholderId = await GetOwnShareholderIdAsync(companyId);
        if (shareholderId == null)
        {
            return Ok(ApiResponse<IEnumerable<VestingGrantResponse>>.Ok(Array.Empty<VestingGrantResponse>()));
        }

        var result = await _vestingGrantService.GetByShareholderAsync(clientId, shareholderId.Value, companyId);
        return Ok(ApiResponse<IEnumerable<VestingGrantResponse>>.Ok(result));
    }

    /// <summary>
    /// Lista grants de vesting com paginação e filtros
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vestingPlanId,
        [FromQuery] Guid? shareholderId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        var clientId = HttpContext.GetRequiredClientId();

        if (IsRestrictedUser())
        {
            shareholderId = await GetOwnShareholderIdAsync(companyId);
            if (shareholderId == null)
            {
                return Ok(ApiResponse<VestingGrantListResponse>.Ok(
                    new VestingGrantListResponse(Array.Empty<VestingGrantResponse>(), 0, page, pageSize)));
            }
        }

        var result = await _vestingGrantService.GetPagedAsync(
            clientId, companyId, page, pageSize, vestingPlanId, shareholderId, status);
        return Ok(ApiResponse<VestingGrantListResponse>.Ok(result));
    }

    /// <summary>
    /// Lista grants de um acionista específico
    /// </summary>
    [HttpGet("by-shareholder/{shareholderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VestingGrantResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByShareholder(Guid shareholderId, [FromQuery] Guid? companyId)
    {
        var clientId = HttpContext.GetRequiredClientId();
        if (IsRestrictedUser() && await GetOwnShareholderIdAsync(companyId) != shareholderId)
        {
            throw new ForbiddenException();
        }

        var result = await _vestingGrantService.GetByShareholderAsync(clientId, shareholderId, companyId);
        return Ok(ApiResponse<IEnumerable<VestingGrantResponse>>.Ok(result));
    }

    /// <summary>
    /// Obtém grant de vesting por id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var result = await _vestingGrantService.GetByIdAsync(id, clientId);
        await EnsureCanAccessGrantAsync(result);
        return Ok(ApiResponse<VestingGrantResponse>.Ok(result));
    }

    /// <summary>
    /// Cria novo grant de vesting
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVestingGrantRequest request)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();
        var result = await _vestingGrantService.CreateAsync(clientId, request, userId);
        _logger.LogInformation("Grant de vesting criado: {VestingGrantId}", result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<VestingGrantResponse>.Ok(result, "Grant de vesting criado com sucesso"));
    }

    /// <summary>
    /// Aprova um grant pendente
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId() ?? Guid.Empty;
        var result = await _vestingGrantService.ApproveAsync(id, clientId, userId);
        _logger.LogInformation("Grant aprovado: {VestingGrantId}", id);
        return Ok(ApiResponse<VestingGrantResponse>.Ok(result, "Grant aprovado com sucesso"));
    }

    /// <summary>
    /// Ativa um grant aprovado
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId() ?? Guid.Empty;
        var result = await _vestingGrantService.ActivateAsync(id, clientId, userId);
        return Ok(ApiResponse<VestingGrantResponse>.Ok(result, "Grant ativado com sucesso"));
    }

    /// <summary>
    /// Cancela um grant
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId() ?? Guid.Empty;
        var result = await _vestingGrantService.CancelAsync(id, clientId, userId);
        return Ok(ApiResponse<VestingGrantResponse>.Ok(result, "Grant cancelado"));
    }

    /// <summary>
    /// Recalcula as ações vestidas com base na data atual
    /// </summary>
    [HttpPatch("{id:guid}/recalculate")]
    [ProducesResponseType(typeof(ApiResponse<VestingGrantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recalculate(Guid id)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId() ?? Guid.Empty;
        var result = await _vestingGrantService.RecalculateVestingAsync(id, clientId, userId);
        return Ok(ApiResponse<VestingGrantResponse>.Ok(result, "Vesting recalculado"));
    }

    /// <summary>
    /// Exercita ações vestidas (converte em participação real)
    /// </summary>
    [HttpPost("{id:guid}/exercise")]
    [ProducesResponseType(typeof(ApiResponse<VestingTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exercise(Guid id, [FromBody] ExerciseSharesRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        await EnsureCanAccessGrantAsync(await _vestingGrantService.GetByIdAsync(id, clientId));
        var userId = GetUserId() ?? Guid.Empty;
        var result = await _vestingGrantService.ExerciseSharesAsync(id, clientId, request, userId);
        _logger.LogInformation("Ações exercitadas — Grant: {VestingGrantId}, Quantidade: {Shares}",
            id, request.SharesToExercise);
        return Ok(ApiResponse<VestingTransactionResponse>.Ok(result, "Ações exercitadas com sucesso"));
    }

    /// <summary>
    /// Calcula o status de vesting em uma data específica
    /// </summary>
    [HttpGet("{id:guid}/calculate")]
    [ProducesResponseType(typeof(ApiResponse<VestingCalculationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate(Guid id, [FromQuery] DateTime? asOfDate)
    {
        var clientId = HttpContext.GetRequiredClientId();
        await EnsureCanAccessGrantAsync(await _vestingGrantService.GetByIdAsync(id, clientId));
        var result = await _vestingGrantService.CalculateAsOfAsync(id, clientId, asOfDate);
        return Ok(ApiResponse<VestingCalculationResult>.Ok(result));
    }

    /// <summary>
    /// Retorna projeção de vesting para X meses à frente
    /// </summary>
    [HttpGet("{id:guid}/projection")]
    [ProducesResponseType(typeof(ApiResponse<VestingProjectionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Projection(Guid id, [FromQuery] int monthsAhead = 24)
    {
        var clientId = HttpContext.GetRequiredClientId();
        await EnsureCanAccessGrantAsync(await _vestingGrantService.GetByIdAsync(id, clientId));
        var result = await _vestingGrantService.GetProjectionAsync(id, clientId, monthsAhead);
        return Ok(ApiResponse<VestingProjectionResponse>.Ok(result));
    }

    /// <summary>
    /// Lista transações de exercício de um grant
    /// </summary>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VestingTransactionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        await EnsureCanAccessGrantAsync(await _vestingGrantService.GetByIdAsync(id, clientId));
        var result = await _vestingGrantService.GetTransactionsAsync(id, clientId);
        return Ok(ApiResponse<IEnumerable<VestingTransactionResponse>>.Ok(result));
    }

    /// <summary>
    /// Remove (soft delete) um grant de vesting
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        EnsureNotRestrictedUser();
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();
        await _vestingGrantService.DeleteAsync(id, clientId, userId);
        _logger.LogInformation("Grant de vesting removido: {VestingGrantId}", id);
        return Ok(ApiResponse.Ok("Grant removido com sucesso"));
    }

    // Usuários sem nenhum papel de gestão só enxergam os dados do próprio sócio
    private bool IsRestrictedUser()
        => !_currentUserService.Roles.Any(r => !RestrictedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

    private void EnsureNotRestrictedUser()
    {
        if (IsRestrictedUser())
        {
            throw new ForbiddenException();
        }
    }

    private async Task EnsureCanAccessGrantAsync(VestingGrantResponse grant)
    {
        if (!IsRestrictedUser())
        {
            return;
        }

        var own = await GetOwnShareholderIdAsync(grant.CompanyId);
        if (own == null || own != grant.ShareholderId)
        {
            throw new ForbiddenException();
        }
    }

    // Sócio vinculado ao usuário logado; null se não houver vínculo ou se ele não pertencer à empresa
    private async Task<Guid?> GetOwnShareholderIdAsync(Guid? companyId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return null;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user?.ShareholderId == null)
        {
            return null;
        }

        if (companyId.HasValue &&
            !await _shareholderRepository.IsLinkedToCompanyAsync(user.ShareholderId.Value, companyId.Value))
        {
            return null;
        }

        return user.ShareholderId;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
