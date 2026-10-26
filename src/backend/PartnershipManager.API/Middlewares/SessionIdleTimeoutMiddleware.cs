using System.Security.Claims;
using PartnershipManager.Domain.Constants;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.API.Middlewares;

public sealed class SessionIdleTimeoutMiddleware
{
    private readonly RequestDelegate _next;

    public SessionIdleTimeoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedException();
            }

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null ||
                user.Status != UserStatus.Active ||
                user.IsLockedOut ||
                string.IsNullOrWhiteSpace(user.RefreshToken) ||
                user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Sessão expirada por inatividade.");
            }
        }

        await _next(context);
    }
}
