# Security Remediation Tracker

Documento de acompanhamento para tratar item a item os riscos levantados na revisão de segurança.

## Como usar este tracker

- Status sugerido por item: `Pendente`, `Em andamento`, `Bloqueado`, `Validado`.
- Em cada item, registrar evidências objetivas (PR, commit, teste, log sanitizado, print de configuração sem segredos).
- Não inserir segredos, tokens, hashes, senhas ou dumps sensíveis neste arquivo.

## Visão geral de prioridade

1. Contenção imediata: segredos e vazamento em logs.
2. Bloqueio de exploração: autorização/tenant, webhook, links de autenticação, XSS.
3. Fortalecimento de sessão e plataforma: refresh/revogação, hardening HTTP, Node/container/dependências/CI.
4. Validação operacional em ambiente: Azure/App Service/Key Vault/imagem implantada/contas seed.

## Itens de remediação

### 1) Escalação de privilégio e acesso cross-tenant em gestão de usuários
- **Severidade:** HIGH
- **Arquivos principais:** `src/backend/PartnershipManager.API/Controllers/UsersController.cs`
- **Impacto possível:**
  - Admin de um tenant pode alterar usuários de outro tenant (quebra de isolamento).
  - Possível elevação para `SuperAdmin` e comprometimento total multi-tenant.
  - Alteração indevida de usuários (desativar/excluir/promover).
- **Ação técnica:**
  - Validar ownership por `ClientId` e `CompanyId` em todos os endpoints de gestão de usuários.
  - Bloquear atribuição de `SuperAdmin` exceto para `SuperAdmin`.
  - Impedir autoelevação/autoalteração de role sensível.
- **Configuração de ambiente necessária:**
  - Garantir claims de tenant consistentes no emissor JWT.
  - Definir políticas de autorização por role no ambiente de API (mesmas regras entre dev/homolog/prod).
- **Evidência esperada:**
  - Testes automatizados negando leitura/alteração cross-tenant e atribuição indevida de role.
  - Validar claims de tenant e matriz de permissões nos ambientes implantados.
- **Progresso:** Verificações de tenant por `ClientId` e `CompanyId` aplicadas aos endpoints de usuário; atribuição de `SuperAdmin` restrita ao próprio `SuperAdmin`; criação por HR limitada a `Employee`, `Investor` e `Viewer`; usuários não-SuperAdmin não podem alterar suas próprias roles.
- **Validação de código:** Testes focados de acesso por tenant/role e suíte backend completa aprovados (226 testes).
- **Status:** Em andamento — falta validar dados/configuração e políticas efetivas nos ambientes implantados.

### 2) Vazamento de senha/hash em log de autenticação
- **Severidade:** HIGH
- **Arquivos principais:** `src/backend/PartnershipManager.Infrastructure/Services/AuthService.cs`
- **Impacto possível:**
  - Exposição de credenciais em logs de aplicação.
  - Se logs forem acessíveis, há risco de comprometimento de contas.
- **Ação técnica:**
  - Remover logging de senha/hash e qualquer dado sensível.
  - Substituir por logs estruturados sem conteúdo secreto.
- **Configuração de ambiente necessária:**
  - Revisar política de retenção de logs e permissões de acesso.
  - Se aplicável, saneamento/rotação de credenciais potencialmente expostas.
- **Evidência esperada:**
  - `VerifyPassword` não registra senha nem hash; teste automatizado verifica saída de console vazia.
  - Revisão operacional dos logs existentes e aplicação da política de retenção/acesso.
- **Progresso:** Removidas as mensagens de console com senha/hash da verificação de senha; teste confirma que a verificação não escreve na saída de console.
- **Validação de código:** Teste focado e suíte backend completa aprovados (226 testes).
- **Status:** Em andamento — revisão/limpeza de logs históricos depende do operador do ambiente.

