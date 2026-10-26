using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.DataRoom;
using PartnershipManager.Application.Services;
using PartnershipManager.Domain.Entities;

namespace PartnershipManager.API.Controllers;

[Route("api/dataroom")]
[Authorize]
[Produces("application/json")]
public class DataRoomController : BaseApiController
{
    private const long MaxUploadBytes = 50L * 1024 * 1024;

    private readonly IDataRoomService _service;
    private readonly ILogger<DataRoomController> _logger;
    private readonly string _storageBasePath;

    public DataRoomController(IDataRoomService service, ILogger<DataRoomController> logger, IConfiguration configuration)
    {
        _service = service;
        _logger = logger;
        _storageBasePath = configuration["Storage:BasePath"] ?? "/app/storage";
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var userId = GetUserId() ?? Guid.Empty;
            var result = await _service.GetOrCreateDataRoomAsync(companyId, userId);
            return Ok(ApiResponse<DataRoomResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter data room");
            return StatusCode(500, new { message = "Erro ao obter data room" });
        }
    }

    private DataRoomUser CurrentUser()
        => new(GetUserId() ?? Guid.Empty,
               User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value,
               User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList());

    private IActionResult Fail(Exception ex, string message) => ex switch
    {
        UnauthorizedAccessException => StatusCode(403, new { message = ex.Message }),
        KeyNotFoundException => NotFound(new { message = ex.Message }),
        ArgumentException => BadRequest(new { message = ex.Message }),
        _ => LogAndFail(ex, message)
    };

    private IActionResult LogAndFail(Exception ex, string message)
    {
        _logger.LogError(ex, "{Message}", message);
        return StatusCode(500, new { message });
    }

