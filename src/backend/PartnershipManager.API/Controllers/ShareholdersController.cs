using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Shareholders.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// CRUD de sócios/acionistas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ShareholdersController : ControllerBase
{
    private readonly IShareholderService _shareholderService;
    private readonly ILogger<ShareholdersController> _logger;

    public ShareholdersController(IShareholderService shareholderService, ILogger<ShareholdersController> logger)
    {
        _shareholderService = shareholderService;
        _logger = logger;
    }

    /// <summary>
    /// Lista shareholders com filtros
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ShareholderListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortDirection = null)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);

        var clientId = HttpContext.GetRequiredClientId();

        var result = await _shareholderService.GetPagedAsync(clientId, companyId, page, pageSize, search, type, status, sortDirection);

        return Ok(ApiResponse<ShareholderListResponse>.Ok(result));
    }

    /// <summary>
    /// Obtém shareholder por id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShareholderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var shareholder = await _shareholderService.GetByIdAsync(id, clientId);
        return Ok(ApiResponse<ShareholderResponse>.Ok(shareholder));
    }

    /// <summary>
    /// Cria novo shareholder
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShareholderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShareholderRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        var shareholder = await _shareholderService.CreateAsync(clientId, request, userId);
        _logger.LogInformation("Shareholder criado: {ShareholderId}", shareholder.Id);

        return CreatedAtAction(nameof(GetById), new { id = shareholder.Id }, ApiResponse<ShareholderResponse>.Ok(shareholder, "Shareholder criado com sucesso"));
    }

    /// <summary>
    /// Atualiza um shareholder
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShareholderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShareholderRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        var shareholder = await _shareholderService.UpdateAsync(id, clientId, request, userId);
        _logger.LogInformation("Shareholder atualizado: {ShareholderId}", id);

        return Ok(ApiResponse<ShareholderResponse>.Ok(shareholder, "Shareholder atualizado com sucesso"));
    }

    /// <summary>
    /// Remove (soft delete) um shareholder
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        await _shareholderService.DeleteAsync(id, clientId, userId);
        _logger.LogInformation("Shareholder removido: {ShareholderId}", id);

        return Ok(ApiResponse.Ok("Shareholder removido"));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
