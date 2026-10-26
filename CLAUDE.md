# Partnership Manager — Claude Code Guide

Sistema de Gestão Societária para gerenciar Cap Table, Vesting, Contratos, Valuations e comunicação com investidores.

---

## Project Structure

```
partnership-manager/
├── .claude/
│   ├── hooks/
│   │   └── session-start.sh     # Auto-installs deps on web sessions
│   └── settings.json
├── src/
│   ├── backend/                 # .NET 9 API (C#)
│   │   ├── PartnershipManager.API/           # Controllers, Middlewares, Program.cs
│   │   ├── PartnershipManager.Application/   # DTOs, Validators, Services
│   │   ├── PartnershipManager.Domain/        # Entities, Enums, Interfaces
│   │   ├── PartnershipManager.Infrastructure/ # Repositories, Dapper, Cache
│   │   └── PartnershipManager.Tests/         # xUnit tests (Unit, Integration, E2E)
│   └── frontend/                # React 18 + TypeScript (Vite)
│       └── src/
│           ├── components/      # Reusable UI components
│           ├── pages/           # Route-level page components
│           ├── stores/          # Zustand global state
│           ├── services/        # Axios API calls
│           ├── hooks/           # Custom React hooks
│           ├── constants/       # App-wide constants and messages
│           ├── types/           # TypeScript type definitions
│           └── utils/           # Utility functions
├── docker/                      # MySQL and Redis config
├── docker-compose.yml
└── Dockerfile
```

---

## Tech Stack

### Backend (.NET 9 / C#)
- **Dapper** — micro ORM for SQL queries
- **MySQL 8.0** — primary relational database
- **Redis** — distributed cache
- **Hangfire** — background job processing
- **JWT** — authentication
- **FluentValidation** — request validation
- **Serilog** — structured logging
- **Swagger** — API docs at `/swagger`

### Frontend (React 18 / TypeScript)
- **Vite** — dev server and build tool
- **Tailwind CSS** — utility-first styling
- **React Query (@tanstack/react-query)** — async state / data fetching
- **Zustand** — lightweight global state
- **React Hook Form + Zod** — form handling and schema validation
- **Axios** — HTTP client
- **Recharts** — charts and data visualization
- **Lucide React** — icon library

### Infrastructure
- **Docker + Docker Compose** — full-stack local environment
- **Nginx** — production web server / reverse proxy

---

## Common Commands

### Frontend
```bash
# Working directory: src/frontend

npm run dev          # Start Vite dev server (http://localhost:5173)
npm run build        # TypeScript check + production build
npm run lint         # ESLint (zero warnings policy)
npm run format       # Prettier format
npm run test         # Vitest (single run)
npm run test:watch   # Vitest (watch mode)
npm run test:coverage # Coverage report
```

### Backend
```bash
# Working directory: src/backend

dotnet restore                    # Restore NuGet packages
dotnet build                      # Build all projects
dotnet run --project PartnershipManager.API   # Start API (http://localhost:5000)
dotnet test                       # Run all xUnit tests
dotnet test --filter Category=Unit           # Run only unit tests
```

### Docker (Full Stack)
```bash
docker-compose up -d              # Start all services (API, MySQL, Redis, Nginx)
docker-compose up -d --build      # Rebuild and start
docker-compose down               # Stop all services
docker-compose logs -f backend    # Tail backend logs
```

---

## Testing

### Frontend — Vitest + Testing Library
- Test files: `src/**/__tests__/**/*.{ts,tsx}` or `src/**/*.test.{ts,tsx}`
- Setup file: `src/test/setup.ts`
- Run one test file: `npx vitest run src/path/to/file.test.tsx`

### Backend — xUnit + Moq + FluentAssertions
- Test project: `src/backend/PartnershipManager.Tests/`
- Unit tests: `Unit/Domain/` and `Unit/Application/`
- Integration tests: `Integration/`
- E2E tests: `E2E/`
- Run one test: `dotnet test --filter "FullyQualifiedName~TestClassName"`

---

## Environment Setup

### Local Development (without Docker)
Requires: Node.js 24.22.1+, .NET SDK 9.0, MySQL 8.0, Redis

1. Copy `.env.example` to `.env` and fill in values
2. Start MySQL and Redis locally (or via Docker)
3. Run backend: `cd src/backend && dotnet run --project PartnershipManager.API`
4. Run frontend: `cd src/frontend && npm ci && npm run dev`

### Docker (Recommended)
```bash
cp .env.example .env      # Configure environment variables
docker-compose up -d      # Spin up the full stack
```

Services:
| Service  | URL / Port        |
|----------|-------------------|
| Frontend | http://localhost  |
| API      | http://localhost:5000 |
| Swagger  | http://localhost:5000/swagger |
| MySQL    | localhost:3306    |
| Redis    | localhost:6379    |

---

## Architecture Notes

- **Clean Architecture**: Domain → Application → Infrastructure → API (dependency direction)
- **Repository Pattern**: all DB access goes through interfaces in Domain, implemented in Infrastructure via Dapper
- **No EF Core ORM**: raw SQL through Dapper; migrations are SQL script files
- **CQRS-light**: Commands and Queries separated in Application layer via service classes
- **Frontend API layer**: all HTTP calls centralized in `src/services/`; React Query handles caching and refetching
- **Auth flow**: JWT issued by API, stored in browser, sent as `Authorization: Bearer <token>` header

