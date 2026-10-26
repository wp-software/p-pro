using System.Security.Claims;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.API.Middlewares;

/// <summary>
/// Middleware para extrair e validar o contexto da Company a partir do header ou query
/// Valida se o usuário tem acesso à company através da tabela user_companies
/// </summary>
public class CompanyContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CompanyContextMiddleware> _logger;
    private const string CompanyIdHeader = "X-Company-Id";
    
    public CompanyContextMiddleware(RequestDelegate next, ILogger<CompanyContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, ICompanyRepository companyRepository)
    {
        // Ignorar rotas que não requerem company context
        if (ShouldSkipValidation(context))
        {
            await _next(context);
            return;
        }
        
        var user = context.User;
        
        if (user?.Identity?.IsAuthenticated == true)
        {
            var clientId = context.GetClientId();
            
            // Tentar obter CompanyId do header ou query
            var companyIdStr = context.Request.Headers[CompanyIdHeader].FirstOrDefault() 
                            ?? context.Request.Query["companyId"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(companyIdStr) && Guid.TryParse(companyIdStr, out var companyId))
            {
                // Validar se a company existe
                var company = await companyRepository.GetByIdAsync(companyId);
                
                if (company == null)
                {
                    _logger.LogWarning("Company não encontrada: {CompanyId}", companyId);
                    throw new NotFoundException("Empresa", companyId);
                }
                
                // Validar se a company pertence ao mesmo client do usuário
                var isSuperAdmin = user.IsInRole("SuperAdmin");
                if (isSuperAdmin)
                {
                    // SuperAdmin acessa qualquer tenant: assume o client da empresa selecionada
                    context.Items["ClientId"] = company.ClientId;
                }
                else if (clientId.HasValue && company.ClientId != clientId.Value)
                {
                    _logger.LogWarning(
                        "Usuário tentou acessar company de outro client. UserId: {UserId}, CompanyId: {CompanyId}, CompanyClientId: {CompanyClientId}, UserClientId: {UserClientId}",
                        user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        companyId,
                        company.ClientId,
                        clientId.Value);
                    
                    throw new ForbiddenException("Acesso negado a esta empresa");
                }
                
                // Validar se a company está ativa
                if (company.Status.ToString() != "Active")
                {
                    _logger.LogWarning("Company inativa foi solicitada: {CompanyId} - Status: {Status}", 
                        companyId, company.Status);
                    throw new ForbiddenException("Empresa inativa");
                }
                
                // TODO: Validar se o usuário tem acesso através de user_companies
                // Por enquanto, apenas validamos se pertence ao mesmo client
                
                // Adicionar CompanyId aos items do contexto HTTP
                context.Items["CompanyId"] = companyId;
                context.Items["Company"] = company;
                
                _logger.LogDebug("Contexto de empresa configurado: {CompanyId} - {CompanyName}", 
                    companyId, company.Name);
            }
            else
            {
                // CompanyId não fornecido - isso é OK para algumas rotas
                _logger.LogDebug("CompanyId não fornecido no header ou query");
            }
        }
        
        await _next(context);
    }
    
    private static bool ShouldSkipValidation(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Ignorar rotas que não precisam de company context
        return path.Contains("/auth/") ||
               path.Contains("/health") ||
               path.Contains("/swagger") ||
               path.Contains("/_framework") ||
               path.Contains("/api-docs") ||
               path.Contains("/clients") ||  // Rotas de gerenciamento de clients
               path.Contains("/users/me");   // Perfil do usuário
    }
}

/// <summary>
/// Extensão para facilitar o uso do CompanyContextMiddleware
/// </summary>
public static class CompanyContextMiddlewareExtensions
{
    public static IApplicationBuilder UseCompanyContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CompanyContextMiddleware>();
    }
}

/// <summary>
/// Extensões para acessar o contexto da Company
/// </summary>
public static class HttpContextCompanyExtensions
{
    public static Guid? GetCompanyId(this HttpContext context)
    {
        return context.Items["CompanyId"] as Guid?;
    }
    
    public static Guid GetRequiredCompanyId(this HttpContext context)
    {
        var companyId = context.GetCompanyId();
        if (!companyId.HasValue)
        {
            throw new BusinessRuleException("CompanyRequired", "CompanyId é obrigatório. Forneça o header X-Company-Id ou query parameter companyId");
        }
        return companyId.Value;
    }
}
