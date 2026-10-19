using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Features.Billing.Commands;
using PartnershipManager.Application.Features.Billing.DTOs;
using PartnershipManager.Application.Features.Billing.Queries;
using PartnershipManager.Domain.Entities.Billing;
using PartnershipManager.Infrastructure.Jobs;

namespace PartnershipManager.API.Controllers.Billing;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IMediator mediator,
        ILogger<InvoicesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as faturas com filtros opcionais
    /// </summary>
    /// <param name="clientId">ID do cliente (opcional)</param>
    /// <param name="status">Status da fatura (opcional)</param>
    /// <param name="startDate">Data inicial do período (opcional)</param>
    /// <param name="endDate">Data final do período (opcional)</param>
    /// <param name="planId">ID do plano (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] Guid? clientId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? planId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            InvoiceStatus? invoiceStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsedStatus))
            {
                invoiceStatus = parsedStatus;
            }

            var query = new GetFilteredInvoicesQuery
            {
                ClientId = clientId,
                Status = invoiceStatus,
                StartDate = startDate,
                EndDate = endDate,
                PlanId = planId
            };

            var invoices = await _mediator.Send(query, cancellationToken);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar faturas");
            return StatusCode(500, new { message = "Erro ao buscar faturas" });
        }
    }

    /// <summary>
    /// Obtém dados de MRR (Monthly Recurring Revenue) dos últimos N meses
    /// </summary>
    /// <param name="months">Número de meses a buscar (padrão: 12)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("mrr")]
    [ProducesResponseType(typeof(MrrDataDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MrrDataDto>> GetMrrData(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetMrrDataQuery { Months = months };
            var mrrData = await _mediator.Send(query, cancellationToken);
            return Ok(mrrData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados de MRR");
            return StatusCode(500, new { message = "Erro ao buscar dados de MRR" });
        }
    }

    /// <summary>
    /// Busca uma fatura por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoiceByIdQuery(id);
            var invoice = await _mediator.Send(query, cancellationToken);

            if (invoice == null)
            {
                return NotFound(new { message = "Fatura não encontrada" });
            }

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fatura {InvoiceId}", id);
            return StatusCode(500, new { message = "Erro ao buscar fatura" });
        }
    }

    /// <summary>
    /// Busca faturas de um cliente específico
    /// </summary>
    [HttpGet("client/{clientId}")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetByClient(Guid clientId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoicesByClientQuery { ClientId = clientId };
            var invoices = await _mediator.Send(query, cancellationToken);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar faturas do cliente {ClientId}", clientId);
            return StatusCode(500, new { message = "Erro ao buscar faturas do cliente" });
        }
    }

    /// <summary>
    /// Cria uma nova fatura
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var invoiceId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = invoiceId }, invoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fatura");
            return StatusCode(500, new { message = "Erro ao criar fatura" });
        }
    }

    /// <summary>
    /// Atualiza uma fatura existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateInvoiceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest(new { message = "ID da URL não corresponde ao ID do corpo da requisição" });
            }

            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = "Fatura não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar fatura {InvoiceId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar fatura" });
        }
    }

    /// <summary>
    /// Marca uma fatura como paga
    /// </summary>
    [HttpPost("{id}/pay")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsPaid(Guid id, [FromBody] MarkAsPaidRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var paymentDate = request?.PaymentDate ?? DateTime.UtcNow;
            var command = new MarkInvoiceAsPaidCommand(id, paymentDate);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = "Fatura não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar fatura {InvoiceId} como paga", id);
            return StatusCode(500, new { message = "Erro ao marcar fatura como paga" });
        }
    }

    public record MarkAsPaidRequest(DateTime? PaymentDate);

    /// <summary>
    /// Cancela uma fatura
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelInvoiceCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = "Fatura não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar fatura {InvoiceId}", id);
            return StatusCode(500, new { message = "Erro ao cancelar fatura" });
        }
    }

    /// <summary>
    /// Deleta uma fatura
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteInvoiceCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = "Fatura não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar fatura {InvoiceId}", id);
            return StatusCode(500, new { message = "Erro ao deletar fatura" });
        }
    }

    /// <summary>
    /// Gera o PDF da fatura
    /// </summary>
    [HttpGet("{id}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoicePdfQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null || result.PdfData == null || result.PdfData.Length == 0)
            {
                return NotFound(new { message = "Fatura não encontrada ou erro ao gerar PDF" });
            }

            return File(result.PdfData, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF da fatura {InvoiceId}", id);
            return StatusCode(500, new { message = "Erro ao gerar PDF da fatura" });
        }
    }

    /// <summary>
    /// Gera faturas mensais para todas as assinaturas ativas
    /// </summary>
    /// <param name="backgroundJobs">Serviço de jobs em background</param>
    /// <param name="month">Mês de referência (1-12)</param>
    /// <param name="year">Ano de referência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPost("generate-monthly")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GenerateMonthly(
        [FromServices] IBackgroundJobs backgroundJobs,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Se não for especificado, usa o mês/ano atual
            var now = DateTime.UtcNow;
            var referenceMonth = month ?? now.Month;
            var referenceYear = year ?? now.Year;

            // Validar mês
            if (referenceMonth < 1 || referenceMonth > 12)
            {
                return BadRequest(new { message = "Mês inválido. Deve estar entre 1 e 12." });
            }

            // Validar ano
            if (referenceYear < 2000 || referenceYear > 2100)
            {
                return BadRequest(new { message = "Ano inválido." });
            }

            await backgroundJobs.GenerateMonthlyInvoicesAsync(referenceMonth, referenceYear);
            
            return Ok(new 
            { 
                message = $"Geração de faturas para {referenceMonth:D2}/{referenceYear} iniciada com sucesso.",
                status = "completed",
                invoicesGenerated = 0 // Será atualizado pelo job
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar faturas mensais");
            return StatusCode(500, new { message = "Erro ao gerar faturas mensais" });
        }
    }

    /// <summary>
    /// Obtém estatísticas de faturas
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetStatistics(
        [FromQuery] Guid? clientId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetInvoiceStatisticsQuery
            {
                ClientId = clientId,
                StartDate = startDate,
                EndDate = endDate
            };

            var statistics = await _mediator.Send(query, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de faturas");
            return StatusCode(500, new { message = "Erro ao buscar estatísticas de faturas" });
        }
    }
}
