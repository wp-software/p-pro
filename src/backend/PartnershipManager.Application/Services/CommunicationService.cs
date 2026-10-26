using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnershipManager.Application.Common.Security;
using PartnershipManager.Application.Common.Models;
using PartnershipManager.Application.DTOs.Communication;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Application.Services;

public interface ICommunicationService
{
    Task<PagedResult<CommunicationListResponse>> GetByCompanyAsync(Guid companyId, Guid? userId, int page, int pageSize, string? search = null, string? commType = null, bool? isPublished = null);
    Task<CommunicationResponse?> GetByIdAsync(Guid id, Guid companyId, Guid? userId = null);
    Task<Guid> CreateAsync(Guid companyId, CreateCommunicationRequest request, Guid userId);
    Task UpdateAsync(Guid id, Guid companyId, UpdateCommunicationRequest request, Guid userId);
    Task PublishAsync(Guid id, Guid companyId, Guid userId);
    Task DeleteAsync(Guid id, Guid companyId, Guid userId);
    Task TrackViewAsync(Guid id, Guid userId, int? durationSecs);
    Task<IEnumerable<CommunicationListResponse>> GetForPortalAsync(Guid companyId, string role, int limit = 10);
}

public class CommunicationService : ICommunicationService
{
    private readonly ICommunicationRepository _repo;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CommunicationService> _logger;
    private readonly IConfiguration _configuration;

