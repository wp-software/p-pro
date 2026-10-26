# Partnership Manager
## Especificação Técnica - Arquitetura e Stack

**Versão:** 2.0  
**Data:** 21 de Janeiro de 2025  
**Status:** Aprovado para Desenvolvimento

---

## 1. Visão Geral da Arquitetura

### 1.1 Stack Tecnológico

| Camada | Tecnologia | Versão |
|--------|------------|--------|
| **Backend** | ASP.NET Core | 8.0 LTS |
| **Frontend** | Vue.js + Nuxt | 3.x |
| **Database** | MySQL | 8.0 |
| **Cache** | Redis | 7.x |
| **ORM** | Entity Framework Core | 8.x |
| **UI Components** | PrimeVue | 4.x |
| **CSS Framework** | Tailwind CSS | 3.x |

### 1.2 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND                                    │
│                         (Nuxt 3 + Vue 3)                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │   Pages     │  │ Components  │  │   Stores    │  │ Composables │   │
│  │  (Nuxt)     │  │ (PrimeVue)  │  │  (Pinia)    │  │   (Vue)     │   │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ HTTPS/REST
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              BACKEND                                     │
│                        (ASP.NET Core 8)                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                        API Layer                                 │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │   │
│  │  │Controllers│  │ Minimal  │  │  Filters │  │Middleware│        │   │
│  │  │          │  │   APIs   │  │          │  │          │        │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                     Application Layer                            │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │   │
│  │  │ Services │  │  DTOs    │  │Validators│  │ Mappers  │        │   │
│  │  │          │  │          │  │(Fluent)  │  │(Mapster) │        │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                       Domain Layer                               │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │   │
│  │  │ Entities │  │  Enums   │  │Exceptions│  │Interfaces│        │   │
│  │  │          │  │          │  │          │  │          │        │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    Infrastructure Layer                          │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │   │
│  │  │  EF Core │  │  Redis   │  │ Firebase │  │Background│        │   │
│  │  │DbContext │  │  Cache   │  │   Auth   │  │  Jobs    │        │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            DATA LAYER                                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │   MySQL     │  │    Redis    │  │  Firebase   │                     │
│  │  (Primary)  │  │   (Cache)   │  │  (Storage)  │                     │
│  └─────────────┘  └─────────────┘  └─────────────┘                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### 1.3 Serviços Externos

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SERVIÇOS EXTERNOS                                 │
│                                                                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │  Firebase   │  │  Clicksign  │  │   Resend    │                     │
│  │   (Auth)    │  │(Signatures) │  │   (Email)   │                     │
│  └─────────────┘  └─────────────┘  └─────────────┘                     │
└─────────────────────────────────────────────────────────────────────────┘
```

**Nota sobre Billing:** Nesta versão, o módulo de faturamento será apenas cadastral (sem integração com gateway de pagamento). A estrutura está preparada para futura integração.

---

## 2. Backend (.NET 8)

### 2.1 Estrutura de Projetos

```
PartnershipManager/
├── src/
│   ├── PartnershipManager.Api/              # API Layer
│   │   ├── Controllers/                     # REST Controllers
│   │   ├── Endpoints/                       # Minimal APIs (alternativa)
│   │   ├── Filters/                         # Action Filters
│   │   ├── Middleware/                      # Custom Middleware
│   │   ├── Extensions/                      # Service Extensions
│   │   └── Program.cs                       # Entry Point
│   │
│   ├── PartnershipManager.Application/      # Application Layer
│   │   ├── Common/
│   │   │   ├── Behaviors/                   # Pipeline Behaviors
│   │   │   ├── Interfaces/                  # Service Interfaces
│   │   │   ├── Mappings/                    # AutoMapper/Mapster Profiles
│   │   │   └── Models/                      # DTOs, ViewModels
│   │   ├── Features/                        # Vertical Slices por Feature
│   │   │   ├── Billing/
│   │   │   ├── Companies/
│   │   │   ├── Shareholders/
│   │   │   ├── Shares/
│   │   │   ├── Contracts/
│   │   │   ├── Vesting/
│   │   │   ├── Valuations/
│   │   │   └── Portal/
│   │   └── DependencyInjection.cs
│   │
│   ├── PartnershipManager.Domain/           # Domain Layer
│   │   ├── Entities/                        # Domain Entities
│   │   ├── Enums/                           # Enumerations
│   │   ├── Events/                          # Domain Events
│   │   ├── Exceptions/                      # Domain Exceptions
│   │   ├── ValueObjects/                    # Value Objects
│   │   └── Interfaces/                      # Repository Interfaces
│   │
│   └── PartnershipManager.Infrastructure/   # Infrastructure Layer
│       ├── Data/
│       │   ├── Configurations/              # EF Configurations
│       │   ├── Migrations/                  # EF Migrations
│       │   ├── Repositories/                # Repository Implementations
│       │   └── AppDbContext.cs              # DbContext
│       ├── Services/                        # External Services
│       │   ├── Firebase/                    # Auth Service
│       │   ├── Email/                       # Email Service (Resend)
│       │   └── Storage/                     # Storage Service
│       ├── Jobs/                            # Background Jobs
│       └── DependencyInjection.cs
│
├── tests/
│   ├── PartnershipManager.UnitTests/
│   ├── PartnershipManager.IntegrationTests/
│   └── PartnershipManager.ArchTests/
│
├── docker-compose.yml
├── Directory.Build.props
└── PartnershipManager.sln
```

### 2.2 Pacotes NuGet Principais

```xml
<!-- PartnershipManager.Api.csproj -->
<ItemGroup>
  <!-- Framework -->
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
  
  <!-- Autenticação Firebase -->
  <PackageReference Include="FirebaseAdmin" Version="2.4.*" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
  
  <!-- Observabilidade -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
  <PackageReference Include="Serilog.Sinks.MySQL" Version="5.0.*" />
