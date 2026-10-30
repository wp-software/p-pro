using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.Workflow;
using PartnershipManager.Application.Services;

namespace PartnershipManager.API.Controllers;

/// <summary>Grupos de aprovação e modelos reutilizáveis de fluxo.</summary>
[Route("api")]
[Authorize]
[Produces("application/json")]
public class ApprovalConfigController : BaseApiController
{
    private const string ManagerRoles = "SuperAdmin,Admin,Founder";

    private readonly IApprovalConfigService _service;
    private readonly ILogger<ApprovalConfigController> _logger;

    public ApprovalConfigController(IApprovalConfigService service, ILogger<ApprovalConfigController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Groups ────────────────────────────────────────────────────────────────

    [HttpGet("approval-groups")]
    public async Task<IActionResult> GetGroups()
        => Ok(ApiResponse<IEnumerable<ApprovalGroupResponse>>.Ok(
            await _service.GetGroupsAsync(HttpContext.GetRequiredCompanyId())));

    [HttpGet("approval-groups/{id:guid}")]
    public async Task<IActionResult> GetGroup(Guid id)
    {
        var group = await _service.GetGroupAsync(id, HttpContext.GetRequiredCompanyId());
        return group == null
            ? NotFound(new { message = "Grupo não encontrado" })
            : Ok(ApiResponse<ApprovalGroupResponse>.Ok(group));
    }

    [HttpPost("approval-groups")]
    [Authorize(Roles = ManagerRoles)]
    public Task<IActionResult> CreateGroup([FromBody] SaveApprovalGroupRequest request)
        => Handle(async () =>
        {
            var id = await _service.CreateGroupAsync(HttpContext.GetRequiredCompanyId(), GetRequiredUserId(), request);
            return CreatedAtAction(nameof(GetGroup), new { id }, new { id });
        }, "criar grupo de aprovação");

    [HttpPut("approval-groups/{id:guid}")]
    [Authorize(Roles = ManagerRoles)]
    public Task<IActionResult> UpdateGroup(Guid id, [FromBody] SaveApprovalGroupRequest request)
        => Handle(async () =>
        {
            var found = await _service.UpdateGroupAsync(id, HttpContext.GetRequiredCompanyId(), GetRequiredUserId(), request);
            return found ? Ok(ApiResponse.Ok("Grupo atualizado")) : NotFound(new { message = "Grupo não encontrado" });
        }, "atualizar grupo de aprovação");

    [HttpDelete("approval-groups/{id:guid}")]
    [Authorize(Roles = ManagerRoles)]
    public Task<IActionResult> DeleteGroup(Guid id)
        => Handle(async () =>
        {
            await _service.DeleteGroupAsync(id, HttpContext.GetRequiredCompanyId());
            return Ok(ApiResponse.Ok("Grupo removido"));
        }, "remover grupo de aprovação");

    // ── Templates ─────────────────────────────────────────────────────────────

    [HttpGet("workflow-templates")]
    public async Task<IActionResult> GetTemplates()
        => Ok(ApiResponse<IEnumerable<WorkflowTemplateResponse>>.Ok(
            await _service.GetTemplatesAsync(HttpContext.GetRequiredCompanyId())));

    [HttpGet("workflow-templates/{id:guid}")]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        var template = await _service.GetTemplateAsync(id, HttpContext.GetRequiredCompanyId());
        return template == null
            ? NotFound(new { message = "Modelo não encontrado" })
            : Ok(ApiResponse<WorkflowTemplateResponse>.Ok(template));
    }

    [HttpPost("workflow-templates")]
    public Task<IActionResult> CreateTemplate([FromBody] SaveWorkflowTemplateRequest request)
        => Handle(async () =>
        {
            var id = await _service.CreateTemplateAsync(HttpContext.GetRequiredCompanyId(), GetRequiredUserId(), request);
            return CreatedAtAction(nameof(GetTemplate), new { id }, new { id });
        }, "criar modelo de fluxo");

    [HttpPut("workflow-templates/{id:guid}")]
    public Task<IActionResult> UpdateTemplate(Guid id, [FromBody] SaveWorkflowTemplateRequest request)
        => Handle(async () =>
        {
            var found = await _service.UpdateTemplateAsync(id, HttpContext.GetRequiredCompanyId(), GetRequiredUserId(), request);
            return found ? Ok(ApiResponse.Ok("Modelo atualizado")) : NotFound(new { message = "Modelo não encontrado" });
        }, "atualizar modelo de fluxo");

    [HttpDelete("workflow-templates/{id:guid}")]
    public Task<IActionResult> DeleteTemplate(Guid id)
        => Handle(async () =>
        {
            await _service.DeleteTemplateAsync(id, HttpContext.GetRequiredCompanyId());
            return Ok(ApiResponse.Ok("Modelo removido"));
        }, "remover modelo de fluxo");

    private async Task<IActionResult> Handle(Func<Task<IActionResult>> action, string description)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao {Action}", description);
            return StatusCode(500, new { message = $"Erro ao {description}" });
        }
    }
}
