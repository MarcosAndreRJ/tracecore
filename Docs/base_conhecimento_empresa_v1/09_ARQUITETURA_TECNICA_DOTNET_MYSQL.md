# 09 — Arquitetura técnica — .NET/C# + MySQL

## 1. Stack baseline

### Aplicação
- **.NET 10 LTS**.
- **C# 14**.
- **ASP.NET Core 10**.
- **ASP.NET Core Razor Pages** para UI web em C# (revisão de ADR-0004; Blazor Web App era a baseline original, não foi o que se implementou — ver `21_ADRS_E_DECISOES_ABERTAS.md`).
- **SignalR** para notificações/atualizações em tempo real quando necessário.
- **Dapper + MySqlConnector** como caminho de persistência baseline, evitando dependência crítica de compatibilidade de provider EF.
- **FluentMigrator ou DbUp** para migrações SQL versionadas; escolher um via ADR.
- `System.Text.Json` para serialização.
- `Microsoft.Extensions.*` para DI, configuração, logging e options.

### Banco
- **MySQL 8.4 LTS** como baseline de produção.
- InnoDB.
- UTF8MB4.
- timezone persistido em UTC; conversão na UI.

### Observabilidade
- OpenTelemetry.
- logs estruturados.
- métricas.
- tracing distribuído para integrações.

### Testes
- xUnit.
- FluentAssertions ou assertions nativas — decidir via ADR.
- Testcontainers for .NET com MySQL para integração.
- Playwright for .NET para E2E da UI.

## 2. Estilo arquitetural

### Monólito modular
Escolha inicial recomendada.

Motivos:
- domínio ainda vai amadurecer;
- transações entre módulos são frequentes;
- menor complexidade operacional;
- implantação simples;
- refatoração mais fácil;
- evita microserviços prematuros.

Módulos devem possuir limites claros e não acessar tabelas internas de outro módulo de forma arbitrária.

## 3. Estrutura da solution

```text
KnowledgePlatform.sln
src/
  KnowledgePlatform.Web/             # Razor Pages / composição (ver ADR-0004)
  KnowledgePlatform.Api/             # endpoints HTTP externos/internos
  KnowledgePlatform.Application/     # casos de uso
  KnowledgePlatform.Domain/          # domínio e regras puras
  KnowledgePlatform.Infrastructure/  # MySQL, arquivos, integrações
  KnowledgePlatform.Contracts/       # DTOs/eventos públicos
  KnowledgePlatform.Worker/          # jobs, indexação, agregações
  KnowledgePlatform.Shared/          # somente abstrações realmente comuns
tests/
  KnowledgePlatform.Domain.Tests/
  KnowledgePlatform.Application.Tests/
  KnowledgePlatform.IntegrationTests/
  KnowledgePlatform.E2E.Tests/
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
- Notifications

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
- `CreateCaseCommand`
- `AddDiagnosticStepCommand`
- `ResolveCaseCommand`
- `PublishKnowledgeItemCommand`
- `SearchKnowledgeQuery`
- `GetCaseTimelineQuery`

Handlers devem:
1. validar autorização;
2. validar input;
3. carregar estado necessário;
4. aplicar regra;
5. persistir atomicamente;
6. gerar evento/outbox;
7. registrar auditoria conforme regra.

## 7. Persistência

### Dapper
Usar para comandos e consultas explícitas. Não espalhar SQL em componentes Blazor.

Organização:
```text
Infrastructure/Persistence/
  Migrations/
  Repositories/
  Queries/
  TypeHandlers/
```

### Transações
Criar abstração `IUnitOfWork` para uma conexão/transação por caso de uso quando necessário.

### Concorrência
Entidades editáveis críticas devem possuir `row_version` numérico ou estratégia equivalente de optimistic concurrency.

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

Worker .NET separado, mas mesmo monorepo/deploy lógico inicialmente.

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

Recursos de IA, conectores e diagnósticos experimentais devem poder ser habilitados por configuração/feature flag.

## 13. APIs internas e externas

- REST JSON para integrações e automação.
- endpoints versionados `/api/v1/...`.
- Problem Details RFC 9457 para erros HTTP.
- idempotency key em operações externas de criação quando necessário.
- correlação por `trace_id`/`correlation_id`.

## 14. Decisão importante sobre EF Core

A baseline deste documento usa Dapper/MySqlConnector para reduzir risco de compatibilidade entre .NET 10/EF Core 10 e providers MySQL no momento inicial do projeto. Se a equipe quiser EF Core, executar spike técnico e registrar ADR com provider, versão, suporte, migrações, concorrência e testes. O domínio não deve depender de EF Core.

