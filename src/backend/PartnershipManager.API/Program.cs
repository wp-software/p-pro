using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using PartnershipManager.API.Extensions;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Infrastructure.Jobs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// SERILOG
// =====================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// =====================================================
// SERVICES
// =====================================================
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// =====================================================
// MIDDLEWARE PIPELINE
// =====================================================

app.UseForwardedHeaders();

// Exception handling (primeiro da pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Expor documentação interativa apenas em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "P-Pro | WP Manager API v1");
        c.RoutePrefix = "swagger";
    });
}

// HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCors("DefaultPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionIdleTimeoutMiddleware>();

// Client and Company Context (após autenticação)
app.UseClientContext();
app.UseCompanyContext();

// Public readiness endpoint for platform probes; do not disclose individual checks.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(report.Status.ToString());
    }
});
app.MapHealthChecks("/health/details", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

// Hangfire Dashboard - Temporarily disabled
// app.MapHangfireDashboard("/hangfire", new DashboardOptions
// {
//     Authorization = new[] { new HangfireAuthorizationFilter() }
// });

// Map Controllers
app.MapControllers();

// Configure recurring jobs
// HangfireJobsConfiguration.ConfigureRecurringJobs();

// =====================================================
// ROOT ENDPOINT
// =====================================================
app.MapGet("/", () => Results.Ok(new
{
    Name = "P-Pro | WP Manager API",
    Status = "Running"
}));

// =====================================================
// RUN
// =====================================================
Log.Information("P-Pro | WP Manager API starting...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Filtro de autorização para Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Em desenvolvimento, permitir acesso local
        if (httpContext.Request.Host.Host == "localhost")
            return true;
            
        // Em produção, verificar se usuário está autenticado e é admin
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
