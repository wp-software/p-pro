# Pendências de Dependências Vulneráveis

Estado após o commit de atualização de dependências (Dependabot/`npm audit`/`dotnet list package --vulnerable`).

## Já corrigido

- Backend: `MailKit`/`MimeKit`, `System.Text.Encodings.Web`, `Newtonsoft.Json`, `System.Security.Cryptography.Pkcs`, `System.Net.Http` 4.3.4 e `System.Security.Cryptography.X509Certificates` 4.3.2 (fixados no `PartnershipManager.Infrastructure.csproj`; vinham de `NCalc2`). `dotnet list package --vulnerable` sem pendências.
- Frontend (produção): `npm audit fix` e `react-router-dom` 6 → 7.18.4. `npm audit --omit=dev` com 0 vulnerabilidades.

## Pendente: ferramentas de desenvolvimento (frontend)

Não rodam em produção; exigem atualização de versão principal (`npm audit` indica 3 críticas, 12 altas, 5 moderadas).

| Pacote | Versão atual | Alvo | Severidade |
|--------|--------------|------|------------|
| `vitest` | ^2.1.9 | 5.0.5 | crítica |
| `@vitest/coverage-v8` | ^2.1.9 | 5.0.5 | crítica |
| `vite` (inclui esbuild) | ^5.3.4 | 8.3.4 | alta |
| `@typescript-eslint/eslint-plugin` | ^7.15.0 | 8.71.2 | alta |
| `@typescript-eslint/parser` | ^7.15.0 | 8.71.2 | alta |
| `tailwindcss` | ^3.4.7 | 4.3.3 | alta |

### Ordem sugerida (uma por vez, com `npm run build` e `npm run test` ao final de cada)

1. `vitest` + `@vitest/coverage-v8` (só testes; manter as duas versões iguais).
2. `@typescript-eslint/*` (exige configuração do ESLint, ver abaixo).
3. `vite` (verificar `vite.config.ts`, plugin React e versão do Node).
4. `tailwindcss` 4 (maior risco: mudança de configuração e de classes; testar visualmente todas as telas).

## Outras pendências

- ESLint sem configuração: `npm run lint` falha por falta de arquivo de config (já ocorria antes). Criar a config antes de atualizar `@typescript-eslint`.
- Validar manualmente as rotas no navegador após o `react-router-dom` 7 (os testes não cobrem a navegação).
- Dependabot do GitHub pode continuar mostrando alertas até reanalisar o repositório; conferir em `github.com/wp-software/p-pro/security/dependabot`.
- Aviso do build backend: 1 warning de nullable (não relacionado às dependências).
