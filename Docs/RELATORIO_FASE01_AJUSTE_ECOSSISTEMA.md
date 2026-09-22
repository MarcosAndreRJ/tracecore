# RELATÓRIO — FASE 01: MODELAGEM E CATÁLOGOS ADMINISTRÁVEIS (Ajuste do Ecossistema)

> Data: 21/09/2026 · Projeto: TraceCore · Prompt: `Docs/prompts/ajuste_modulos/01_modelagem_e_catalogos.md`
> Regras seguidas: uma fase por vez, build + testes + validação manual na aplicação antes de avançar, sem commit/push.

---

## 1. Arquivos criados/alterados

**Criados:**
- `src/TraceCore.Domain/Entities/CatalogTypeEntities.cs` — entidades `ComponentType` e `IntegrationType` (novo arquivo dedicado — ver decisão no item 3).
- `src/TraceCore.Infrastructure/Migrations/M20260921_24_AddComponentAndIntegrationTypeCatalogs.cs` — migration aditiva + seed idempotente.
- `tests/TraceCore.IntegrationTests/CatalogTypesAndIntegrationAuditTests.cs` — 7 testes novos (ver item 7).

**Alterados:**
- `src/TraceCore.Domain/Entities/IntegrationEntities.cs` — campos `Responsibility`/`HostingLocation`/`Direction` (`string?`) + constantes `ValidResponsibilities`, `ValidHostingLocations`, `ValidDirections` (padrão `ProductTechnicalProfile.ValidExternalResearchPolicies`); construtor ganhou 3 parâmetros opcionais no fim (compatibilidade preservada).
- `src/TraceCore.Domain/Repositories/ICatalogRepository.cs` — `GetComponentTypesAsync`, `GetComponentTypeByIdAsync`, `AddComponentTypeAsync`, `UpdateComponentTypeAsync`.
- `src/TraceCore.Domain/Repositories/IIntegrationRepository.cs` — `GetIntegrationTypesAsync`, `GetIntegrationTypeByIdAsync`, `AddIntegrationTypeAsync`, `UpdateIntegrationTypeAsync`.
- `src/TraceCore.Application/DTOs/IntegrationDtos.cs` — `CreateIntegrationCommand` e `IntegrationDto` com `Responsibility`/`HostingLocation`/`Direction` (parâmetros opcionais no fim); `ConfigureIntegrationHealthCheckCommand` ganhou `UpdatedBy` (necessário para a auditoria).
- `src/TraceCore.Application/Services/ICatalogService.cs` / `CatalogService.cs` — `GetComponentTypesAsync`, `CreateComponentTypeAsync`, `DeactivateComponentTypeAsync` (+ métodos privados de checagem de uso; `using System.Linq` adicionado).
- `src/TraceCore.Application/Services/IIntegrationService.cs` / `IntegrationService.cs` — mesmo trio para tipos de integração; `IAuditService` injetado e auditoria nas 4 operações existentes; validação dos 3 campos novos; `ToDto` propaga os campos.
- `src/TraceCore.Infrastructure/Persistence/Repositories/MySqlIntegrationRepository.cs` — SELECTs incluem `responsibility`/`hosting_location`/`direction` (aliases Dapper); INSERT e UPDATE incluem as 3 colunas; CRUD de `integration_types`.
- `src/TraceCore.Infrastructure/Persistence/Repositories/MySqlCatalogRepository.cs` — CRUD de `component_types` (reusa `GetAllComponentsAsync` já existente para a checagem de uso).
- `src/TraceCore.Infrastructure/Persistence/InMemory/InMemoryRepositories.cs` — `InMemoryDataStore`: `ComponentTypes`/`IntegrationTypes` (dicionários), `_componentTypeIdSeq`/`_integrationTypeIdSeq`, limpeza + zera no `Reset()`, seed espelhado da migration (14/18); integrações seedadas ganharam os 3 campos novos; `InMemoryCatalogRepository` e `InMemoryIntegrationRepository` com o mesmo CRUD.

**Sem alterações:** Nenhum `.cshtml` tocado (Fase 02/03). `product`/sistemas, `IntegrationRun`, `Docs/sql_mockup/*.sql` intocados.

## 2. Migration criada

`M20260921_24_AddComponentAndIntegrationTypeCatalogs.cs` (`[Migration(2026092124, ...)]` — confirmei que a última real era `M20260919_23`, portando a nova é a 24).

Resultado no banco real (MariaDB, verificado via query):
- `component_types` (id PK identity, code VARCHAR(64) UNIQUE, name VARCHAR(150), is_active BOOLEAN default TRUE, created_at) — **14 linhas** seedadas.
- `integration_types` (mesma estrutura) — **18 linhas** seedadas.
- `integrations` + `responsibility`/`hosting_location`/`direction` VARCHAR(50) NULL (3 colunas confirmadas).

Totamente aditiva: sem DROP/TRUNCATE; rodou sobre a base já populada (1.281 casos etc.) sem perda. Seed idempotente (`INSERT ... SELECT ... WHERE NOT EXISTS`) — reaplicar não duplica (contagens confirmadas exatas). `Down()` desfaz colunas e tabelas.

## 3. Modelagem de ComponentType/IntegrationType — catálogo administrável (tabela), NÃO string controlada