</ItemGroup>

<!-- PartnershipManager.Application.csproj -->
<ItemGroup>
  <!-- CQRS (opcional) -->
  <PackageReference Include="MediatR" Version="12.2.*" />
  
  <!-- Validação -->
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.*" />
  
  <!-- Mapping -->
  <PackageReference Include="Mapster" Version="7.4.*" />
  <PackageReference Include="Mapster.DependencyInjection" Version="1.0.*" />
</ItemGroup>

<!-- PartnershipManager.Infrastructure.csproj -->
<ItemGroup>
  <!-- Entity Framework + MySQL -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
  <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />
  
  <!-- Cache -->
  <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.*" />
  
  <!-- Background Jobs -->
  <PackageReference Include="Hangfire.Core" Version="1.8.*" />
  <PackageReference Include="Hangfire.MySqlStorage" Version="2.0.*" />
  
  <!-- Firebase -->
  <PackageReference Include="FirebaseAdmin" Version="2.4.*" />
  
  <!-- Email -->
  <PackageReference Include="Resend.Net" Version="1.0.*" />
  
  <!-- Storage -->
  <PackageReference Include="AWSSDK.S3" Version="3.7.*" />
  <!-- ou Firebase Storage via REST API -->
</ItemGroup>
```

**Nota:** O pacote `Pomelo.EntityFrameworkCore.MySql` é a opção mais robusta e mantida para MySQL com EF Core 8.

### 2.3 Exemplo de Entidade

```csharp
// Domain/Entities/Shareholder.cs
namespace PartnershipManager.Domain.Entities;

public class Shareholder : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public ShareholderType Type { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public Address? Address { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public DateOnly? ExitDate { get; private set; }
    public ShareholderStatus Status { get; private set; }

    // Navigation Properties
    public Company Company { get; private set; } = null!;
    public User? User { get; private set; }
    public ICollection<Share> Shares { get; private set; } = new List<Share>();
    public ICollection<VestingGrant> VestingGrants { get; private set; } = new List<VestingGrant>();

    // Calculated Properties
    public decimal TotalShares => Shares.Where(s => s.Status == ShareStatus.Active).Sum(s => s.Quantity);
    
