using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.Features.Financial.DTOs;
using PartnershipManager.Infrastructure.Services.Financial;

namespace PartnershipManager.API.Controllers;

/// <summary>Per-company configurable financial metrics and dashboard layout.</summary>
[ApiController]
[Route("api/financial")]
[Authorize]
public class FinancialMetricConfigController : ControllerBase
{
    private readonly IFinancialMetricConfigService _service;

    public FinancialMetricConfigController(IFinancialMetricConfigService service)
    {
        _service = service;
    }

    [HttpGet("metric-definitions")]
    public async Task<IActionResult> GetDefinitions([FromQuery] Guid companyId, [FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetDefinitionsAsync(HttpContext.GetRequiredClientId(), companyId, includeInactive);
        return Ok(ApiResponse<IReadOnlyList<FinancialMetricDefinitionResponse>>.Ok(result));
    }

    [HttpPost("metric-definitions")]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateMetricDefinitionRequest request)
    {
        var result = await _service.CreateDefinitionAsync(HttpContext.GetRequiredClientId(), request);
        return Created($"api/financial/metric-definitions/{result.Id}", ApiResponse<FinancialMetricDefinitionResponse>.Ok(result));
    }

    [HttpPut("metric-definitions/{id:guid}")]
    public async Task<IActionResult> UpdateDefinition([FromRoute] Guid id, [FromBody] UpdateMetricDefinitionRequest request)
    {
        var result = await _service.UpdateDefinitionAsync(id, HttpContext.GetRequiredClientId(), request);
        return Ok(ApiResponse<FinancialMetricDefinitionResponse>.Ok(result));
    }

    [HttpDelete("metric-definitions/{id:guid}")]
    public async Task<IActionResult> DeleteDefinition([FromRoute] Guid id)
    {
        await _service.DeleteDefinitionAsync(id, HttpContext.GetRequiredClientId());
        return NoContent();
    }

    [HttpGet("{id:guid}/values")]
    public async Task<IActionResult> GetValues([FromRoute] Guid id)
    {
        var result = await _service.GetPeriodValuesAsync(id, HttpContext.GetRequiredClientId());
        return Ok(ApiResponse<PeriodMetricValuesResponse>.Ok(result));
    }

    [HttpPut("{id:guid}/values")]
    public async Task<IActionResult> SaveValues([FromRoute] Guid id, [FromBody] SavePeriodMetricValuesRequest request)
    {
        var result = await _service.SavePeriodValuesAsync(id, HttpContext.GetRequiredClientId(), request, GetUserId() ?? Guid.Empty);
        return Ok(ApiResponse<PeriodMetricValuesResponse>.Ok(result));
    }

    [HttpGet("metric-series")]
    public async Task<IActionResult> GetSeries([FromQuery] Guid companyId, [FromQuery] int year)
    {
        var result = await _service.GetSeriesAsync(HttpContext.GetRequiredClientId(), companyId, year);
        return Ok(ApiResponse<FinancialMetricSeriesResponse>.Ok(result));
    }

    [HttpGet("dashboard-config")]
    public async Task<IActionResult> GetDashboardConfig([FromQuery] Guid companyId)
    {
        var result = await _service.GetDashboardConfigAsync(HttpContext.GetRequiredClientId(), companyId);
        return Ok(ApiResponse<FinancialDashboardConfigResponse>.Ok(result));
    }

    [HttpPut("dashboard-config")]
    public async Task<IActionResult> SaveDashboardConfig([FromBody] SaveDashboardConfigRequest request)
    {
        var result = await _service.SaveDashboardConfigAsync(HttpContext.GetRequiredClientId(), request);
        return Ok(ApiResponse<FinancialDashboardConfigResponse>.Ok(result));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