Decisão: **tabela própria** para os dois, porque a empresa precisa adicionar novos tipos sem deploy (ex.: "SAP IDoc" não existia como opção fixa). Detalhe resolvido:
- **Arquivo novo** `CatalogTypeEntities.cs` em vez de adicionar em `CatalogEntities.cs` — entidades de catálogo são conceito próprio (não são entidade de produto).
- **Colisão de nome resolvida**: a entidade chama-se `IntegrationType` mesmo, porque a propriedade `Integration.IntegrationType` (string) é membro de instância e não conflita com o tipo — comprovado compilando (0 erros). A preocupação do prompt não se materializou; manteve-se a convenção do projeto sem sufixo `Entity`.
- `ComponentEntity.ComponentType` e `Integration.IntegrationType` **continuam string aberta** (sem FK): compatibilidade com valores já gravados (`Service`, `Sap`, `Module` etc.) preservada; catálogo alimenta a UI nas Fases 02/03.

## 4. Responsibility/HostingLocation/Direction — strings controladas, não tabela

Modeladas como `string?` opcionais em `Integration` (null não quebra criação existente), validadas em Application com listas de valores aceitos em constantes estáticas (padrão do projeto, não enum fechado):
- `Responsibility`: `NossaEmpresa, Cliente, Terceiro`
- `HostingLocation`: `Empresa, Cliente, Terceiro, Cloud-SaaS, Hibrido`
- `Direction`: `Entrada, Saida, Bidirecional`

Validação **case-insensitive** de entrada; o valor é armazenado trimado como enviado. Valor fora da lista → `ArgumentException` com mensagem listando os aceitos. Indeliberadamente sem tabela: poucos valores estáveis, sem administração dinâmica.

## 5. Onde vivem os catálogos (repository próprio vs. existente)

Decisão documentada: **nos repositórios/serviços existentes** — `ICatalogRepository`/`ICatalogService` para `ComponentType`, `IIntegrationRepository`/`IIntegrationService` para `IntegrationType`. Motivos: conceito diretamente acoplado ao "dono" de cada catálogo (componentes vs. integrações), zero registros novos de DI, e as checagens de "em uso" já dependem dos repositórios de cada domínio (componentes/integrações) — uma interface própria atravessaria os dois lados sem benefício. Nenhuma alteração em `DependencyInjection.cs` foi necessária (confirmado).

## 6. IntegrationService agora audita tudo (correção da dívida técnica)

`IAuditService` injetado (resolvido sem novo registro). As 4 operações existentes gravam antes/depois sem dado sensível:
- `integration.create` (entity `integrations`)
- `integration.status_update` (entity `integrations`)
- `integration.healthcheck_configure` (entity `integrations`)
- `integration.run_register` (entity `integration_runs`)

Novas ações de catálogo auditadas em `CatalogService`/`IntegrationService`: `component_type.create`, `component_type.deactivate`, `integration_type.create`, `integration_type.deactivate`.

**Política escolhida e testada para inativação:** bloquear inativação de tipo **em uso** (lança `InvalidOperationException` "…não pode ser inativado…"), comparação case-insensitive contra o `Code` OU o `Name` gravado nos cadastros. Exclusão física nunca é permitida.

## 7. Build e testes

- `dotnet build src\TraceCore.Web\TraceCore.Web.csproj -c Release`: **0 erros**, 18 warnings (NU1903/NU1904 pré-existentes do pacote `System.Security.Cryptography.Xml` 10.0.0 / DataProtection — não relacionados a esta fase).
- Suíte completa `tests\TraceCore.IntegrationTests`: **158/158 aprovados, 0 falhas** — incluindo os **7 testes novos** desta fase (criar/inativar catálogo com auditoria; bloqueio ao inativar em uso; aceite/validação dos 3 campos; auditoria das 4 operações de integração).

## 8. Validação no banco real + aplicação

- App iniciada em `Development` (AutoMigrate=true): log confirma **"Migrations executadas com sucesso"**; `http://localhost:5098/login` responde 200.
- Banco real verificado por query: `component_types` 14 linhas (`Module` ativo presente — já em uso na massa), `integration_types` 18 linhas (`Sap` presente), 3 colunas novas em `integrations`, sem duplicidade de seeds.
- Massa de dados intocada: casos (1.281), sequência e audit continuam íntegros (audit cresceu apenas por probes de login ambientais, sem relação com a fase).

## 9. Pendências/limitações — explicitamente para as Fases 02/03

- **Fase 02**: `Integrations/Index.cshtml:334-345` ainda com os 8 tipos fixos — trocar por `<select>` alimentado de `GetIntegrationTypesAsync`; implementar `UpdateIntegrationAsync` (edição completa + persistir os 3 campos novos no formulário); `IntegrationService` continua sem edição (já existe `UpdateIntegrationApi` no `MySqlIntegrationRepository`).
- **Fase 03**: `Catalog/Components/Index.cshtml:296-304` ainda com os 6 tipos fixos — trocar por `<select>` alimentado de `GetComponentTypesAsync`; UI para inativar tipos em catálogo.
- **Fase 04**: menu duplicado do Ecossistema em `_Layout.cshtml:173-193` não tratado nesta fase (nem deveria).
- Nenhum endpoint HTTP/API novo foi criado nesta fase (decisão: serviços chamados por controllers nas fases de UI).