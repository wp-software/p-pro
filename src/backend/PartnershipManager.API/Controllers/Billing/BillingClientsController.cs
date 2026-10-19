using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.Application.Features.Billing.DTOs;
using PartnershipManager.Domain.Entities.Billing;
using PartnershipManager.Domain.Interfaces.Billing;

namespace PartnershipManager.API.Controllers;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/billing/clients")]
public class BillingClientsController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<BillingClientsController> _logger;

    public BillingClientsController(
        IClientRepository clientRepository,
        ILogger<BillingClientsController> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os clientes
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientListResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClientListResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var clients = await _clientRepository.GetAllAsync(cancellationToken);
            
            var response = new List<ClientListResponseDto>();
            foreach (var client in clients)
            {
                var subscriptionsCount = await _clientRepository.GetSubscriptionsCountAsync(client.Id, cancellationToken);
                
                response.Add(new ClientListResponseDto
                {
                    Id = client.Id,
                    Name = client.Name,
                    Email = client.Email,
                    Document = client.Document,
                    Type = client.Type.ToString().ToLower(),
                    Status = MapStatus(client.Status),
                    CreatedAt = client.CreatedAt,
                    SubscriptionsCount = subscriptionsCount
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar clientes");
            return StatusCode(500, new { message = "Erro ao buscar clientes" });
        }
    }

    /// <summary>
    /// Busca cliente por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
            
            if (client == null)
                return NotFound(new { message = "Cliente não encontrado" });

            var subscriptionsCount = await _clientRepository.GetSubscriptionsCountAsync(client.Id, cancellationToken);

            var response = new ClientResponseDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email,
                Document = client.Document,
                Type = client.Type.ToString().ToLower(),
                Status = MapStatus(client.Status),
                Phone = client.Phone,
                Address = client.Address,
                City = client.City,
                State = client.State,
                ZipCode = client.ZipCode,
                Country = client.Country,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                SubscriptionsCount = subscriptionsCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cliente {ClientId}", id);
            return StatusCode(500, new { message = "Erro ao buscar cliente" });
        }
    }

    /// <summary>
    /// Cria um novo cliente
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientResponseDto>> Create(
        [FromBody] ClientCreateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validar se email já existe
            var existingEmail = await _clientRepository.GetByEmailAsync(dto.Email, cancellationToken);
            if (existingEmail != null)
                return BadRequest(new { message = "Email já cadastrado" });

            // Validar se documento já existe
            var existingDocument = await _clientRepository.GetByDocumentAsync(dto.Document, cancellationToken);
            if (existingDocument != null)
                return BadRequest(new { message = "Documento já cadastrado" });

            var client = new Client
            {
                Name = dto.Name,
                Email = dto.Email,
                Document = dto.Document,
                Type = dto.Type.ToLower() == "individual" ? ClientType.Individual : ClientType.Company,
                Status = ClientStatus.Active,
                Phone = dto.Phone,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Country = dto.Country ?? "Brasil"
            };

            var clientId = await _clientRepository.CreateAsync(client, cancellationToken);
            var createdClient = await _clientRepository.GetByIdAsync(clientId, cancellationToken);

            if (createdClient == null)
                return StatusCode(500, new { message = "Erro ao criar cliente" });

            var response = new ClientResponseDto
            {
                Id = createdClient.Id,
                Name = createdClient.Name,
                Email = createdClient.Email,
                Document = createdClient.Document,
                Type = createdClient.Type.ToString().ToLower(),
                Status = MapStatus(createdClient.Status),
                Phone = createdClient.Phone,
                Address = createdClient.Address,
                City = createdClient.City,
                State = createdClient.State,
                ZipCode = createdClient.ZipCode,
                Country = createdClient.Country,
                CreatedAt = createdClient.CreatedAt,
                UpdatedAt = createdClient.UpdatedAt,
                SubscriptionsCount = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente");
            return StatusCode(500, new { message = "Erro ao criar cliente" });
        }
    }

    /// <summary>
    /// Atualiza um cliente
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientResponseDto>> Update(
        Guid id,
        [FromBody] ClientUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingClient = await _clientRepository.GetByIdAsync(id, cancellationToken);
            if (existingClient == null)
                return NotFound(new { message = "Cliente não encontrado" });

            // Validar se email já existe em outro cliente
            var existingEmail = await _clientRepository.GetByEmailAsync(dto.Email, cancellationToken);
            if (existingEmail != null && existingEmail.Id != id)
                return BadRequest(new { message = "Email já cadastrado" });

            // Validar se documento já existe em outro cliente
            var existingDocument = await _clientRepository.GetByDocumentAsync(dto.Document, cancellationToken);
            if (existingDocument != null && existingDocument.Id != id)
                return BadRequest(new { message = "Documento já cadastrado" });

            existingClient.Name = dto.Name;
            existingClient.Email = dto.Email;
            existingClient.Document = dto.Document;
            existingClient.Type = dto.Type.ToLower() == "individual" ? ClientType.Individual : ClientType.Company;
            existingClient.Status = MapStatusFromString(dto.Status);
            existingClient.Phone = dto.Phone;
            existingClient.Address = dto.Address;
            existingClient.City = dto.City;
            existingClient.State = dto.State;
            existingClient.ZipCode = dto.ZipCode;
            existingClient.Country = dto.Country ?? "Brasil";

            var updated = await _clientRepository.UpdateAsync(existingClient, cancellationToken);
            if (!updated)
                return StatusCode(500, new { message = "Erro ao atualizar cliente" });

            var updatedClient = await _clientRepository.GetByIdAsync(id, cancellationToken);
            var subscriptionsCount = await _clientRepository.GetSubscriptionsCountAsync(id, cancellationToken);

            var response = new ClientResponseDto
            {
                Id = updatedClient!.Id,
                Name = updatedClient.Name,
                Email = updatedClient.Email,
                Document = updatedClient.Document,
                Type = updatedClient.Type.ToString().ToLower(),
                Status = MapStatus(updatedClient.Status),
                Phone = updatedClient.Phone,
                Address = updatedClient.Address,
                City = updatedClient.City,
                State = updatedClient.State,
                ZipCode = updatedClient.ZipCode,
                Country = updatedClient.Country,
                CreatedAt = updatedClient.CreatedAt,
                UpdatedAt = updatedClient.UpdatedAt,
                SubscriptionsCount = subscriptionsCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cliente {ClientId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar cliente" });
        }
    }

    /// <summary>
    /// Remove um cliente (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
            if (client == null)
                return NotFound(new { message = "Cliente não encontrado" });

            var deleted = await _clientRepository.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return StatusCode(500, new { message = "Erro ao deletar cliente" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar cliente {ClientId}", id);
            return StatusCode(500, new { message = "Erro ao deletar cliente" });
        }
    }

    private static string MapStatus(ClientStatus status)
    {
        return status switch
        {
            ClientStatus.Active => "active",
            ClientStatus.Suspended => "suspended",
            ClientStatus.Cancelled => "cancelled",
            _ => "active"
        };
    }

    private static ClientStatus MapStatusFromString(string status)
    {
        return status.ToLower() switch
        {
            "active" => ClientStatus.Active,
            "suspended" => ClientStatus.Suspended,
            "cancelled" => ClientStatus.Cancelled,
            _ => ClientStatus.Active
        };
    }
}