### 3) Segredos/credenciais hardcoded em config
- **Severidade:** HIGH
- **Arquivos principais:** `src/backend/PartnershipManager.API/appsettings*.json`, `docker-compose*.yml`
- **Impacto possível:**
  - Forja de JWT e acesso não autorizado.
  - Acesso indevido ao banco de dados e, caso seja habilitado, ao Hangfire.
  - Exposição lateral em forks/clones/histórico.
- **Ação técnica:**
  - Remover segredos versionados do código.
  - Consumir segredos por variáveis seguras e/ou Key Vault.
  - Validar falha de inicialização quando segredo obrigatório estiver fraco/ausente.
- **Configuração de ambiente necessária:**
  - **Obrigatórias para a aplicação:** `Jwt__Secret` (chave aleatória forte; ao trocar, sessões JWT existentes deixam de funcionar) e `ConnectionStrings__MySQL` (conexão do banco de produção com credencial rotacionada e TLS).
  - **E-mail:** o provedor em uso é Resend; `Email__Resend__ApiKey` já está configurada no ambiente de produção, conforme confirmação operacional. Manter essa chave no store seguro e não provisionar configuração SMTP. Como credenciais SMTP antigas aparecem em configuração versionada, revogue-as no provedor se ainda forem válidas e remova-as dos arquivos no escopo da limpeza de segredos; não é necessário substituí-las por credenciais SMTP no ambiente.
  - **Redis — configurar depois:** `ConnectionStrings__Redis`. É opcional para inicialização: sem Redis o código usa cache em memória; configurar somente quando o serviço Redis estiver sendo usado, com autenticação e TLS conforme a infraestrutura.
  - **ClickSign — configurar depois, antes de habilitar/usar a integração:** `ClickSign__AccessToken` e `ClickSign__WebhookSecret`. O token autentica chamadas à ClickSign; o segredo será necessário para validar a assinatura do webhook. Também manter `ClickSign__UseSandbox` e `ClickSign__BaseUrl` como configurações não secretas apropriadas ao ambiente.
  - **Hangfire — configurar depois, somente se for reativado:** `ConnectionStrings__Hangfire`. O registro do servidor Hangfire está atualmente desativado no código; não provisionar apenas por existir uma chave em arquivo de configuração. Se a ativação for aprovada, configurar conexão/credenciais de banco segregadas e revisar também `Hangfire__DashboardUser` e `Hangfire__DashboardPassword`.
  - Provisionar/rotacionar os segredos aplicáveis em Azure App Service/Key Vault antes de remover valores versionados; não copiar valores de arquivos de configuração para o ambiente sem rotação.
  - Configurações não secretas relacionadas: `Email__Provider=Resend`, `Email__FrontendUrl`, `Email__FromEmail` e `Cors__Origins` com valores próprios de cada ambiente.
- **Evidência esperada:**
  - AppSettings sem segredos versionados; nenhuma credencial SMTP configurada no ambiente.
  - Presença confirmada das chaves necessárias no ambiente sem revelar valores; Resend confirmado como já configurado em produção.
  - Redis, ClickSign e Hangfire registrados como pendências de configuração para quando forem ajustados/ativados.
- **Status:** Em andamento — inventário de variáveis e dependências documentado; JWT/MySQL ainda precisam de confirmação de provisionamento/rotação, e Redis/ClickSign/Hangfire ficam para configuração posterior.

### 4) Credenciais seed/demo conhecidas
- **Severidade:** HIGH
- **Arquivos principais:** `docker/mysql/migrations/049_add_superadmin_user.sql`, `docker/mysql/migrations/056_seed_fase1_foundation.sql`, `CREDENCIAIS_DEMO.md`
- **Impacto possível:**
  - Se contas seed existirem em produção, comprometimento administrativo completo.
- **Ação técnica:**
  - Restringir seeds de demo para desenvolvimento.
  - Revisar/remover documentação pública de credenciais demo.
  - Forçar rotação/expiração dessas contas onde existirem.
- **Configuração de ambiente necessária:**
  - Auditoria de usuários reais em produção/homologação.
  - Política de bloqueio para contas de seed fora de dev.
- **Evidência esperada:**
  - Consulta/auditoria demonstrando ausência ou rotação das contas seed em produção.
