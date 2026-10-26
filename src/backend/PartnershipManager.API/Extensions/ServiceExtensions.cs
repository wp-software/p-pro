using System.Text;
using Dapper;
using FluentValidation;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PartnershipManager.Application.Interfaces;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Billing;
using PartnershipManager.Domain.Interfaces.Services;
using PartnershipManager.Infrastructure.Caching;
using PartnershipManager.Infrastructure.Jobs;
using PartnershipManager.Infrastructure.Persistence;
using PartnershipManager.Infrastructure.Persistence.Repositories;
using PartnershipManager.Infrastructure.Persistence.TypeHandlers;
using PartnershipManager.Infrastructure.Repositories.Billing;
using PartnershipManager.Application.Services;
using PartnershipManager.Infrastructure.Repositories;
using PartnershipManager.Infrastructure.Services;
using PartnershipManager.Infrastructure.Services.CustomFormulas;
using PartnershipManager.Infrastructure.Services.Documents;
using PartnershipManager.Infrastructure.Services.Financial;
using PartnershipManager.Infrastructure.Services.Valuation;

namespace PartnershipManager.API.Extensions;

/// <summary>
/// Extensões para configuração de serviços da Application
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(PartnershipManager.Application.Features.Auth.Validators.LoginValidator).Assembly));
        
        // FluentValidation - registra todos os validators do assembly
        services.AddValidatorsFromAssemblyContaining<PartnershipManager.Application.Features.Auth.Validators.LoginValidator>();
        
        return services;
    }
}

/// <summary>
/// Extensões para configuração de serviços de infraestrutura
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - Dapper
        var connectionString = configuration.GetConnectionString("MySQL") 
            ?? throw new InvalidOperationException("MySQL connection string not found");
        
        services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
        services.AddScoped<DapperContext>();
        
        // Configure Dapper TypeHandlers for enums
        SqlMapper.AddTypeHandler(new EnumMemberTypeHandler<ContractTemplateType>());
        SqlMapper.AddTypeHandler(new EnumMemberTypeHandler<ContractStatus>());
        SqlMapper.AddTypeHandler(new EnumMemberTypeHandler<ClauseType>());
        SqlMapper.AddTypeHandler(new EnumMemberTypeHandler<SignatureStatus>());
        
        // Configure Dapper TypeHandler for JSON Lists
        SqlMapper.AddTypeHandler(new JsonListTypeHandler());
        
        // Repositories
        services.AddScoped<ICoreClientRepository, CoreClientRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IShareholderRepository, ShareholderRepository>();
        services.AddScoped<IShareClassRepository, ShareClassRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Billing Repositories
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        
        // Auth Service
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, GoogleSmtpEmailService>();
        services.AddScoped<IShareholderService, ShareholderService>();
        services.AddScoped<IShareClassService, ShareClassService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IRoundSimulatorService, RoundSimulatorService>();
        
        // Repositories - Shares
        services.AddScoped<IShareRepository, ShareRepository>();
        services.AddScoped<IShareTransactionRepository, ShareTransactionRepository>();
        
        // Contracts Module - Repositories
        services.AddScoped<IContractTemplateRepository, ContractTemplateRepository>();
        services.AddScoped<IClauseRepository, ClauseRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IContractVersionRepository, ContractVersionRepository>();

        // Contracts Module - Services
        services.AddScoped<IContractTemplateService, ContractTemplateService>();
        services.AddScoped<IClauseService, ClauseService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IContractGenerationService, ContractGenerationService>();
        services.AddScoped<IContractStorageService, ContractStorageService>();
        services.AddScoped<IContractVersionService, ContractVersionService>();
        services.AddScoped<IClickSignWebhookService, ClickSignWebhookService>();
        services.AddHttpClient<IClickSignService, ClickSignService>();

        // Vesting Module - Repositories
        services.AddScoped<IVestingPlanRepository, VestingPlanRepository>();
        services.AddScoped<IVestingGrantRepository, VestingGrantRepository>();
        services.AddScoped<IVestingScheduleRepository, VestingScheduleRepository>();
        services.AddScoped<IVestingMilestoneRepository, VestingMilestoneRepository>();
        services.AddScoped<IVestingTransactionRepository, VestingTransactionRepository>();

        // Grant Milestones Module - Repositories
        services.AddScoped<IMilestoneTemplateRepository, MilestoneTemplateRepository>();
        services.AddScoped<IGrantMilestoneRepository, GrantMilestoneRepository>();
        services.AddScoped<IMilestoneProgressRepository, MilestoneProgressRepository>();
        services.AddScoped<IVestingAccelerationRepository, VestingAccelerationRepository>();

        // Vesting Module - Services
        services.AddScoped<IVestingPlanService, VestingPlanService>();
        services.AddScoped<IVestingGrantService, VestingGrantService>();
        services.AddScoped<IVestingMilestoneService, VestingMilestoneService>();

        // Grant Milestones Module - Services
        services.AddScoped<IMilestoneTemplateService, MilestoneTemplateService>();
        services.AddScoped<IMilestoneTrackingService, MilestoneTrackingService>();
        services.AddScoped<IVestingAccelerationEngine, VestingAccelerationEngine>();
        
        // Fase 5 - Valuation Module - Repositories
        services.AddScoped<IValuationRepository, ValuationRepository>();
        services.AddScoped<IValuationMethodRepository, ValuationMethodRepository>();
        services.AddScoped<IValuationDocumentRepository, ValuationDocumentRepository>();

        // Fase 5 - Financial Module - Repositories
        services.AddScoped<IFinancialPeriodRepository, FinancialPeriodRepository>();
        services.AddScoped<IFinancialMetricRepository, FinancialMetricRepository>();
        services.AddScoped<IFinancialConfigRepository, FinancialConfigRepository>();

        // Fase 5 - Document Module - Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Fase 5 - Custom Formula Module - Repositories
        services.AddScoped<ICustomFormulaRepository, CustomFormulaRepository>();
        services.AddScoped<IFormulaVersionRepository, FormulaVersionRepository>();
        services.AddScoped<IFormulaExecutionRepository, FormulaExecutionRepository>();

        // Fase 5 - Services
        services.AddScoped<ICustomFormulaEngine, CustomFormulaEngine>();
        services.AddScoped<IValuationCalculationEngine, ValuationCalculationEngine>();
        services.AddScoped<IValuationService, ValuationService>();
        services.AddScoped<IFinancialPeriodService, FinancialPeriodService>();
        services.AddScoped<IFinancialMetricConfigService, FinancialMetricConfigService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ICustomFormulaService, CustomFormulaService>();

        // PDF Generator Service  
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        
        // Cache - Redis ou Memory
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "pm:";
            });
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
        
        // Hangfire - Temporarily disabled for testing
        /*
        var hangfireConnection = configuration.GetConnectionString("Hangfire") ?? connectionString;
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(new MySqlStorage(
                    hangfireConnection,
                    new MySqlStorageOptions
                    {
                        TablesPrefix = "hangfire_",
                        PrepareSchemaIfNecessary = true,
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5)
                    }));
        });
        
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "default", "emails", "reports" };
        });
        */
        
        services.AddScoped<IBackgroundJobs, BackgroundJobs>();

        // Phase 6 - Communication Module
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<ICommunicationService, CommunicationService>();

        // Phase 6 - Data Room Module
        services.AddScoped<IDataRoomRepository, DataRoomRepository>();
        services.AddScoped<IDataRoomService, DataRoomService>();

        // Phase 6 - Notification Module
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        // Phase 6 - Workflow Module
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowService, WorkflowService>();

        // Phase 6 - Investor Portal
        services.AddScoped<IInvestorPortalService, InvestorPortalService>();
        
        return services;
    }
}

