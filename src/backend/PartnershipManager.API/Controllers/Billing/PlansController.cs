using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Domain.Entities.Billing;
using PartnershipManager.Domain.Interfaces.Billing;
using System.Text.Json;

namespace PartnershipManager.API.Controllers.Billing;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PlansController> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public PlansController(
        IPlanRepository planRepository,
        ILogger<PlansController> logger)
    {
        _planRepository = planRepository;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os planos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var plans = await _planRepository.GetAllAsync(cancellationToken);
            
            var response = plans.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                BillingCycle = p.BillingCycle.ToString().ToLower(),
                p.MaxCompanies,
                p.MaxUsers,
                Features = string.IsNullOrEmpty(p.Features) ? new string[0] : JsonSerializer.Deserialize<string[]>(p.Features, _jsonOptions),
                p.IsActive,
                p.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar planos");
            return StatusCode(500, new { message = "Erro ao buscar planos" });
        }
    }

    /// <summary>
    /// Lista apenas planos ativos
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var plans = await _planRepository.GetActiveAsync(cancellationToken);
            
            var response = plans.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                BillingCycle = p.BillingCycle.ToString().ToLower(),
                p.MaxCompanies,
                p.MaxUsers,
                Features = string.IsNullOrEmpty(p.Features) ? new string[0] : JsonSerializer.Deserialize<string[]>(p.Features, _jsonOptions),
                p.IsActive,
                p.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar planos ativos");
            return StatusCode(500, new { message = "Erro ao buscar planos ativos" });
        }
    }

    /// <summary>
    /// Busca plano por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
            
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            var response = new
            {
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price,
                BillingCycle = plan.BillingCycle.ToString().ToLower(),
                plan.MaxCompanies,
                plan.MaxUsers,
                Features = string.IsNullOrEmpty(plan.Features) ? new string[0] : JsonSerializer.Deserialize<string[]>(plan.Features, _jsonOptions),
                plan.IsActive,
                plan.CreatedAt,
                plan.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar plano {PlanId}", id);
            return StatusCode(500, new { message = "Erro ao buscar plano" });
        }
    }

    /// <summary>
    /// Cria um novo plano
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(
        [FromBody] PlanCreateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = new Plan
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                BillingCycle = dto.BillingCycle.ToLower() == "monthly" ? BillingCycle.Monthly : BillingCycle.Yearly,
                MaxCompanies = dto.MaxCompanies,
                MaxUsers = dto.MaxUsers,
                Features = dto.Features != null && dto.Features.Length > 0 
                    ? JsonSerializer.Serialize(dto.Features, _jsonOptions) 
                    : null,
                IsActive = dto.IsActive
            };

            var planId = await _planRepository.CreateAsync(plan, cancellationToken);
            var createdPlan = await _planRepository.GetByIdAsync(planId, cancellationToken);

            if (createdPlan == null)
                return StatusCode(500, new { message = "Erro ao criar plano" });

            var response = new
            {
                createdPlan.Id,
                createdPlan.Name,
                createdPlan.Description,
                createdPlan.Price,
                BillingCycle = createdPlan.BillingCycle.ToString().ToLower(),
                createdPlan.MaxCompanies,
                createdPlan.MaxUsers,
                Features = string.IsNullOrEmpty(createdPlan.Features) ? new string[0] : JsonSerializer.Deserialize<string[]>(createdPlan.Features, _jsonOptions),
                createdPlan.IsActive,
                createdPlan.CreatedAt,
                createdPlan.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar plano");
            return StatusCode(500, new { message = "Erro ao criar plano" });
        }
    }

    /// <summary>
    /// Atualiza um plano
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] PlanUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingPlan = await _planRepository.GetByIdAsync(id, cancellationToken);
            if (existingPlan == null)
                return NotFound(new { message = "Plano não encontrado" });

            existingPlan.Name = dto.Name;
            existingPlan.Description = dto.Description;
            existingPlan.Price = dto.Price;
            existingPlan.BillingCycle = dto.BillingCycle.ToLower() == "monthly" ? BillingCycle.Monthly : BillingCycle.Yearly;
            existingPlan.MaxCompanies = dto.MaxCompanies;
            existingPlan.MaxUsers = dto.MaxUsers;
            existingPlan.Features = dto.Features != null && dto.Features.Length > 0 
                ? JsonSerializer.Serialize(dto.Features, _jsonOptions) 
                : null;
            existingPlan.IsActive = dto.IsActive;

            await _planRepository.UpdateAsync(existingPlan, cancellationToken);
            
            var updatedPlan = await _planRepository.GetByIdAsync(id, cancellationToken);
            if (updatedPlan == null)
                return StatusCode(500, new { message = "Erro ao atualizar plano" });

            var response = new
            {
                updatedPlan.Id,
                updatedPlan.Name,
                updatedPlan.Description,
                updatedPlan.Price,
                BillingCycle = updatedPlan.BillingCycle.ToString().ToLower(),
                updatedPlan.MaxCompanies,
                updatedPlan.MaxUsers,
                Features = string.IsNullOrEmpty(updatedPlan.Features) ? new string[0] : JsonSerializer.Deserialize<string[]>(updatedPlan.Features, _jsonOptions),
                updatedPlan.IsActive,
                updatedPlan.CreatedAt,
                updatedPlan.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar plano {PlanId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar plano" });
        }
    }

    /// <summary>
    /// Deleta um plano
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            await _planRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar plano {PlanId}", id);
            return StatusCode(500, new { message = "Erro ao deletar plano" });
        }
    }

    /// <summary>
    /// Ativa/Desativa um plano
    /// </summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            plan.IsActive = !plan.IsActive;
            await _planRepository.UpdateAsync(plan, cancellationToken);

            return Ok(new { message = $"Plano {(plan.IsActive ? "ativado" : "desativado")} com sucesso", isActive = plan.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar status do plano {PlanId}", id);
            return StatusCode(500, new { message = "Erro ao alternar status do plano" });
        }
    }
}

public record PlanCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string BillingCycle { get; init; } = "monthly";
    public int MaxCompanies { get; init; }
    public int MaxUsers { get; init; }
    public string[]? Features { get; init; }
    public bool IsActive { get; init; } = true;
}

public record PlanUpdateDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string BillingCycle { get; init; } = "monthly";
    public int MaxCompanies { get; init; }
    public int MaxUsers { get; init; }
    public string[]? Features { get; init; }
    public bool IsActive { get; init; }
}