- **Status:** Pendente

### 5) Webhook ClickSign sem validação de assinatura
- **Severidade:** HIGH
- **Arquivos principais:** `src/backend/PartnershipManager.API/Controllers/ClickSignWebhookController.cs`, `src/backend/PartnershipManager.Infrastructure/Services/ClickSignWebhookService.cs`
- **Impacto possível:**
  - Alteração fraudulenta de estado de contratos e assinaturas.
  - Risco jurídico e operacional elevado.
- **Ação técnica:**
  - Validar assinatura/HMAC no payload bruto.
  - Rejeitar requisição quando segredo ausente/inválido.
  - Opcional: allowlist de origem/IP e proteção anti-replay.
- **Configuração de ambiente necessária:**
  - Definir `ClickSign:WebhookSecret` via segredo do ambiente.
  - Configurar source trust (IP/rede) conforme suporte da integração.
- **Evidência esperada:**
  - Testes com assinatura válida/inválida.
  - Endpoint retornando erro para payload não assinado.
- **Status:** Pendente

### 6) Endpoints sensíveis sem RBAC fino + validação de associação à empresa
- **Severidade:** HIGH
- **Arquivos principais:** múltiplos em `src/backend/PartnershipManager.API/Controllers/` e middleware de contexto de empresa
- **Impacto possível:**
  - Usuários autenticados com baixa permissão executam ações críticas.
  - Violação de segregação de funções.
- **Ação técnica:**
  - Aplicar políticas por recurso/ação (least privilege).
  - Validar que a empresa solicitada corresponde à empresa associada ao usuário autenticado antes de aceitar `X-Company-Id`/`companyId`.
  - Não presumir que exista uma tabela `user_companies`: a revisão do schema/código encontrou apenas um TODO no middleware. Confirmar o modelo atual de associação antes de implementar suporte a múltiplas empresas por usuário; até lá, negar contextos sem associação comprovável.
  - Definir e aprovar a matriz de permissões por ação para ações societárias, financeiras, jurídicas, RH e leitura; exigir as roles/policies correspondentes em cada endpoint, não apenas autenticação.
- **Configuração de ambiente necessária:**
  - Matriz de papéis/permissões aprovada e replicada por ambiente (por exemplo, quem pode emitir/transferir ações, aprovar valuation, editar contratos, publicar comunicações e consultar documentos).
  - Claims `clientId`, `companyId` e roles emitidas corretamente nos JWTs; garantir que mudanças de role sejam refletidas/revogadas conforme a política de sessão.
  - Não requer um novo segredo de ambiente. Se for decidido suportar múltiplas empresas por usuário, provisionar/migrar a relação de associação e atribuí-la corretamente antes de habilitar a seleção de empresa.
- **Evidência esperada:**
  - Testes de autorização por endpoint sensível, role e empresa.
  - Cenários negativos bloqueados para perfis não autorizados e para usuário que manipula o ID/header de empresa.
  - Confirmação explícita do modelo de associação: empresa única via claim ou associação multiempresa persistida.
- **Status:** Pendente

### 7) Uso de header `Origin` para links de reset/ativação
- **Severidade:** HIGH
- **Arquivos principais:** `src/backend/PartnershipManager.API/Controllers/AuthController.cs`, `src/backend/PartnershipManager.API/Controllers/UsersController.cs`
- **Impacto possível:**
  - Envio de links com domínio atacante e roubo de token de reset/ativação.
- **Ação técnica:**
  - Gerar links apenas com base em URL confiável de configuração.
  - Usar `https://ppro.wpmanager.com.br` como URL confiável canônica de produção.
  - Ignorar `Origin` para construção de links de e-mail em todos os fluxos de reset, ativação e convite.
- **Configuração de ambiente necessária:**
  - Definir `Email__FrontendUrl=https://ppro.wpmanager.com.br` no App Service/Key Vault para produção.
  - Para desenvolvimento/homologação, usar URLs próprias somente em configurações isoladas desses ambientes; não aceitar URL dinâmica do header `Origin`.