---

## Key Domain Modules

| Module | Description |
|--------|-------------|
| Cap Table | Equity ownership table per company |
| Vesting | Vesting schedules and cliff logic |
| Contracts | Contract generation and versioning |
| Valuation | Company valuation rounds (financial modeling) |
| Billing / Invoices | Invoice generation and tracking |
| Partnership Manager | Investor communication and relationship management |

---

## System Administration Access

### SuperAdmin Credentials
| Field    | Value                  |
|----------|------------------------|
| E-mail   | `admin@sistema.com`    |
| Senha    | `SysAdmin@2024!`       |
| Role     | `SuperAdmin`           |
| Status   | `Active`               |

The SuperAdmin account has no `company_id` restriction — it can access all tenants. It is created by **migration 049** (`docker/mysql/migrations/049_add_superadmin_user.sql`).

To apply to an existing database:
```bash
mysql -u pm_user -p partnership_manager < docker/mysql/migrations/049_add_superadmin_user.sql
```

> **Security note:** Change this password immediately in any non-local environment via `POST /api/auth/change-password`.

---

## Billing & Clients — Assessment

### What's implemented

| Area | Status | Notes |
|------|--------|-------|
| `BillingClients` CRUD | ✅ Complete | Controller, service, repo, DTOs all wired up |
| `BillingPlans` | ✅ Complete | Plan management with MaxCompanies/MaxUsers limits |
| `BillingSubscriptions` | ✅ Complete | Activate, Suspend, Cancel lifecycle |
| `BillingInvoices` CRUD | ✅ Complete | Create, update, soft-delete |
| Invoice actions | ✅ Complete | MarkAsPaid, MarkAsOverdue, Cancel |
| PDF generation | ✅ Complete | `GET /api/invoices/{id}/pdf` |
| Monthly invoice generation | ✅ Complete | `POST /api/invoices/generate-monthly` |
| Invoice statistics / MRR | ✅ Complete | Revenue metrics and monthly breakdown |
| Filtered invoice list | ✅ Complete | Filter by client, status, date range, plan |
| `BillingPayments` table | ⚠️ Partial | Table exists and FK set up; no payment handler yet |
| Link BillingClients ↔ core clients | ⚠️ Partial | Migration 007 adds `core_client_id` FK but no service logic using it |

### Known issues / gaps

1. **Duplicate broken `CREATE TABLE users` in `init.sql`** — Fixed in this branch (lines 182–184 removed).
2. **`BillingPayments` has no repository or handler** — the table and entity exist, but payment recording isn't connected to invoice status changes. `MarkAsPaid` updates `InvoiceStatus` without creating a `BillingPayments` row.
3. **`BillingClients` ↔ `clients` are separate entities** — there are two unrelated "client" concepts:
   - `clients` — SaaS tenant root (multi-tenancy), used in auth and company management.
   - `BillingClients` — billing-only entity for invoicing. Migration 007 adds `core_client_id` FK but no service code syncs them.
4. **Invoice number race condition** — `GenerateInvoiceNumberAsync` uses `COUNT(*) + 1` without a transaction/lock; concurrent inserts can produce duplicate numbers.
5. **Overdue detection is query-only** — `GetOverdueInvoicesAsync` finds past-due invoices but there is no Hangfire job to automatically flip their status to `Overdue`.

### Billing module file map

```
backend/
  Domain/Entities/Billing/        Client.cs, Invoice.cs, Subscription.cs, Plan.cs, Payment.cs
  Domain/Interfaces/Billing/      IBillingRepositories.cs
  Application/Features/Billing/
    DTOs/                         ClientDTOs.cs, InvoiceDtos.cs, SubscriptionDtos.cs
    Commands/                     InvoiceCommands.cs
    Queries/                      InvoiceQueries.cs
    Handlers/                     InvoiceCommandHandlers.cs, InvoiceQueryHandlers.cs
  Infrastructure/Repositories/Billing/
                                  ClientRepository.cs, InvoiceRepository.cs,
                                  SubscriptionRepository.cs, PlanRepository.cs
  API/Controllers/Billing/
                                  BillingClientsController.cs, InvoicesController.cs,
                                  SubscriptionsController.cs

frontend/
  pages/billing/                  BillingDashboard.tsx, Invoices.tsx, ClientsSubscriptions.tsx
  components/modals/              InvoiceModal.tsx, SubscriptionModal.tsx
```

---

## Code Conventions

### C# Backend
- Use `async/await` throughout; all repository methods are async
- Validate inputs with FluentValidation in Application layer, not controllers
- Return `Result<T>` or throw domain exceptions — never leak infrastructure exceptions to API layer
- Log with Serilog structured properties: `Log.Information("Action {Action} by {UserId}", ...)`

### TypeScript Frontend
- Strict TypeScript — no `any`
- Components in PascalCase, hooks prefixed with `use`
- API response types defined in `src/types/`
- Global app state in Zustand stores; server state via React Query
- Form validation schemas live alongside their form components using Zod