/// <summary>
/// Extensões para configuração de serviços da API
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Controllers
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        
        // CORS
        var corsOrigins = configuration["Cors:Origins"]?
            .Split(',')
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrEmpty(o))
            .ToArray() ?? new[] { "http://localhost:3000" };
        var corsOriginSet = new HashSet<string>(corsOrigins, StringComparer.OrdinalIgnoreCase);
        
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin))
                            return false;
                        
                        // Allow configured origins
                        if (corsOriginSet.Contains(origin))
                            return true;
                        
                        if (environment.IsDevelopment() &&
                            (origin.EndsWith(".app.github.dev", StringComparison.OrdinalIgnoreCase) ||
                             origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) ||
                             origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase)))
                            return true;
                        
                        return false;
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("*");
            });
        });
        
        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not found");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "PartnershipManager";
        var jwtAudience = configuration["Jwt:Audience"] ?? "PartnershipManagerApp";
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        
        services.AddAuthorization();
        
        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "P-Pro | WP Manager API",
                Version = "v1",
                Description = "API para gestão societária",
                Contact = new OpenApiContact
                {
                    Name = "Equipe WP Manager",
                    Email = "suporte@wpmanager.com.br"
                }
            });
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Digite o token JWT"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        // Health Checks
        services.AddHealthChecks()
            .AddMySql(
                configuration.GetConnectionString("MySQL")!,
                name: "mysql",
                tags: new[] { "db", "mysql" });
            
            // Redis health check disabled during development if not configured
            // .AddRedis(
            //     configuration.GetConnectionString("Redis") ?? "localhost:6379",
            //     name: "redis",
            //     tags: new[] { "cache", "redis" });
            
            // .AddHangfire(options =>
            // {
            //     options.MinimumAvailableServers = 1;
            // }, name: "hangfire", tags: new[] { "jobs", "hangfire" });
        
        // Current User Service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        return services;
    }
}

/// <summary>
/// Serviço para obter informações do usuário atual
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }
    
    public Guid? ClientId
    {
        get
        {
            var clientId = _httpContextAccessor.HttpContext?.Items["ClientId"];
            return clientId is Guid id ? id : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var companyId = _httpContextAccessor.HttpContext?.User
                .FindFirst("companyId")?.Value;
            return Guid.TryParse(companyId, out var id) ? id : null;
        }
    }
    
    public string? Email => _httpContextAccessor.HttpContext?.User
        .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    
    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User
        .FindAll(System.Security.Claims.ClaimTypes.Role)
        .Select(c => c.Value) ?? Enumerable.Empty<string>();
    
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User
        .Identity?.IsAuthenticated ?? false;
}