- **Evidência esperada:**
  - E-mails de reset/ativação/convite apontam para `https://ppro.wpmanager.com.br` em produção, mesmo que a requisição contenha outro `Origin`.
- **Progresso:** Removido o uso do header `Origin` em links de autenticação, convite, comunicação e workflow; `Email:FrontendUrl` de produção definido como `https://ppro.wpmanager.com.br`.
- **Status:** Em andamento — validar entrega dos links em produção após deploy.

### 8) XSS armazenado por sanitização insuficiente
- **Severidade:** HIGH
- **Arquivos principais:** frontend de comunicações/contratos e sanitização backend de templates
- **Impacto possível:**
  - Execução de script no navegador de usuários privilegiados.
  - Roubo de token de sessão e ações indevidas no contexto do usuário.
- **Ação técnica:**
  - Trocar blacklist por sanitização allowlist robusta.
  - Validar esquemas de URL (`http/https`) antes de renderizar links.
  - Adicionar CSP (defesa em profundidade).
- **Configuração de ambiente necessária:**
  - Definir e publicar política CSP adequada ao app.
  - Alinhar reverse proxy para enviar headers de segurança.
- **Evidência esperada:**
  - Testes com payloads XSS conhecidos bloqueados.
  - Inspeção de resposta com headers de segurança aplicados.
- **Progresso:** Allowlist aplicada no backend e no frontend com DOMPurify; conteúdo de comunicação/template sanitizado antes de persistir e ao renderizar; links documentais aceitam apenas HTTP(S) e caminhos relativos seguros. Suíte backend (231 testes), suíte frontend (34 testes) e build TypeScript/Vite passaram.
- **Progresso adicional:** CSP e headers `X-Content-Type-Options`, `X-Frame-Options` e `Referrer-Policy` adicionados às configurações Nginx do container integrado e do frontend.
- **Status:** Em andamento — validar a sintaxe Nginx e a política efetiva no proxy/ambiente após deploy; `nginx -t` não pôde ser executado porque o binário não está instalado neste ambiente.

### 9) Refresh token e revogação de sessão insuficientes
- **Severidade:** MEDIUM
- **Arquivos principais:** `src/backend/PartnershipManager.API/Controllers/AuthController.cs`, `src/backend/PartnershipManager.Infrastructure/Persistence/Repositories/UserRepository.cs`
- **Impacto possível:**
  - Sessão continua válida após desativação/reset.
  - Persistência indevida de acesso após incidente.
- **Ação técnica:**
  - Validar status/lockout no refresh.
  - Revogar e rotacionar refresh token em mudança/reset de senha.
  - Armazenar refresh token protegido (SHA-256) e encerrar sessões após 5 minutos sem atividade.
- **Configuração de ambiente necessária:**
  - Política de expiração/rotação de token padronizada.
  - Procedimento de revogação em massa para incidentes.
- **Evidência esperada:**
  - Testes cobrindo expiração por inatividade, status/lockout e reset de senha invalidando refresh.
- **Progresso:** Refresh tokens são persistidos como SHA-256 e rotacionados; refresh verifica status/lockout; troca/reset de senha revoga o token. Cliente e backend aplicam timeout de 5 minutos, com heartbeat de atividade. Suíte backend completa aprovada (231 testes), incluindo sessão expirada.
- **Operação:** Migration `docker/mysql/migrations/072_secure_refresh_token_idle_timeout.sql` aplicada no banco, conforme confirmação operacional; ela transforma tokens existentes em hashes e limita sessões atuais a no máximo mais 5 minutos.
- **Status:** Em andamento — falta validar logout/renovação em ambiente integrado após deploy.

### 10) Hardening HTTP/diagnóstico insuficiente
- **Severidade:** MEDIUM
- **Arquivos principais:** `src/backend/PartnershipManager.API/Program.cs`, `src/backend/PartnershipManager.API/Extensions/ServiceExtensions.cs`, `nginx.conf`
- **Impacto possível:**
  - Superfície adicional de reconhecimento e abuso.
  - Exposição de metadados operacionais.
