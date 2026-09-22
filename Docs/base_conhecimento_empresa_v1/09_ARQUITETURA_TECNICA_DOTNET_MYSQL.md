# 09 — Arquitetura técnica — .NET/C# + MySQL

## 1. Stack baseline (implementada)

### Aplicação
- **.NET 10** (`net10.0`).
- **C# 14**.
- **ASP.NET Core 10**.
- **ASP.NET Core Razor Pages** para UI web em C# (revisão de ADR-0004; Blazor Web App era a baseline original, não foi o que se implementou — ver `21_ADRS_E_DECISOES_ABERTAS.md`).
- **SignalR** para notificações/atualizações em tempo real quando necessário (ainda não exercitado em telas de produção).
- **Dapper + MySqlConnector** como caminho de persistência (ADR revisada — `Persistence:Provider` nunca esteve ativo; banco acessado via SQL direto nos repositórios).
- **FluentMigrator** para migrações SQL versionadas (29 migrations: `M20260917_01` a `M20260922_29`).
- `System.Text.Json` para serialização.
- `Microsoft.Extensions.*` para DI, configuração, logging e options.
- Bootstrap 5 + Bootstrap Icons (locais, sem CDN) e Design System próprio (`css/tokens.css`, `css/base.css`, componentes em `css/components/*`).

### Banco
- **MySQL 8.4 LTS** como baseline de produção.
- InnoDB.
- UTF8MB4.
- timezone persistido em UTC; conversão na UI.

### IA / LLM (Fase 17)
- Provedores desacoplados em `llm_providers`/`llm_model_configs` com protocolos `OpenAICompatible` e `AnthropicMessages`.
- Interfaces `ILlmProviderResolver`, `ILlmModelCatalog` (`LlmModelEntry`), `ISecretStore` (chave `llm_apikey_{providerCode}`; implementada por `ProtectedFileSecretStore` via ASP.NET Core Data Protection — criptografado em repouso em `App_Data/Secrets/`, com fallback de configuração `Llm:{providerCode}:ApiKey`).
- Catálogo dinâmico de modelos consultando a API do provedor no painel `Settings/LlmProviders`.

### Observabilidade (direção)
- OpenTelemetry.
- logs estruturados.
- métricas.
- tracing distribuído para integrações.

### Testes (implementado)
- xUnit com **105 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (Testcontainers/MySQL).
- Playwright for .NET para E2E da UI na direção futura.

## 2. Estilo arquitetural

### Monólito modular
Escolha inicial recomendada e adotada.

Motivos:
- domínio ainda vai amadurecer;
- transações entre módulos são frequentes;
- menor complexidade operacional;
- implantação simples;
- refatoração mais fácil;
- evita microserviços prematuros.

Módulos devem possuir limites claros e não acessar tabelas internas de outro módulo de forma arbitrária.

## 3. Estrutura da solution (real)

```text
TraceCore.sln
src/
  TraceCore.Web/              # Razor Pages UI (Pages/, css Design System, wwwroot)
  TraceCore.Api/              # endpoints HTTP externos/internos
  TraceCore.Application/      # casos de uso e serviços
  TraceCore.Domain/           # entidades, regras puras e contratos de serviço
  TraceCore.Infrastructure/   # MySQL, migrations (FluentMigrator), repositórios, serviços Llm
  TraceCore.Contracts/        # DTOs/eventos públicos
  TraceCore.Worker/           # jobs, indexação, agregações
  TraceCore.Shared/           # somente abstrações realmente comuns
tests/
  TraceCore.IntegrationTests/ # 105 testes aprovados (MySQL/Testcontainers)
```

Se o repositório preferir vertical slices, módulos podem ser subdivididos internamente sem quebrar essa separação macro.

## 4. Módulos lógicos

- Identity
- Organization
- Catalog
- Cases
- Knowledge
- Diagnostics
- Search
- Analytics
- Audit
- Integrations
- Ai
- Administration
- Notifications (pendente — ADR-P009)

Cada módulo deve expor comandos/queries/serviços públicos e ocultar detalhes de persistência.