    public CommunicationService(
        ICommunicationRepository repo,
        INotificationService notificationService,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<CommunicationService> logger,
        IConfiguration configuration)
    {
        _repo = repo;
        _notificationService = notificationService;
        _emailService = emailService;
        _uow = uow;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetFrontendUrl()
    {
        return (_configuration["Email:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
    }

    public async Task<PagedResult<CommunicationListResponse>> GetByCompanyAsync(
        Guid companyId, Guid? userId, int page, int pageSize,
        string? search = null, string? commType = null, bool? isPublished = null)
    {
        var (items, total) = await _repo.GetByCompanyAsync(companyId, page, pageSize, search, commType, isPublished);
        var mapped = items.Select(c => MapToListResponse(c));
        return new PagedResult<CommunicationListResponse>(mapped, total, page, pageSize);
    }

    public async Task<CommunicationResponse?> GetByIdAsync(Guid id, Guid companyId, Guid? userId = null)
    {
        var c = await _repo.GetByIdAsync(id, companyId);
        if (c == null) return null;

        bool hasViewed = userId.HasValue && await _repo.HasViewedAsync(id, userId.Value);
        return MapToResponse(c, hasViewed);
    }

    public async Task<Guid> CreateAsync(Guid companyId, CreateCommunicationRequest request, Guid userId)
    {
        var c = new Communication
        {
            CompanyId = companyId,
            Title = request.Title,
            Content = request.Content,
            ContentHtml = HtmlContentSanitizer.Sanitize(request.ContentHtml),
            Summary = request.Summary,
            CommType = request.CommType,
            Visibility = request.Visibility,
            TargetRoles = request.TargetRoles,
            SendEmail = request.SendEmail,
            IsPinned = request.IsPinned,
            ExpiresAt = request.ExpiresAt
        };
        c.CreatedBy = userId;
        c.UpdatedBy = userId;
        return await _repo.CreateAsync(c);
    }

    public async Task UpdateAsync(Guid id, Guid companyId, UpdateCommunicationRequest request, Guid userId)
    {
        var c = await _repo.GetByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Comunicação não encontrada.");

        c.Title = request.Title;
        c.Content = request.Content;
        c.ContentHtml = HtmlContentSanitizer.Sanitize(request.ContentHtml);
        c.Summary = request.Summary;
        c.CommType = request.CommType;
        c.Visibility = request.Visibility;
        c.TargetRoles = request.TargetRoles;
        c.SendEmail = request.SendEmail;
        c.IsPinned = request.IsPinned;
        c.ExpiresAt = request.ExpiresAt;
        c.UpdatedBy = userId;

        await _repo.UpdateAsync(c);
    }

    public async Task PublishAsync(Guid id, Guid companyId, Guid userId)
    {
        var communication = await _repo.GetByIdAsync(id, companyId);
        if (communication == null) return;

        await _repo.PublishAsync(id, companyId);

        // Determinar usuários a notificar com base na visibilidade
        var users = await _uow.Users.GetActiveUsersByCompanyAsync(companyId);
        List<Domain.Entities.User> targetUsers;

        if (communication.Visibility == CommunicationVisibilities.All)
        {
            targetUsers = users.Where(u => u.Id != userId).ToList();
        }
        else
        {
            // Mapear visibility para roles
            var visibilityRoleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { CommunicationVisibilities.Investors, "Investor" },
                { CommunicationVisibilities.Founders, "Founder" },
                { CommunicationVisibilities.Employees, "Employee" },
            };

            IEnumerable<string> targetRoles;
            if (communication.Visibility == CommunicationVisibilities.Specific && !string.IsNullOrEmpty(communication.TargetRoles))
            {
                targetRoles = JsonSerializer.Deserialize<IEnumerable<string>>(communication.TargetRoles) ?? [];
            }
            else if (visibilityRoleMap.TryGetValue(communication.Visibility, out var mappedRole))
            {
                targetRoles = [mappedRole];
            }
            else
            {
                targetRoles = [];
            }

            var targetRoleSet = new HashSet<string>(targetRoles, StringComparer.OrdinalIgnoreCase);
            targetUsers = [];
            foreach (var user in users.Where(u => u.Id != userId))
            {
                var roles = await _uow.UserRoles.GetRoleNamesByUserIdAsync(user.Id);
                if (roles.Any(r => targetRoleSet.Contains(r)))
                    targetUsers.Add(user);
            }
        }

        if (targetUsers.Count == 0) return;

        var body = !string.IsNullOrEmpty(communication.Summary)
            ? communication.Summary
            : $"Nova comunicação publicada: {communication.Title}";

        // Notificação in-app para todos os usuários alvo
        await _notificationService.NotifyUsersAsync(
            companyId,
            targetUsers.Select(u => u.Id),
            "communication_published",
            communication.Title,
            body,
            actionUrl: $"/portal/communications/{id}",
            referenceType: "communication",
            referenceId: id
        );

        // Envio de e-mail — somente se `send_email` estiver habilitado na comunicação
        if (!communication.SendEmail) return;

        var company = await _uow.Companies.GetByIdAsync(companyId);
        var companyName = company?.Name ?? "Partnership Manager";
        var actionUrl = $"{GetFrontendUrl()}/portal/communications/{id}";

        foreach (var user in targetUsers)
        {
            var pref = await _notificationService.GetPreferenceChannelAsync(user.Id, "communication_published");
            if (pref == "none" || pref == "in_app") continue;

            try
            {
                await _emailService.SendCommunicationPublishedEmailAsync(
                    user.Email,
                    user.Name,
                    communication.Title,
                    communication.Summary,
                    communication.CommType,
                    actionUrl,
                    companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de comunicação para {Email}", user.Email);
                // Não propaga — notificações in-app já foram enviadas
            }
        }
    }

    public Task DeleteAsync(Guid id, Guid companyId, Guid userId)
        => _repo.SoftDeleteAsync(id, companyId);

    public Task TrackViewAsync(Guid id, Guid userId, int? durationSecs)
        => _repo.TrackViewAsync(id, userId, durationSecs);

    public async Task<IEnumerable<CommunicationListResponse>> GetForPortalAsync(Guid companyId, string role, int limit = 10)
    {
        var items = await _repo.GetForRoleAsync(companyId, role, limit);
        return items.Select(c => MapToListResponse(c));
    }

    private static CommunicationListResponse MapToListResponse(Communication c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        CommType = c.CommType,
        Visibility = c.Visibility,
        IsPinned = c.IsPinned,
        PublishedAt = c.PublishedAt,
        CreatedAt = c.CreatedAt,
        ViewsCount = c.ViewsCount,
        Summary = c.Summary
    };

    private static CommunicationResponse MapToResponse(Communication c, bool hasViewed) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        Title = c.Title,
        Content = c.Content,
        ContentHtml = c.ContentHtml,
        Summary = c.Summary,
        CommType = c.CommType,
        Visibility = c.Visibility,
        TargetRoles = c.TargetRoles,
        IsPinned = c.IsPinned,
        PublishedAt = c.PublishedAt,
        ExpiresAt = c.ExpiresAt,
        ViewsCount = c.ViewsCount,
        HasViewed = hasViewed,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
