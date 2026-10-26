# P-Pro | WP Manager

Sistema de Gestão Societária para empresas que precisam gerenciar Cap Table, Vesting, Contratos, Valuations e comunicação com investidores.

## 🚀 Stack Tecnológica

### Backend
- **.NET 9** - Framework web
- **Dapper** - Micro ORM para acesso a dados
- **MySQL 8.0** - Banco de dados relacional
- **Redis** - Cache distribuído
- **Hangfire** - Processamento de jobs em background
- **JWT** - Autenticação
- **Swagger** - Documentação de APIs
- **FluentValidation** - Validação de dados
- **Serilog** - Logging estruturado

### Frontend
- **React 18** - Biblioteca UI
- **TypeScript** - Tipagem estática
- **Vite** - Build tool
- **Tailwind CSS** - Framework CSS
- **React Query** - Gerenciamento de estado assíncrono
- **Zustand** - Gerenciamento de estado global
- **React Hook Form + Zod** - Formulários e validação
- **Lucide React** - Ícones

### Infraestrutura
- **Docker** & **Docker Compose** - Containerização
- **Nginx** - Servidor web (produção)

---

## 📁 Estrutura do Projeto

```
partnership-manager/
├── docker/
│   ├── mysql/
│   │   ├── init/           # Scripts de inicialização
│   │   └── my.cnf          # Configuração do MySQL
│   └── redis/
│       └── redis.conf      # Configuração do Redis
├── src/
│   ├── backend/
│   │   ├── PartnershipManager.API/           # Controllers, Middlewares
│   │   ├── PartnershipManager.Application/   # DTOs, Validators, Services
│   │   ├── PartnershipManager.Domain/        # Entities, Enums, Interfaces
│   │   └── PartnershipManager.Infrastructure/ # Repositories, Dapper, Cache
│   └── frontend/
│       ├── src/
│       │   ├── components/    # Componentes React
│       │   ├── pages/         # Páginas
│       │   ├── stores/        # Estado global (Zustand)
│       │   ├── services/      # Chamadas API
│       │   ├── hooks/         # Custom hooks
│       │   ├── constants/     # Constantes e mensagens
│       │   ├── types/         # Tipos TypeScript
│       │   └── utils/         # Utilitários
│       └── public/
├── docs/                      # Documentação
├── scripts/                   # Scripts utilitários
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## 🛠️ Setup do Ambiente

### Pré-requisitos
- Docker e Docker Compose
- Node.js 24.22.1+ (para desenvolvimento local)
- .NET SDK 9.0 (para desenvolvimento local)

### 1. Clonar e Configurar

```bash
# Clonar o repositório
git clone <repository-url>
cd partnership-manager

# Copiar arquivo de ambiente
cp .env.example .env

# Editar variáveis conforme necessário
nano .env
```

### 2. Iniciar com Docker

```bash
# Iniciar todos os serviços
docker-compose up -d

# Verificar status
docker-compose ps

# Ver logs
docker-compose logs -f api
```

### 3. Acessar os Serviços

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | - |
| **API** | http://localhost:5000 | - |
| **Swagger** | http://localhost:5000/swagger | - |
| **Hangfire** | http://localhost:5000/hangfire | admin / Hangfire@2025 |
| **Adminer** | http://localhost:8080 | pm_user / Pm@2025 |

### 4. Usuário de Demonstração



---

## 💻 Desenvolvimento Local

### Backend (.NET)

```bash
cd src/backend

# Restaurar pacotes
dotnet restore

# Executar em modo desenvolvimento
dotnet run --project PartnershipManager.API

# Build
dotnet build

# Testes
dotnet test
```

### Frontend (React)

```bash
cd src/frontend

# Instalar dependências conforme o lockfile
npm ci

# Executar em modo desenvolvimento
npm run dev

# Build de produção
npm run build

# Lint
npm run lint
```

---

## 🎨 Design System

### Cores Principais

| Cor | Código | Uso |
|-----|--------|-----|
| **Primary** | `#111827` | Sidebar, botões primários, textos |
| **Secondary** | `#333333` | Textos secundários, bordas |
| **Background** | `#F3F4F6` | Fundo da aplicação |
| **Accent** | `#0891B2` | Destaques, links, elementos interativos |
| **Success** | `#059669` | Status positivo, valores monetários |
| **Warning** | `#D97706` | Alertas, pendências |
| **Error** | `#DC2626` | Erros, exclusões |
| **Info** | `#2563EB` | Informações, fundadores |
| **Purple** | `#7C3AED` | Investidores, simulador |

### Componentes UI

- **Buttons**: Primary, Secondary, Success, Danger, Ghost
- **Cards**: Default, Flat, StatCard
- **Badges**: Active, Pending, Inactive, Vesting, Investor, Founder
- **Inputs**: Com label, error, hint
- **Tables**: Com sorting, filtros, ações
- **Modals**: Header, body, footer
- **Charts**: Donut, Line, Bar (via Recharts)

---

## 📚 APIs

### Autenticação

```http
POST /api/auth/login
POST /api/auth/logout
POST /api/auth/refresh
GET  /api/auth/me
POST /api/auth/change-password
```

### Empresas

```http
GET    /api/companies
GET    /api/companies/{id}
POST   /api/companies
PUT    /api/companies/{id}
PATCH  /api/companies/{id}/shares
DELETE /api/companies/{id}
```

### Usuários

```http
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
POST   /api/users/{id}/roles
DELETE /api/users/{id}/roles/{role}
```

---

## 🔧 Variáveis de Ambiente

```env
# Database
MYSQL_ROOT_PASSWORD=Root@123
MYSQL_DATABASE=partnership_manager
MYSQL_USER=pm_user
MYSQL_PASSWORD=Pm@2025

# JWT
JWT_SECRET=PartnershipManager_SecretKey_2025_Min32Chars!
JWT_ISSUER=PartnershipManager
JWT_AUDIENCE=PartnershipManagerApp
JWT_EXPIRATION_HOURS=24

# Redis
REDIS_HOST=redis
REDIS_PORT=6379

# Frontend
VITE_API_URL=http://localhost:5000/api

# CORS
CORS_ORIGINS=http://localhost:3000,http://localhost:5173
```

---

## 📦 Scripts Úteis

```bash
# Rebuild containers
docker-compose up -d --build

# Limpar tudo
docker-compose down -v

# Logs de um serviço específico
docker-compose logs -f mysql

# Acessar container
docker exec -it pm-api /bin/bash
docker exec -it pm-mysql mysql -u pm_user -p

# Backup do banco
docker exec pm-mysql mysqldump -u pm_user -p partnership_manager > backup.sql
```

---

## 🚀 Deploy em Produção

1. Ajustar variáveis de ambiente para produção
2. Build das imagens:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml build
   ```
3. Configurar HTTPS (Nginx/Traefik)
4. Configurar backups automáticos
5. Monitoramento (Prometheus/Grafana)

---

## 📝 Roadmap

- [x] **Fase 1**: Core (Dashboard, Cap Table, Sócios, Auth)
- [ ] **Fase 2**: Operações (Aprovações, Simulador de Rodadas)
- [ ] **Fase 3**: Contratos (Templates, Builder, Assinatura)
- [ ] **Fase 4**: Incentivos (Vesting, Metas, Grants)
- [ ] **Fase 5**: Financeiro (Valuation, Métricas, Documentos)
- [ ] **Fase 6**: Portal (Investidor, Comunicações, Data Room)

---

## 📄 Licença

Proprietary - © 2025 P-Pro | WP Manager

---

## 👥 Equipe

Desenvolvido pela Equipe Ophir
