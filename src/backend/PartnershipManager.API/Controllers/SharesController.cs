using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Shares.DTOs;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>
/// Gestão de ações/participações e transações
/// </summary>
[ApiController]
[Route("api/shares")]
[Authorize]
[Produces("application/json")]
public class SharesController : ControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<SharesController> _logger;

    public SharesController(IShareService shareService, ILogger<SharesController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    #region Shares

    /// <summary>
    /// Lista ações/participações com filtros e paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ShareListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllShares(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? shareholderId,
        [FromQuery] Guid? shareClassId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        var clientId = HttpContext.GetRequiredClientId();

        var result = await _shareService.GetSharesPagedAsync(
            clientId, companyId, page, pageSize, shareholderId, shareClassId, status);

        return Ok(ApiResponse<ShareListResponse>.Ok(result));
    }

    /// <summary>
    /// Obtém uma participação por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShareById(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var share = await _shareService.GetShareByIdAsync(id, clientId);
        return Ok(ApiResponse<ShareResponse>.Ok(share));
    }

    /// <summary>
    /// Lista participações de um sócio específico
    /// </summary>
    [HttpGet("by-shareholder/{shareholderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShareResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByShareholderShares(Guid shareholderId)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var shares = await _shareService.GetSharesByShareholderAsync(clientId, shareholderId);
        return Ok(ApiResponse<IEnumerable<ShareResponse>>.Ok(shares));
    }

    /// <summary>
    /// Consulta saldo de ações de um sócio em uma classe específica
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShareholderBalance(
        [FromQuery] Guid shareholderId,
        [FromQuery] Guid shareClassId)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var balance = await _shareService.GetShareholderBalanceAsync(clientId, shareholderId, shareClassId);
        return Ok(ApiResponse<decimal>.Ok(balance));
    }

    #endregion

    #region Transactions

    /// <summary>
    /// Lista transações de ações com filtros e paginação
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<TransactionListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] Guid? companyId,
        [FromQuery] string? transactionType,
        [FromQuery] Guid? shareholderId,
        [FromQuery] Guid? shareClassId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Min(pageSize, SystemConstants.MaxPageSize);
        var clientId = HttpContext.GetRequiredClientId();

        var result = await _shareService.GetTransactionsPagedAsync(
            clientId, companyId, page, pageSize, transactionType, shareholderId, shareClassId, fromDate, toDate);

        return Ok(ApiResponse<TransactionListResponse>.Ok(result));
    }

    /// <summary>
    /// Obtém uma transação por ID
    /// </summary>
    [HttpGet("transactions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShareTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var transaction = await _shareService.GetTransactionByIdAsync(id, clientId);
        return Ok(ApiResponse<ShareTransactionResponse>.Ok(transaction));
    }

    /// <summary>
    /// Lista transações de um sócio específico
    /// </summary>
    [HttpGet("transactions/by-shareholder/{shareholderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShareTransactionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsByShareholder(Guid shareholderId)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var transactions = await _shareService.GetTransactionsByShareholderAsync(clientId, shareholderId);
        return Ok(ApiResponse<IEnumerable<ShareTransactionResponse>>.Ok(transactions));
    }

    #endregion

    #region Operations

    /// <summary>
    /// Emite novas ações para um sócio
    /// </summary>
    [HttpPost("issue")]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IssueShares([FromBody] IssueSharesRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        var share = await _shareService.IssueSharesAsync(clientId, request, userId);
        _logger.LogInformation("Ações emitidas: {Quantity} para sócio {ShareholderId}", request.Quantity, request.ShareholderId);

        return CreatedAtAction(nameof(GetShareById), new { id = share.Id },
            ApiResponse<ShareResponse>.Ok(share, "Ações emitidas com sucesso"));
    }

    /// <summary>
    /// Emite ações para vários sócios em uma única operação
    /// </summary>
    [HttpPost("issue-batch")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShareResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IssueSharesBatch([FromBody] IssueSharesBatchRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        var shares = await _shareService.IssueSharesBatchAsync(clientId, request, userId);
        _logger.LogInformation(
            "Emissão em lote de {Quantity} ações para {ShareholderCount} sócios",
            request.TotalQuantity,
            shares.Count);

        return Ok(ApiResponse<IReadOnlyList<ShareResponse>>.Ok(shares, "Emissão em lote realizada com sucesso"));
    }

    /// <summary>
    /// Transfere ações entre sócios
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferShares([FromBody] TransferSharesRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        var share = await _shareService.TransferSharesAsync(clientId, request, userId);
        _logger.LogInformation("Ações transferidas: {Quantity} de {From} para {To}", 
            request.Quantity, request.FromShareholderId, request.ToShareholderId);

        return CreatedAtAction(nameof(GetShareById), new { id = share.Id },
            ApiResponse<ShareResponse>.Ok(share, "Ações transferidas com sucesso"));
    }

    /// <summary>
    /// Cancela ações de um sócio
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelShares([FromBody] CancelSharesRequest request)
    {
        var clientId = HttpContext.GetRequiredClientId();
        var userId = GetUserId();

        await _shareService.CancelSharesAsync(clientId, request, userId);
        _logger.LogInformation("Ações canceladas: {Quantity} do sócio {ShareholderId}", 
            request.Quantity, request.ShareholderId);

        return NoContent();
    }

    #endregion

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("userId")?.Value
                       ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