    // Factory Method
    public static Shareholder Create(
        Guid companyId,
        string name,
        string documentNumber,
        DocumentType documentType,
        ShareholderType type,
        string email,
        DateOnly entryDate)
    {
        return new Shareholder
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            DocumentNumber = documentNumber,
            DocumentType = documentType,
            Type = type,
            Email = email,
            EntryDate = entryDate,
            Status = ShareholderStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Domain Methods
    public void UpdateStatus(ShareholderStatus newStatus)
    {
        if (Status == ShareholderStatus.Exited)
            throw new DomainException("Cannot change status of exited shareholder");
        
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2.4 Exemplo de Controller

```csharp
// Api/Controllers/ShareholdersController.cs
namespace PartnershipManager.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId}/shareholders")]
[Authorize]
public class ShareholdersController : ControllerBase
{
    private readonly IShareholderService _shareholderService;
    private readonly ILogger<ShareholdersController> _logger;

    public ShareholdersController(
        IShareholderService shareholderService,
        ILogger<ShareholdersController> logger)
    {
        _shareholderService = shareholderService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os sócios de uma empresa
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShareholderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShareholderDto>>> GetAll(
        Guid companyId,
        [FromQuery] ShareholderFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _shareholderService.GetAllAsync(companyId, filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um sócio pelo ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShareholderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShareholderDetailDto>> GetById(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _shareholderService.GetByIdAsync(companyId, id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Cria um novo sócio
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShareholderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShareholderDto>> Create(
        Guid companyId,
        [FromBody] CreateShareholderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shareholderService.CreateAsync(companyId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { companyId, id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um sócio
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ShareholderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShareholderDto>> Update(
        Guid companyId,
        Guid id,
        [FromBody] UpdateShareholderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shareholderService.UpdateAsync(companyId, id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Remove um sócio (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var success = await _shareholderService.DeleteAsync(companyId, id, cancellationToken);
        return success ? NoContent() : NotFound();
    }
}
```

### 2.5 Configuração Firebase Authentication

```csharp
// Infrastructure/Services/Firebase/FirebaseAuthService.cs
namespace PartnershipManager.Infrastructure.Services.Firebase;

public class FirebaseAuthService : IAuthService
{
    private readonly FirebaseApp _firebaseApp;

    public FirebaseAuthService()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-credentials.json")
            });
        }
        else
        {
            _firebaseApp = FirebaseApp.DefaultInstance;
        }
    }

    public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
    {
        return await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
    }

    public async Task<UserRecord> GetUserAsync(string uid)
    {
        return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
    }

    public async Task<UserRecord> CreateUserAsync(string email, string password, string displayName)
    {
        var args = new UserRecordArgs
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
            EmailVerified = false
        };
        return await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
    }
}
```

```csharp
// Api/Middleware/FirebaseAuthMiddleware.cs
namespace PartnershipManager.Api.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthMiddleware> _logger;

    public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService, IUserService userService)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            
            try
            {
                var firebaseToken = await authService.VerifyTokenAsync(token);
                
                // Busca ou cria usuário no banco local
                var user = await userService.GetOrCreateFromFirebaseAsync(firebaseToken.Uid, firebaseToken.Claims);
                
                // Adiciona claims ao contexto
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("firebase_uid", firebaseToken.Uid),
                };
                
                // Adiciona roles
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }
                
                var identity = new ClaimsIdentity(claims, "Firebase");
                context.User = new ClaimsPrincipal(identity);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning("Invalid Firebase token: {Message}", ex.Message);
            }
        }

        await _next(context);
    }
}
```

```csharp
// Program.cs - Configuração
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var projectId = builder.Configuration["Firebase:ProjectId"];
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true
        };
    });

// Registra o serviço do Firebase
builder.Services.AddSingleton<IAuthService, FirebaseAuthService>();

// Middleware
app.UseAuthentication();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseAuthorization();
```

### 2.6 Exemplo de Configuração EF Core

```csharp
// Infrastructure/Data/Configurations/ShareholderConfiguration.cs
namespace PartnershipManager.Infrastructure.Data.Configurations;

public class ShareholderConfiguration : IEntityTypeConfiguration<Shareholder>
{
    public void Configure(EntityTypeBuilder<Shareholder> builder)
    {
        builder.ToTable("shareholders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("shareholder_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(200);
            address.Property(a => a.Number).HasColumnName("address_number").HasMaxLength(20);
            address.Property(a => a.Complement).HasColumnName("address_complement").HasMaxLength(100);
            address.Property(a => a.Neighborhood).HasColumnName("address_neighborhood").HasMaxLength(100);
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("address_state").HasMaxLength(2);
            address.Property(a => a.ZipCode).HasColumnName("address_zip_code").HasMaxLength(10);
            address.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(2);
        });

        builder.Property(x => x.EntryDate)
            .HasColumnName("entry_date")
            .IsRequired();

        builder.Property(x => x.ExitDate)
            .HasColumnName("exit_date");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne(x => x.Company)
            .WithMany(c => c.Shareholders)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.CompanyId)
            .HasDatabaseName("ix_shareholders_company_id");

        builder.HasIndex(x => new { x.CompanyId, x.DocumentNumber })
            .HasDatabaseName("ix_shareholders_company_document")
            .IsUnique();

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("ix_shareholders_type");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_shareholders_status");

        // Query Filter (multi-tenant)
        // builder.HasQueryFilter(x => x.CompanyId == _currentCompanyId);
    }
}
```

---

## 3. Frontend (Vue 3 + Nuxt 3)

### 3.1 Estrutura de Diretórios

```
partnership-manager-web/
├── app/
│   ├── components/              # Componentes Vue
│   │   ├── common/              # Componentes genéricos
│   │   │   ├── AppHeader.vue
│   │   │   ├── AppSidebar.vue
│   │   │   ├── AppBreadcrumb.vue
│   │   │   └── ...
│   │   ├── shareholders/        # Componentes de Shareholders
│   │   │   ├── ShareholderCard.vue
│   │   │   ├── ShareholderForm.vue
│   │   │   ├── ShareholderTable.vue
│   │   │   └── ...
│   │   ├── captable/            # Componentes de Cap Table
│   │   ├── contracts/           # Componentes de Contratos
│   │   ├── vesting/             # Componentes de Vesting
│   │   └── ...
│   │
│   ├── composables/             # Composables (hooks)
│   │   ├── useApi.ts            # HTTP client
│   │   ├── useAuth.ts           # Autenticação
│   │   ├── useCompany.ts        # Contexto da empresa
│   │   ├── useNotification.ts   # Toasts/Alerts
│   │   └── ...
│   │
│   ├── layouts/                 # Layouts Nuxt
│   │   ├── default.vue          # Layout principal (sidebar)
│   │   ├── auth.vue             # Layout de autenticação
│   │   └── portal.vue           # Layout do portal do investidor
│   │
│   ├── pages/                   # Páginas (auto-routing)
│   │   ├── index.vue            # Redirect
│   │   ├── login.vue            # Login
│   │   ├── register.vue         # Registro
│   │   ├── dashboard.vue        # Dashboard
│   │   ├── captable/
│   │   │   └── index.vue        # Cap Table
│   │   ├── shareholders/
│   │   │   ├── index.vue        # Lista de sócios
│   │   │   └── [id].vue         # Detalhe do sócio
│   │   ├── contracts/
│   │   │   ├── index.vue        # Lista de contratos
│   │   │   └── [id].vue         # Detalhe do contrato
│   │   ├── vesting/
│   │   │   └── index.vue        # Vesting & Metas
│   │   ├── valuation/
│   │   │   └── index.vue        # Valuation
│   │   ├── financial/
│   │   │   └── index.vue        # Dados Financeiros
│   │   ├── portal/              # Portal do Investidor
│   │   │   ├── index.vue
│   │   │   └── ...
│   │   └── settings/
│   │       ├── company.vue
│   │       ├── users.vue
│   │       └── billing.vue
│   │
│   └── middleware/              # Middleware Nuxt
│       ├── auth.ts              # Verificação de auth
│       └── company.ts           # Verificação de empresa
│
├── stores/                      # Pinia Stores
│   ├── auth.ts                  # Estado de autenticação
│   ├── company.ts               # Empresa atual
│   ├── shareholders.ts          # Cache de sócios
│   └── ui.ts                    # Estado de UI (sidebar, theme)
│
├── types/                       # TypeScript Types
│   ├── api.ts                   # Tipos da API
│   ├── entities.ts              # Entidades
│   └── index.ts                 # Re-exports
│
├── utils/                       # Utilitários
│   ├── formatters.ts            # Formatação (moeda, data, etc)
│   ├── validators.ts            # Validações
│   └── constants.ts             # Constantes
│
├── assets/                      # Assets estáticos
│   ├── css/
│   │   └── main.css             # Estilos globais + Tailwind
│   └── images/
│
├── public/                      # Arquivos públicos
│   └── favicon.ico
│
├── nuxt.config.ts               # Configuração Nuxt
├── tailwind.config.ts           # Configuração Tailwind
├── tsconfig.json                # Configuração TypeScript
└── package.json
```

### 3.2 Dependências (package.json)

```json
{
  "name": "partnership-manager-web",
  "version": "1.0.0",
  "private": true,
  "scripts": {
    "dev": "nuxt dev",
    "build": "nuxt build",
    "preview": "nuxt preview",
    "generate": "nuxt generate",
    "lint": "eslint .",
    "lint:fix": "eslint . --fix",
    "typecheck": "nuxt typecheck"
  },
  "dependencies": {
    "firebase": "^10.8.0",
    "@pinia/nuxt": "^0.5.1",
    "@primevue/themes": "^4.0.0",
    "@vee-validate/nuxt": "^4.12.0",
    "@vueuse/core": "^10.7.0",
    "@vueuse/nuxt": "^10.7.0",
    "chart.js": "^4.4.0",
    "dayjs": "^1.11.10",
    "nuxt": "^3.9.0",
    "pinia": "^2.1.7",
    "primevue": "^4.0.0",
    "primeicons": "^7.0.0",
    "vue": "^3.4.0",
    "vue-router": "^4.2.0",
    "zod": "^3.22.0"
  },
  "devDependencies": {
    "@nuxt/devtools": "^1.0.0",
    "@nuxt/eslint": "^0.2.0",
    "@nuxtjs/tailwindcss": "^6.10.0",
    "@types/node": "^20.10.0",
    "eslint": "^8.56.0",
    "typescript": "^5.3.0"
  }
}
```

### 3.3 Configuração Nuxt

```typescript
// nuxt.config.ts
export default defineNuxtConfig({
  devtools: { enabled: true },
  
  modules: [
    '@nuxtjs/tailwindcss',
    '@pinia/nuxt',
    '@vueuse/nuxt',
    '@vee-validate/nuxt',
    'nuxt-primevue',
  ],

  css: [
    'primeicons/primeicons.css',
    '~/assets/css/main.css',
  ],

  primevue: {
    options: {
      theme: {
        preset: 'Aura',
        options: {
          darkModeSelector: '.dark',
          cssLayer: {
            name: 'primevue',
            order: 'tailwind-base, primevue, tailwind-utilities',
          },
        },
      },
    },
    components: {
      include: '*',
    },
  },

  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:5000/api',
      clerkPublishableKey: process.env.NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY,
    },
  },

  typescript: {
    strict: true,
    typeCheck: true,
  },

  app: {
    head: {
      title: 'Partnership Manager',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Sistema de Gestão Societária' },
      ],
      link: [
        { rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' },
      ],
    },
  },
})
```

### 3.4 Exemplo de Página

```vue
<!-- pages/shareholders/index.vue -->
<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Shareholder, ShareholderFilter } from '~/types'

definePageMeta({
  layout: 'default',
  middleware: ['auth', 'company'],
})

const { currentCompany } = useCompany()
const { $api } = useNuxtApp()

// State
const loading = ref(false)
const shareholders = ref<Shareholder[]>([])
const totalRecords = ref(0)
const filters = ref<ShareholderFilter>({
  page: 1,
  pageSize: 10,
  type: null,
  status: null,
  search: '',
})

// Fetch data
const fetchShareholders = async () => {
  loading.value = true
  try {
    const response = await $api.shareholders.getAll(currentCompany.value.id, filters.value)
    shareholders.value = response.items
    totalRecords.value = response.totalCount
  } finally {
    loading.value = false
  }
}

// Initial fetch
await fetchShareholders()

// Watchers
watch(filters, fetchShareholders, { deep: true })

// Computed
const stats = computed(() => ({
  total: totalRecords.value,
  founders: shareholders.value.filter(s => s.type === 'founder').length,
  investors: shareholders.value.filter(s => s.type === 'investor').length,
  employees: shareholders.value.filter(s => s.type === 'employee').length,
}))

// Actions
const showCreateDialog = ref(false)
const editingShareholder = ref<Shareholder | null>(null)

const handleCreate = async (data: CreateShareholderRequest) => {
  await $api.shareholders.create(currentCompany.value.id, data)
  showCreateDialog.value = false
  await fetchShareholders()
}

const handleEdit = (shareholder: Shareholder) => {
  editingShareholder.value = shareholder
}

const handleDelete = async (shareholder: Shareholder) => {
  const confirmed = await confirm(`Deseja remover ${shareholder.name}?`)
  if (confirmed) {
    await $api.shareholders.delete(currentCompany.value.id, shareholder.id)
    await fetchShareholders()
  }
}
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Sócios</h1>
        <p class="text-gray-500">Gerencie os sócios da empresa</p>
      </div>
      <Button 
        label="Novo Sócio" 
        icon="pi pi-plus" 
        @click="showCreateDialog = true" 
      />
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-4 gap-4 mb-6">
      <Card class="bg-white">
        <template #content>
          <div class="flex items-center gap-3">
            <div class="p-3 rounded-lg bg-blue-100">
              <i class="pi pi-users text-blue-600 text-xl"></i>
            </div>
            <div>
              <p class="text-2xl font-bold">{{ stats.total }}</p>
              <p class="text-gray-500 text-sm">Total de Sócios</p>
            </div>
          </div>
        </template>
      </Card>

      <Card class="bg-white">
        <template #content>
          <div class="flex items-center gap-3">
            <div class="p-3 rounded-lg bg-indigo-100">
              <i class="pi pi-star text-indigo-600 text-xl"></i>
            </div>
            <div>
              <p class="text-2xl font-bold">{{ stats.founders }}</p>
              <p class="text-gray-500 text-sm">Fundadores</p>
            </div>
          </div>
        </template>
      </Card>

      <Card class="bg-white">
        <template #content>
          <div class="flex items-center gap-3">
            <div class="p-3 rounded-lg bg-purple-100">
              <i class="pi pi-briefcase text-purple-600 text-xl"></i>
            </div>
            <div>
              <p class="text-2xl font-bold">{{ stats.investors }}</p>
              <p class="text-gray-500 text-sm">Investidores</p>
            </div>
          </div>
        </template>
      </Card>

      <Card class="bg-white">
        <template #content>
          <div class="flex items-center gap-3">
            <div class="p-3 rounded-lg bg-green-100">
              <i class="pi pi-user text-green-600 text-xl"></i>
            </div>
            <div>
              <p class="text-2xl font-bold">{{ stats.employees }}</p>
              <p class="text-gray-500 text-sm">Funcionários</p>
            </div>
          </div>
        </template>
      </Card>
    </div>

    <!-- Filters -->
    <Card class="mb-6">
      <template #content>
        <div class="flex items-center gap-4">
          <IconField iconPosition="left" class="flex-1">
            <InputIcon class="pi pi-search" />
            <InputText 
              v-model="filters.search" 
              placeholder="Buscar por nome ou documento..." 
              class="w-full"
            />
          </IconField>

          <Dropdown
            v-model="filters.type"
            :options="shareholderTypes"
            optionLabel="label"
            optionValue="value"
            placeholder="Tipo"
            class="w-48"
            showClear
          />

          <Dropdown
            v-model="filters.status"
            :options="shareholderStatuses"
            optionLabel="label"
            optionValue="value"
            placeholder="Status"
            class="w-48"
            showClear
          />
        </div>
      </template>
    </Card>

    <!-- Table -->
    <Card>
      <template #content>
        <DataTable
          :value="shareholders"
          :loading="loading"
          :paginator="true"
          :rows="filters.pageSize"
          :totalRecords="totalRecords"
          :lazy="true"
          @page="(e) => filters.page = e.page + 1"
          stripedRows
          responsiveLayout="scroll"
        >
          <Column field="name" header="Sócio">
            <template #body="{ data }">
              <div class="flex items-center gap-3">
                <Avatar :label="data.name.charAt(0)" shape="circle" />
                <div>
                  <p class="font-medium">{{ data.name }}</p>
                  <p class="text-sm text-gray-500">{{ data.email }}</p>
                </div>
              </div>
            </template>
          </Column>

          <Column field="type" header="Tipo">
            <template #body="{ data }">
              <Tag :value="data.type" :severity="getTypeSeverity(data.type)" />
            </template>
          </Column>

          <Column field="totalShares" header="Ações">
            <template #body="{ data }">
              {{ formatNumber(data.totalShares) }}
            </template>
          </Column>

          <Column field="ownership" header="Participação">
            <template #body="{ data }">
              <div class="flex items-center gap-2">
                <ProgressBar :value="data.ownershipPercentage" class="w-24 h-2" />
                <span>{{ formatPercent(data.ownershipPercentage) }}</span>
              </div>
            </template>
          </Column>

          <Column field="totalValue" header="Valor">
            <template #body="{ data }">
              <span class="text-green-600 font-medium">
                {{ formatCurrency(data.totalValue) }}
              </span>
            </template>
          </Column>

          <Column field="status" header="Status">
            <template #body="{ data }">
              <Tag :value="data.status" :severity="getStatusSeverity(data.status)" />
            </template>
          </Column>

          <Column header="Ações" :exportable="false" style="width: 100px">
            <template #body="{ data }">
              <div class="flex gap-2">
                <Button 
                  icon="pi pi-eye" 
                  text 
                  rounded 
                  @click="navigateTo(`/shareholders/${data.id}`)" 
                />
                <Button 
                  icon="pi pi-pencil" 
                  text 
                  rounded 
                  @click="handleEdit(data)" 
                />
                <Button 
                  icon="pi pi-trash" 
                  text 
                  rounded 
                  severity="danger" 
                  @click="handleDelete(data)" 
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </template>
    </Card>

    <!-- Create Dialog -->
    <ShareholderFormDialog
      v-model:visible="showCreateDialog"
      @submit="handleCreate"
    />

    <!-- Edit Dialog -->
    <ShareholderFormDialog
      v-if="editingShareholder"
      v-model:visible="!!editingShareholder"
      :shareholder="editingShareholder"
      @submit="handleUpdate"
      @hide="editingShareholder = null"
    />
  </div>
</template>
```

### 3.5 Composable de Autenticação (Firebase)

```typescript
// composables/useAuth.ts
import { 
  getAuth, 
  signInWithEmailAndPassword, 
  createUserWithEmailAndPassword,
  signInWithPopup,
  GoogleAuthProvider,
  signOut,
  onAuthStateChanged,
  type User
} from 'firebase/auth'

export function useAuth() {
  const user = useState<User | null>('firebase_user', () => null)
  const loading = useState('auth_loading', () => true)
  const error = useState<string | null>('auth_error', () => null)

  const auth = getAuth()

  // Observar mudanças de autenticação
  onAuthStateChanged(auth, (firebaseUser) => {
    user.value = firebaseUser
    loading.value = false
  })

  // Login com email/senha
  const login = async (email: string, password: string) => {
    try {
      error.value = null
      loading.value = true
      const result = await signInWithEmailAndPassword(auth, email, password)
      user.value = result.user
      return result.user
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  // Login com Google
  const loginWithGoogle = async () => {
    try {
      error.value = null
      loading.value = true
      const provider = new GoogleAuthProvider()
      const result = await signInWithPopup(auth, provider)
      user.value = result.user
      return result.user
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  // Registrar novo usuário
  const register = async (email: string, password: string) => {
    try {
      error.value = null
      loading.value = true
      const result = await createUserWithEmailAndPassword(auth, email, password)
      user.value = result.user
      return result.user
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  // Logout
  const logout = async () => {
    try {
      await signOut(auth)
      user.value = null
      navigateTo('/login')
    } catch (e: any) {
      error.value = e.message
    }
  }

  // Obter token para API
  const getToken = async (): Promise<string | null> => {
    if (!user.value) return null
    return await user.value.getIdToken()
  }

  // Verificar se está autenticado
  const isAuthenticated = computed(() => !!user.value)

  return {
    user: readonly(user),
    loading: readonly(loading),
    error: readonly(error),
    isAuthenticated,
    login,
    loginWithGoogle,
    register,
    logout,
    getToken,
  }
}
```

```typescript
// plugins/firebase.client.ts
import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'

export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig()

  const firebaseConfig = {
    apiKey: config.public.firebaseApiKey,
    authDomain: config.public.firebaseAuthDomain,
    projectId: config.public.firebaseProjectId,
    storageBucket: config.public.firebaseStorageBucket,
    messagingSenderId: config.public.firebaseMessagingSenderId,
    appId: config.public.firebaseAppId,
  }

  const app = initializeApp(firebaseConfig)
  const auth = getAuth(app)

  return {
    provide: {
      firebaseApp: app,
      firebaseAuth: auth,
    },
  }
})
```

```typescript
// middleware/auth.ts
export default defineNuxtRouteMiddleware(async (to) => {
  const { isAuthenticated, loading } = useAuth()

  // Aguarda carregar estado de auth
  if (loading.value) {
    await new Promise(resolve => setTimeout(resolve, 100))
  }

  // Redireciona para login se não autenticado
  if (!isAuthenticated.value && to.path !== '/login') {
    return navigateTo('/login')
  }
})
```

### 3.6 Exemplo de Composable (API Client)

```typescript
// composables/useApi.ts
import type { UseFetchOptions } from 'nuxt/app'

export function useApi() {
  const config = useRuntimeConfig()
  const { getToken } = useAuth()

  const apiFetch = async <T>(
    endpoint: string,
    options: UseFetchOptions<T> = {}
  ) => {
    const token = await getToken()

    return useFetch<T>(endpoint, {
      baseURL: config.public.apiBaseUrl,
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      ...options,
    })
  }

  // Shareholders
  const shareholders = {
    getAll: (companyId: string, filters: ShareholderFilter) =>
      apiFetch<PagedResult<Shareholder>>(
        `/companies/${companyId}/shareholders`,
        { query: filters }
      ),

    getById: (companyId: string, id: string) =>
      apiFetch<ShareholderDetail>(
        `/companies/${companyId}/shareholders/${id}`
      ),

    create: (companyId: string, data: CreateShareholderRequest) =>
      apiFetch<Shareholder>(
        `/companies/${companyId}/shareholders`,
        { method: 'POST', body: data }
      ),

    update: (companyId: string, id: string, data: UpdateShareholderRequest) =>
      apiFetch<Shareholder>(
        `/companies/${companyId}/shareholders/${id}`,
        { method: 'PUT', body: data }
      ),

    delete: (companyId: string, id: string) =>
      apiFetch<void>(
        `/companies/${companyId}/shareholders/${id}`,
        { method: 'DELETE' }
      ),
  }

  // Shares
  const shares = {
    // ...similar pattern
  }

  // Contracts
  const contracts = {
    // ...similar pattern
  }

  return {
    shareholders,
    shares,
    contracts,
    // ...other resources
  }
}
```

---

## 4. Ambiente de Desenvolvimento

### 4.1 Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  # MySQL Database
  db:
    image: mysql:8.0
    container_name: pm-mysql
    environment:
      MYSQL_ROOT_PASSWORD: root123
      MYSQL_DATABASE: partnership_manager
      MYSQL_USER: partnership
      MYSQL_PASSWORD: partnership123
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
    command: --default-authentication-plugin=mysql_native_password
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "partnership", "-ppartnership123"]
      interval: 5s
      timeout: 5s
      retries: 5

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: pm-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  # MinIO (S3-compatible storage) - opcional, pode usar Firebase Storage
  minio:
    image: minio/minio:latest
    container_name: pm-minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin123
    ports:
      - "9000:9000"
      - "9001:9001"
    volumes:
      - minio_data:/data

  # Backend API (.NET)
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: pm-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=partnership_manager;User=partnership;Password=partnership123
      - Redis__ConnectionString=redis:6379
      - Firebase__ProjectId=${FIREBASE_PROJECT_ID}
    ports:
      - "5000:8080"
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_started

  # Frontend (Nuxt)
  web:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: pm-web
    environment:
      - NUXT_PUBLIC_API_BASE_URL=http://localhost:5000/api
    ports:
      - "3000:3000"
    depends_on:
      - api

volumes:
  mysql_data:
  redis_data:
  minio_data:
```

### 4.2 Dockerfile Backend

```dockerfile
# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/PartnershipManager.Api/PartnershipManager.Api.csproj", "PartnershipManager.Api/"]
COPY ["src/PartnershipManager.Application/PartnershipManager.Application.csproj", "PartnershipManager.Application/"]
COPY ["src/PartnershipManager.Domain/PartnershipManager.Domain.csproj", "PartnershipManager.Domain/"]
COPY ["src/PartnershipManager.Infrastructure/PartnershipManager.Infrastructure.csproj", "PartnershipManager.Infrastructure/"]
RUN dotnet restore "PartnershipManager.Api/PartnershipManager.Api.csproj"

# Copy everything else and build
COPY src/ .
RUN dotnet build "PartnershipManager.Api/PartnershipManager.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "PartnershipManager.Api/PartnershipManager.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PartnershipManager.Api.dll"]
```

### 4.3 Dockerfile Frontend

```dockerfile
# frontend/Dockerfile
FROM node:24.22.1-alpine AS build
WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# Production image
FROM node:24.22.1-alpine AS production
WORKDIR /app

COPY --from=build /app/.output ./.output
COPY --from=build /app/package*.json ./

EXPOSE 3000
CMD ["node", ".output/server/index.mjs"]
```

---

## 5. CI/CD (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  # Backend Tests
  backend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./backend

    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: root
          MYSQL_DATABASE: test_db
          MYSQL_USER: test
          MYSQL_PASSWORD: test
        ports:
          - 3306:3306
        options: >-
          --health-cmd "mysqladmin ping -h localhost"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v3

  # Frontend Tests
  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./frontend

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24.22.1'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Lint
        run: npm run lint

      - name: Type check
        run: npm run typecheck

      - name: Build
        run: npm run build

  # Deploy (only on main)
  deploy:
    needs: [backend, frontend]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
      - uses: actions/checkout@v4

      - name: Deploy to production
        run: echo "Deploy to production environment"
        # Add your deployment steps here
```

---

## 6. Comandos Úteis

### Backend (.NET)

```bash
# Criar solution
dotnet new sln -n PartnershipManager

# Criar projetos
dotnet new webapi -n PartnershipManager.Api -o src/PartnershipManager.Api
dotnet new classlib -n PartnershipManager.Application -o src/PartnershipManager.Application
dotnet new classlib -n PartnershipManager.Domain -o src/PartnershipManager.Domain
dotnet new classlib -n PartnershipManager.Infrastructure -o src/PartnershipManager.Infrastructure

# Adicionar projetos à solution
dotnet sln add src/PartnershipManager.Api
dotnet sln add src/PartnershipManager.Application
dotnet sln add src/PartnershipManager.Domain
dotnet sln add src/PartnershipManager.Infrastructure

# Adicionar referências entre projetos
cd src/PartnershipManager.Api
dotnet add reference ../PartnershipManager.Application
dotnet add reference ../PartnershipManager.Infrastructure

cd ../PartnershipManager.Application
dotnet add reference ../PartnershipManager.Domain

cd ../PartnershipManager.Infrastructure
dotnet add reference ../PartnershipManager.Application
dotnet add reference ../PartnershipManager.Domain

# Migrations
cd src/PartnershipManager.Api
dotnet ef migrations add InitialCreate --project ../PartnershipManager.Infrastructure
dotnet ef database update

# Run
dotnet run --project src/PartnershipManager.Api
```

### Frontend (Nuxt/Vue)

```bash
# Criar projeto
npx nuxi@latest init partnership-manager-web
cd partnership-manager-web

# Instalar dependências
npm install primevue primeicons @primevue/themes
npm install pinia @pinia/nuxt
npm install @vueuse/core @vueuse/nuxt
npm install @vee-validate/nuxt zod
npm install dayjs

# Dev dependencies
npm install -D @nuxtjs/tailwindcss
npm install -D @nuxt/eslint eslint

# Rodar em desenvolvimento
npm run dev

# Build para produção
npm run build

# Preview da build
npm run preview
```

---

*Documento gerado em 21/01/2025 - Partnership Manager v2.0*