- **Ação técnica:**
  - Restringir Swagger e detalhes de health por ambiente/role.
  - Endurecer CORS em produção e restringir hosts permitidos.
  - Aplicar headers de segurança e processar forwarded headers somente do proxy confiável.
- **Configuração de ambiente necessária:**
  - Lista explícita de origens permitidas em produção.
  - Configuração de proxy reverso para headers de segurança.
- **Evidência esperada:**
  - Endpoints de diagnóstico protegidos.
  - Cabeçalhos de segurança presentes nas respostas.
- **Progresso:** Swagger restrito a Development; `/health` expõe somente Healthy/Unhealthy para probes e `/health/details` exige role Admin/SuperAdmin; `/health/live` informa liveness. CORS de produção usa apenas `https://ppro.wpmanager.com.br`, enquanto localhost/Codespaces permanece permitido somente em Development. `AllowedHosts` de produção limitado ao domínio confiável; forwarded headers habilitados com limite de um salto; headers de segurança adicionados às rotas Nginx da API e do frontend.
- **Validação:** Build e suíte backend aprovados (231 testes). A sintaxe Nginx não pôde ser verificada localmente porque o binário `nginx` não está instalado.
- **Status:** Em andamento — validar deployment, probe de health, CORS e headers efetivamente enviados em produção.

### 11) Node/container hardening (Node 20 EOL)
- **Severidade:** MEDIUM
- **Arquivos principais:** `Dockerfile`, `src/frontend/Dockerfile`, `.devcontainer/devcontainer.json`, `docker-compose.yml`
- **Impacto possível:**
  - Build/dev com runtime EOL, vulnerável e fora de suporte.
  - Risco de supply chain por tags flutuantes e processos root.
- **Ação técnica:**
  - Migrar Node para **24.22.1+** (ou superior suportado) nos estágios de build/dev.
  - Trocar `npm install` por `npm ci` em build reproduzível.
  - Evoluir para execução non-root e imagens pinadas por digest.
- **Configuração de ambiente necessária:**
  - Atualizar ambiente de CI/devcontainer para mesma linha Node.
  - Revisar regras de publicação de imagem e scanner de imagem.
- **Evidência esperada:**
  - Build com nova versão Node.
  - Scanner sem apontar EOL para Node 20 nos artefatos de build/dev.
- **Progresso:** Imagens Node migradas para 24.22.1 e pinadas por digest; `npm ci` usado nos Dockerfiles e no hook de setup; `.nvmrc` e Dev Container alinhados. Os estágios Node executam como `node`. Nginx standalone usa `nginx-unprivileged` 1.30 sobre Alpine 3.24, pinado por digest; `musl`, `musl-utils` e `zlib` são atualizados para versões corrigidas durante o build. `.dockerignore` evita enviar dependências, build outputs e arquivos `.env` no contexto. Documentação atualizada.
- **Validação:** `npm ci --ignore-scripts` local, `npm ci` no estágio Docker como usuário `node` e builds Vite de produção passaram. Revalidação local em 22/10/2026: build Docker completo do estágio `production`, build TypeScript/Vite e execução do estágio `development` confirmaram Node `v24.22.1` como UID 1000 (`node`). Trivy no runtime final (Alpine 3.24.2), sem ignorar vulnerabilidades sem correção, não reportou achados. Trivy nos estágios `development` e `build` encontrou em cada imagem 5 CRITICAL, 34 HIGH, 67 MEDIUM, 6 LOW e 3 UNKNOWN, principalmente em dependências de desenvolvimento (incluindo `tinypool`, `vitest`, `vite` e `undici`); tratar no item 13. Ambos os estágios reportam Node `v24.22.1`, sem Node 20. O runtime atualizado respondeu HTTP 200 na porta 8080, passou `nginx -t`, enviou os cabeçalhos de segurança e executou como UID 101. O deploy em produção ainda não foi validado.
- **Status:** Em andamento — implementação e validação local do código/imagens concluídas; resta validar o deployment real do frontend na porta 8080, uma etapa operacional que depende do ambiente de produção. Vulnerabilidades de dependências npm dos estágios build/dev seguem pendentes no item 13.