## 5. Fluxo de uma requisição

```text
Razor Pages/API
  -> Authorization
  -> Application Use Case
  -> Domain Rules
  -> Repository/Query Service
  -> MySQL / Integration
  -> Domain Event / Outbox
  -> Response
```

UI não acessa banco diretamente.

## 6. Padrão de aplicação

Usar commands e queries explicitamente, sem necessidade de introduzir framework CQRS pesado.

Exemplos:
- `CaseService` (abertura, edição, reabertura, encerramento);
- `CaseInvestigationService` (hipóteses, evidências, passos diagnósticos);
- `KnowledgeService` (rascunho, revisão, publicação, uso);
- `ManagementAnalyticsService`/`IManagementAnalyticsRepository` (analytics determinístico);
- `InvestigationCopilotService` (RAG grounded);
- `DiagnosticEngineService` (motor de diagnóstico guiado).

Handlers devem:
1. validar autorização;
2. validar input;
3. carregar estado necessário;
4. aplicar regra;
5. persistir atomicamente;
6. gerar evento/outbox;
7. registrar auditoria conforme regra.

## 7. Persistência

### Dapper/SQL direto
Não espalhar SQL em componentes da UI. Consultas concentradas em repositórios.

Organização real:
```text
Infrastructure/Persistence/
  Migrations/          # FluentMigrator (29 migrations)
  Repositories/
  Queries/
  TypeHandlers/
```

### Transações
Unidade de trabalho por caso de uso, com uma conexão/transação.

### Concorrência
Entidades editáveis críticas possuem `row_version` numérico (optimistic concurrency).

### Soft delete
Aplicar apenas quando a regra exigir preservação histórica. Preferir estados (`Inactive`, `Archived`) quando semanticamente melhores.

## 8. Outbox

Eventos que disparam processamento assíncrono devem usar Transactional Outbox no MySQL.

Exemplos:
- publicação de conhecimento;
- caso resolvido;
- atualização de índice de busca;
- agregação analítica;
- notificação;
- indexação RAG.

O evento e a alteração principal são gravados na mesma transação.

## 9. Jobs

Worker `.NET` separado (`TraceCore.Worker`), mesmo monorepo/deploy lógico inicialmente.

Responsabilidades:
- consumir outbox;
- agregações analíticas;
- revisão vencida;
- indexação;
- integração externa;
- limpeza de dados temporários;
- health checks programados.

Jobs precisam ser idempotentes e ter política de retry/backoff.

## 10. Cache

Não tornar Redis obrigatório no MVP. Usar cache local para metadados de baixa volatilidade quando útil. Introduzir cache distribuído apenas mediante necessidade medida.

## 11. Arquivos e anexos

Não armazenar arquivos grandes diretamente no MySQL por padrão.

Banco guarda:
- ID;
- nome;
- hash;
- tamanho;
- MIME;
- classificação;
- storage key;
- autor;
- entidade relacionada.

Storage físico deve ser abstrato por `IFileStorage`.

## 12. Feature flags

Recursos de IA, conectores e diagnósticos experimentais devem poder ser habilitados por configuração/feature flag. Permissões granulares do sistema controlam a exposição de funcionalidades (ex.: `ia.usar`, `catalogo.gerenciar`, `integracao.gerenciar`).

## 13. APIs internas e externas

- REST JSON para integrações e automação (endpoints atuais em `/api/cases/...` no pipeline do `TraceCore.Web`; ver ADR-P011).
- endpoints versionados `/api/v1/...` como contrato alvo caso a API seja formalizada em projeto dedicado.
- Problem Details RFC 9457 para erros HTTP.
- idempotency key em operações externas de criação quando necessário.
- correlação por `trace_id`/`correlation_id`.

## 14. Decisão importante sobre EF Core

A baseline deste documento usa Dapper/SQL direto para reduzir risco de compatibilidade entre .NET 10/EF Core 10 e providers MySQL. ADR revisada: `Persistence:Provider` nunca esteve ativo; o domínio não depende de EF Core (`src/TraceCore.Domain` sem referência a banco).