    [HttpGet("folders")]
    public async Task<IActionResult> GetFolders([FromQuery] Guid? parentId = null)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var result = await _service.GetFoldersAsync(companyId, CurrentUser(), parentId);
            return Ok(ApiResponse<IEnumerable<DataRoomFolderResponse>>.Ok(result));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao buscar pastas"); }
    }

    [HttpPost("folders")]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
    {
        try
        {
            var companyId = HttpContext.GetRequiredCompanyId();
            var id = await _service.CreateFolderAsync(companyId, request, CurrentUser());
            return CreatedAtAction(nameof(GetFolders), new { }, new { id });
        }
        catch (Exception ex) { return Fail(ex, "Erro ao criar pasta"); }
    }

    [HttpDelete("folders/{id:guid}")]
    public async Task<IActionResult> DeleteFolder(Guid id)
    {
        try
        {
            await _service.DeleteFolderAsync(HttpContext.GetRequiredCompanyId(), id, CurrentUser());
            return NoContent();
        }
        catch (Exception ex) { return Fail(ex, "Erro ao deletar pasta"); }
    }

    [HttpPut("folders/{id:guid}/roles")]
    public async Task<IActionResult> SetFolderRoles(Guid id, [FromBody] SetFolderRolesRequest request)
    {
        try
        {
            await _service.SetFolderRolesAsync(HttpContext.GetRequiredCompanyId(), id, request.Roles, CurrentUser());
            return Ok(ApiResponse.Ok("Permissões da pasta atualizadas"));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao atualizar permissões da pasta"); }
    }

    [HttpGet("folders/{id:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        try
        {
            var result = await _service.GetDocumentsInFolderAsync(HttpContext.GetRequiredCompanyId(), id, CurrentUser());
            return Ok(ApiResponse<IEnumerable<object>>.Ok(result.Select(d => new
            {
                d.Id, d.Name, d.DocumentType, d.FileName, d.FileSizeBytes, d.MimeType,
                d.Visibility, d.IsVerified, d.CreatedAt, d.DownloadUrl, HasStoredFile = (d.StoragePath ?? "").StartsWith("dataroom")
            })));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao buscar documentos"); }
    }

    [HttpPost("folders/{id:guid}/documents")]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddDocumentToFolderRequest request)
    {
        try
        {
            await _service.AddDocumentToFolderAsync(HttpContext.GetRequiredCompanyId(), id, request.DocumentId, CurrentUser());
            return Ok(ApiResponse.Ok("Documento adicionado à pasta"));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao adicionar documento"); }
    }

    [HttpDelete("folders/{folderId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> RemoveDocument(Guid folderId, Guid documentId)
    {
        try
        {
            await _service.RemoveDocumentFromFolderAsync(HttpContext.GetRequiredCompanyId(), folderId, documentId, CurrentUser());
            return NoContent();
        }
        catch (Exception ex) { return Fail(ex, "Erro ao remover documento"); }
    }

    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs([FromQuery] Guid? folderId = null, [FromQuery] int limit = 100)
    {
        try
        {
            var result = await _service.GetActivityLogsAsync(HttpContext.GetRequiredCompanyId(), CurrentUser(), folderId, limit);
            return Ok(ApiResponse<IEnumerable<DataRoomActivityLogResponse>>.Ok(result));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao buscar log de atividades"); }
    }

    [HttpPost("financial-documents")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadFinancialDocument(
        IFormFile file, [FromForm] int year, [FromForm] int month,
        [FromForm] Guid? periodId, [FromForm] string? metricKey, [FromForm] string? name, [FromForm] string? documentType)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Arquivo é obrigatório." });
            if (file.Length > MaxUploadBytes)
                return BadRequest(new { message = "Arquivo excede o limite de 50 MB." });

            var companyId = HttpContext.GetRequiredCompanyId();
            var clientId = HttpContext.GetRequiredClientId();
            var originalName = Path.GetFileName(file.FileName);
            var type = string.IsNullOrWhiteSpace(documentType) ? DocumentTypes.Other : documentType;
            var docName = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(originalName) : name.Trim();

            var result = await _service.UploadFinancialDocumentAsync(
                clientId, companyId, year, month, periodId, metricKey, docName, type, originalName, file.Length,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                async () =>
                {
                    var relative = Path.Combine("dataroom", companyId.ToString(), $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}");
                    var full = Path.Combine(_storageBasePath, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    await using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write);
                    await file.CopyToAsync(fs);
                    return relative;
                },
                CurrentUser());

            return Ok(ApiResponse<FinancialUploadResult>.Ok(result));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao enviar documento financeiro"); }
    }

    [HttpGet("financial-documents")]
    public async Task<IActionResult> GetFinancialDocuments([FromQuery] Guid periodId)
    {
        try
        {
            var result = await _service.GetFinancialDocumentsAsync(
                HttpContext.GetRequiredClientId(), HttpContext.GetRequiredCompanyId(), periodId, CurrentUser());
            return Ok(ApiResponse<IEnumerable<FinancialDocumentResponse>>.Ok(result));
        }
        catch (Exception ex) { return Fail(ex, "Erro ao buscar documentos financeiros"); }
    }

    [HttpDelete("financial-documents/{documentId:guid}")]
    public async Task<IActionResult> RemoveFinancialDocument(Guid documentId)
    {
        try
        {
            await _service.RemoveFinancialDocumentAsync(
                HttpContext.GetRequiredClientId(), HttpContext.GetRequiredCompanyId(), documentId, CurrentUser());
            return NoContent();
        }
        catch (Exception ex) { return Fail(ex, "Erro ao remover documento financeiro"); }
    }

    [HttpGet("folders/{folderId:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid folderId, Guid documentId)
    {
        try
        {
            var doc = await _service.GetDocumentForDownloadAsync(HttpContext.GetRequiredCompanyId(), folderId, documentId, CurrentUser());
            var root = Path.GetFullPath(_storageBasePath);
            var full = Path.GetFullPath(Path.Combine(root, doc.StoragePath ?? string.Empty));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar) || !System.IO.File.Exists(full))
                return NotFound(new { message = "Arquivo não disponível para download." });

            return PhysicalFile(full, doc.MimeType ?? "application/octet-stream", doc.FileName);
        }
        catch (Exception ex) { return Fail(ex, "Erro ao baixar documento"); }
    }
}