### 12) Tokens em `sessionStorage` (amplifica impacto de XSS)
- **Severidade:** LOW
- **Arquivos principais:** `src/frontend/src/stores/authStore.ts`
- **Impacto possível:**
  - Em caso de XSS, tokens podem ser extraídos e reutilizados.
- **Ação técnica:**
  - Avaliar refresh token em cookie HttpOnly/SameSite e redução de exposição no frontend.
- **Configuração de ambiente necessária:**
  - Política de cookies segura (`Secure`, `HttpOnly`, `SameSite`) e HTTPS obrigatório.
- **Evidência esperada:**
  - Fluxo de autenticação mantendo segurança sem regressões funcionais.
- **Status:** Pendente

### 13) Vulnerabilidades npm reportadas em auditoria
- **Severidade:** MEDIUM
- **Arquivos principais:** `src/frontend/package.json`, `src/frontend/package-lock.json`
- **Impacto possível:**
  - Dependências vulneráveis (incluindo dev tooling) podem ser exploradas em cadeias específicas.
- **Ação técnica:**
  - Atualizar dependências vulneráveis com validação de compatibilidade.
  - Rodar auditoria completa e de produção após atualização.
- **Configuração de ambiente necessária:**
  - Definir política mínima de severidade que bloqueia merge/deploy no CI.
- **Evidência esperada:**
  - Relatório de auditoria pós-correção.
- **Status:** Pendente

### 14) Pipeline sem gates de segurança obrigatórios
- **Severidade:** MEDIUM
- **Arquivos principais:** `.github/workflows/main_ppro.yml`
- **Impacto possível:**
  - Risco de promover código vulnerável para produção sem bloqueios.
- **Ação técnica:**
  - Incluir gates de build, testes, SAST e SCA antes de deploy.
  - Atualizar actions legadas para versões mantidas.
- **Configuração de ambiente necessária:**
  - Segredos de CI para scanners e políticas de branch protection.
  - Regra de bloqueio de merge/deploy em falha de segurança.
- **Evidência esperada:**
  - Workflow falhando em vulnerabilidade crítica/alta não tratada.
- **Status:** Pendente

## Checklist de configuração por ambiente

### Desenvolvimento
- Variáveis locais sem segredos reais e sem credenciais de produção.
- Node padronizado (24.22.1+), `npm ci`, auditoria de dependências.
- Portas e serviços dev sem exposição pública desnecessária.

### CI/CD
- Segredos em store seguro do provedor (não no repositório).
- Gates obrigatórios: build, testes, auditoria dependências, SAST.
- Política de bloqueio por severidade definida.
- Atualização e pinagem de actions/imagens conforme política interna.

### Produção
- Segredos em Key Vault/App Settings com rotação e owner definido.
- Resend como provedor de e-mail; API key já configurada em produção (confirmado pelo responsável). Não provisionar SMTP.
- JWT e MySQL: confirmar/configurar segredos rotacionados antes de remover os valores versionados.
- Redis: configurar `ConnectionStrings__Redis` posteriormente, se/ao usar o serviço.
- ClickSign: configurar `ClickSign__AccessToken` e `ClickSign__WebhookSecret` posteriormente, antes de ativar/usar a integração.
- Hangfire: configurar `ConnectionStrings__Hangfire` e credenciais do dashboard somente se a reativação for planejada.
- CORS restrito, headers de segurança ativos e diagnóstico protegido.
- Contas seed/demo inexistentes ou desativadas com senha rotacionada.
- Validação de webhook assinada e monitorada.
- Procedimento de revogação de sessão e resposta a incidentes documentado.

## Registro de validação (preencher a cada item concluído)

| Item | PR/Commit | Ambiente validado | Evidência (sem segredo) | Resultado |
|---|---|---|---|---|
| Ex.: Item 5 | #0000 | Homologação | Teste de assinatura inválida retornando 401 | Validado |
