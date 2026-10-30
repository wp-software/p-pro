using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.Workflow;
using PartnershipManager.Application.Services;

namespace PartnershipManager.API.Controllers;

[Route("api/workflows")]
[Authorize]
[Produces("application/json")]
public class WorkflowsController : BaseApiController
{
    private readonly IWorkflowService _service;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(IWorkflowService service, ILogger<WorkflowsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? workflowType = null)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var result = await _service.GetByCompanyAsync(companyId, page, pageSize, status, workflowType);
            return Ok(ApiResponse<PagedResult<WorkflowResponse>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar workflows");
            return StatusCode(500, new { message = "Erro ao buscar workflows" });
        }
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetRequiredUserId();
            var result = await _service.GetPendingByUserAsync(userId, companyId);
            return Ok(ApiResponse<IEnumerable<WorkflowResponse>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar workflows pendentes");
            return StatusCode(500, new { message = "Erro ao buscar workflows pendentes" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var result = await _service.GetByIdAsync(id, companyId);
            if (result == null) return NotFound(new { message = "Workflow não encontrado" });
            return Ok(ApiResponse<WorkflowResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar workflow {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar workflow" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest request)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetRequiredUserId();
            var id = await _service.CreateAsync(companyId, request, userId);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar workflow");
            return StatusCode(500, new { message = "Erro ao criar workflow" });
        }
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, Guid stepId, [FromBody] WorkflowDecisionRequest request)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetRequiredUserId();
            await _service.ApproveStepAsync(id, stepId, companyId, userId, request.Comments);
            return Ok(ApiResponse.Ok("Etapa aprovada com sucesso"));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar etapa {StepId} do workflow {Id}", stepId, id);
            return StatusCode(500, new { message = "Erro ao aprovar etapa" });
        }
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, Guid stepId, [FromBody] WorkflowDecisionRequest request)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetRequiredUserId();
            await _service.RejectStepAsync(id, stepId, companyId, userId, request.Comments ?? string.Empty);
            return Ok(ApiResponse.Ok("Etapa rejeitada"));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao rejeitar etapa {StepId} do workflow {Id}", stepId, id);
            return StatusCode(500, new { message = "Erro ao rejeitar etapa" });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] WorkflowDecisionRequest request)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetRequiredUserId();
            await _service.CancelAsync(id, companyId, userId, request.Comments ?? "Cancelado pelo usuário");
            return Ok(ApiResponse.Ok("Workflow cancelado"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar workflow {Id}", id);
            return StatusCode(500, new { message = "Erro ao cancelar workflow" });
        }
    }

}
