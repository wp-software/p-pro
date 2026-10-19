using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Features.Billing.DTOs;
using PartnershipManager.Domain.Entities.Billing;
using PartnershipManager.Domain.Interfaces.Billing;

namespace PartnershipManager.API.Controllers.Billing;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionRepository subscriptionRepository,
        IClientRepository clientRepository,
        IPlanRepository planRepository,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _clientRepository = clientRepository;
        _planRepository = planRepository;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as assinaturas
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionListResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubscriptionListResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.GetAllAsync(cancellationToken);
            
            var response = subscriptions.Select(s => new SubscriptionListResponseDto
            {
                Id = s.Id,
                ClientId = s.ClientId,
                ClientName = s.Client.Name,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                PlanPrice = s.Plan.Price,
                BillingCycle = s.Plan.BillingCycle.ToString().ToLower(),
                Status = MapStatus(s.Status),
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                AutoRenew = s.AutoRenew,
                CompaniesCount = s.CompaniesCount,
                UsersCount = s.UsersCount,
                CreatedAt = s.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar assinaturas");
            return StatusCode(500, new { message = "Erro ao buscar assinaturas" });
        }
    }

    /// <summary>
    /// Busca assinatura por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            var response = new SubscriptionResponseDto
            {
                Id = subscription.Id,
                ClientId = subscription.ClientId,
                ClientName = subscription.Client.Name,
                ClientEmail = subscription.Client.Email,
                PlanId = subscription.PlanId,
                PlanName = subscription.Plan.Name,
                PlanPrice = subscription.Plan.Price,
                BillingCycle = subscription.Plan.BillingCycle.ToString().ToLower(),
                Status = MapStatus(subscription.Status),
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                AutoRenew = subscription.AutoRenew,
                CompaniesCount = subscription.CompaniesCount,
                UsersCount = subscription.UsersCount,
                DueDay = subscription.DueDay,
                PaymentMethod = MapPaymentMethod(subscription.PaymentMethod),
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao buscar assinatura" });
        }
    }

    /// <summary>
    /// Busca assinaturas por cliente
    /// </summary>
    [HttpGet("client/{clientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionListResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubscriptionListResponseDto>>> GetByClientId(Guid clientId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.GetByClientIdAsync(clientId, cancellationToken);
            
            var response = subscriptions.Select(s => new SubscriptionListResponseDto
            {
                Id = s.Id,
                ClientId = s.ClientId,
                ClientName = s.Client.Name,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                PlanPrice = s.Plan.Price,
                BillingCycle = s.Plan.BillingCycle.ToString().ToLower(),
                Status = MapStatus(s.Status),
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                AutoRenew = s.AutoRenew,
                CompaniesCount = s.CompaniesCount,
                UsersCount = s.UsersCount,
                CreatedAt = s.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar assinaturas do cliente {ClientId}", clientId);
            return StatusCode(500, new { message = "Erro ao buscar assinaturas" });
        }
    }

    /// <summary>
    /// Cria uma nova assinatura
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionResponseDto>> Create(
        [FromBody] SubscriptionCreateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validar se cliente existe
            var client = await _clientRepository.GetByIdAsync(dto.ClientId, cancellationToken);
            if (client == null)
                return BadRequest(new { message = "Cliente não encontrado" });

            // Validar se plano existe
            var plan = await _planRepository.GetByIdAsync(dto.PlanId, cancellationToken);
            if (plan == null)
                return BadRequest(new { message = "Plano não encontrado" });

            var subscription = new Subscription
            {
                ClientId = dto.ClientId,
                PlanId = dto.PlanId,
                Status = SubscriptionStatus.Active,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                AutoRenew = dto.AutoRenew,
                CompaniesCount = 0,
                UsersCount = 0,
                DueDay = dto.DueDay,
                PaymentMethod = ParsePaymentMethod(dto.PaymentMethod)
            };

            var subscriptionId = await _subscriptionRepository.CreateAsync(subscription, cancellationToken);
            var createdSubscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);

            if (createdSubscription == null)
                return StatusCode(500, new { message = "Erro ao criar assinatura" });

            var response = new SubscriptionResponseDto
            {
                Id = createdSubscription.Id,
                ClientId = createdSubscription.ClientId,
                ClientName = createdSubscription.Client.Name,
                ClientEmail = createdSubscription.Client.Email,
                PlanId = createdSubscription.PlanId,
                PlanName = createdSubscription.Plan.Name,
                PlanPrice = createdSubscription.Plan.Price,
                BillingCycle = createdSubscription.Plan.BillingCycle.ToString().ToLower(),
                Status = MapStatus(createdSubscription.Status),
                StartDate = createdSubscription.StartDate,
                EndDate = createdSubscription.EndDate,
                AutoRenew = createdSubscription.AutoRenew,
                CompaniesCount = createdSubscription.CompaniesCount,
                UsersCount = createdSubscription.UsersCount,
                DueDay = createdSubscription.DueDay,
                PaymentMethod = MapPaymentMethod(createdSubscription.PaymentMethod),
                CreatedAt = createdSubscription.CreatedAt,
                UpdatedAt = createdSubscription.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar assinatura");
            return StatusCode(500, new { message = "Erro ao criar assinatura" });
        }
    }

    /// <summary>
    /// Atualiza uma assinatura
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionResponseDto>> Update(
        Guid id,
        [FromBody] SubscriptionUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Recebeu DTO - PlanId: {dto.PlanId}, StartDate: {dto.StartDate}, EndDate: {dto.EndDate}");
            
            var existingSubscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (existingSubscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            // Validar se plano existe (se mudou)
            if (dto.PlanId != existingSubscription.PlanId)
            {
                var plan = await _planRepository.GetByIdAsync(dto.PlanId, cancellationToken);
                if (plan == null)
                    return BadRequest(new { message = "Plano não encontrado" });
                
                existingSubscription.PlanId = dto.PlanId;
            }

            existingSubscription.AutoRenew = dto.AutoRenew;
            existingSubscription.CompaniesCount = dto.CompaniesCount;
            existingSubscription.UsersCount = dto.UsersCount;
            existingSubscription.StartDate = dto.StartDate;
            existingSubscription.EndDate = dto.EndDate;
            existingSubscription.DueDay = dto.DueDay;
            existingSubscription.PaymentMethod = ParsePaymentMethod(dto.PaymentMethod);
            
            _logger.LogInformation($"Antes do update - StartDate: {existingSubscription.StartDate}, EndDate: {existingSubscription.EndDate}");

            await _subscriptionRepository.UpdateAsync(existingSubscription, cancellationToken);
            
            var updatedSubscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (updatedSubscription == null)
                return StatusCode(500, new { message = "Erro ao atualizar assinatura" });

            var response = new SubscriptionResponseDto
            {
                Id = updatedSubscription.Id,
                ClientId = updatedSubscription.ClientId,
                ClientName = updatedSubscription.Client.Name,
                ClientEmail = updatedSubscription.Client.Email,
                PlanId = updatedSubscription.PlanId,
                PlanName = updatedSubscription.Plan.Name,
                PlanPrice = updatedSubscription.Plan.Price,
                BillingCycle = updatedSubscription.Plan.BillingCycle.ToString().ToLower(),
                Status = MapStatus(updatedSubscription.Status),
                StartDate = updatedSubscription.StartDate,
                EndDate = updatedSubscription.EndDate,
                AutoRenew = updatedSubscription.AutoRenew,
                CompaniesCount = updatedSubscription.CompaniesCount,
                UsersCount = updatedSubscription.UsersCount,
                DueDay = updatedSubscription.DueDay,
                PaymentMethod = MapPaymentMethod(updatedSubscription.PaymentMethod),
                CreatedAt = updatedSubscription.CreatedAt,
                UpdatedAt = updatedSubscription.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar assinatura" });
        }
    }

    /// <summary>
    /// Ativa uma assinatura
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            subscription.Activate();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Ok(new { message = "Assinatura ativada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao ativar assinatura" });
        }
    }

    /// <summary>
    /// Suspende uma assinatura
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            subscription.Suspend();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Ok(new { message = "Assinatura suspensa com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao suspender assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao suspender assinatura" });
        }
    }

    /// <summary>
    /// Cancela uma assinatura
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            subscription.Cancel();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

            return Ok(new { message = "Assinatura cancelada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao cancelar assinatura" });
        }
    }

    /// <summary>
    /// Deleta uma assinatura
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            await _subscriptionRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar assinatura {SubscriptionId}", id);
            return StatusCode(500, new { message = "Erro ao deletar assinatura" });
        }
    }

    private static string MapStatus(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Pending => "pending",
        SubscriptionStatus.Active => "active",
        SubscriptionStatus.Suspended => "suspended",
        SubscriptionStatus.Cancelled => "cancelled",
        _ => "unknown"
    };

    private static string MapPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "bank_transfer",
        PaymentMethod.CreditCard => "credit_card",
        PaymentMethod.Pix => "pix",
        PaymentMethod.Boleto => "boleto",
        PaymentMethod.Cash => "cash",
        PaymentMethod.Other => "other",
        _ => "other"
    };

    private static PaymentMethod ParsePaymentMethod(string method) => method?.ToLower() switch
    {
        "bank_transfer" => PaymentMethod.BankTransfer,
        "credit_card" => PaymentMethod.CreditCard,
        "pix" => PaymentMethod.Pix,
        "boleto" => PaymentMethod.Boleto,
        "cash" => PaymentMethod.Cash,
        _ => PaymentMethod.Pix
    };
}
