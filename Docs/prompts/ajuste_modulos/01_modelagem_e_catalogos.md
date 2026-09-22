# FASE 01 — MODELAGEM E CATÁLOGOS ADMINISTRÁVEIS

> Execute esta fase primeiro. As fases 02, 03 e 04 dependem diretamente do que for criado aqui.
> Não pule para as fases seguintes sem terminar esta com build e testes passando.

---

## CONTEXTO

O TraceCore possui hoje a área **Ecossistema** (Sistemas / Componentes / Dependências / Integrações), construída nas Fases 2, 7.A e M10. Uso real da aplicação revelou que vários conceitos estão **hardcoded em `<select>` do Razor** quando deveriam ser catálogos administráveis, e que a entidade `Integration` está faltando campos estruturais que já existem conceitualmente no mundo real da empresa (responsabilidade, hospedagem, direção do fluxo).

Esta fase resolve a base de dados e domínio. **Não mexe em UX ainda** — isso é fase 02/03/04.

## OBJETIVO

1. Criar catálogo administrável de **Tipos de Componente** (`ComponentType`), substituindo a lista fixa hoje embutida no formulário de `Catalog/Components/Index.cshtml`.
2. Criar catálogo administrável de **Tipos de Integração** (`IntegrationType`), substituindo a lista fixa hoje embutida no formulário de `Integrations/Index.cshtml`.
3. Adicionar à entidade `Integration` os campos: **Responsabilidade/Fornecedor**, **Hospedagem/Local de Execução**, **Direção**.
4. Preparar `IAuditService` no `IntegrationService`, hoje **totalmente ausente** (confirmado — ver seção "Estado Atual Confirmado").

## ESTADO ATUAL CONFIRMADO

Confirmado lendo o código em `K:\Trabalho\Projetos\TraceCore` nesta análise (não presuma, releia antes de implementar — pode ter mudado):

- `src/TraceCore.Domain/Entities/CatalogEntities.cs:76-107` — `ComponentEntity.ComponentType` é `string` livre, sem tabela de apoio. Nenhuma outra tabela de "tipos de componente" existe no projeto.
- `src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml:296-304` — o `<select>` "Tipo de Componente" tem 6 opções fixas no HTML: `Service, Frontend, Database, Worker, Gateway, Integration`.
- `src/TraceCore.Domain/Entities/IntegrationEntities.cs:10-88` — `Integration.IntegrationType` é `string` livre. **Não existem** campos `Responsibility`, `HostingLocation` ou `Direction` na entidade hoje.
- `src/TraceCore.Web/Pages/Integrations/Index.cshtml:334-345` — o `<select>` "Tipo de Integração" tem 8 opções fixas: `Sap, Ticketing, Monitoring, Telemetry, Directory, Notification, Repository, Other`.
- `src/TraceCore.Application/Services/IntegrationService.cs` — **não injeta `IAuditService`** e não chama `RecordAsync` em nenhum lugar (confirmado via grep: zero ocorrências). Em contraste, `src/TraceCore.Application/Services/CatalogService.cs` audita tudo (`product.create`, `product.update`, `component.create`, `component.update`, `component.dependency_create`, `component.dependency_delete`, `component.owner_add`, `component.owner_delete`). O módulo de Integrações está sem trilha de auditoria desde que foi criado — corrigir aqui, não deixar para depois.
- Não existe hoje NENHUMA tabela de catálogo aberto reaproveitável para `ComponentType`/`IntegrationType`. Já existe `technologies` (Fase 6, ligada a Knowledge e a `product_technologies` do Prompt 2 do Copiloto) — **não é o mesmo conceito**, não reaproveitar por engano (tecnologia ≠ tipo de componente).
- Padrão de migration já em uso: FluentMigrator, arquivo `M{yyyyMMdd}_{seq}_{Descricao}.cs` em `src/TraceCore.Infrastructure/Migrations/`. A última migration hoje é `M20260919_23_GeneralizeAiSourcesForStructuredSearch.cs` — **confirme a última migration real antes de numerar a nova**, não copie esse número cegamente.
- Padrão de "catálogo aberto" já usado no projeto: tabela simples (id, code/name, description, is_active, created_at) sem enum C# fechado — ver `technologies` (`src/TraceCore.Infrastructure/Migrations/M20260918_08_CreateKnowledgeBaseSchema.cs`) e `product_technical_sources`/`SourceType` (Prompt 2 do Copiloto, string aberta validada em código, não enum).

## ESCOPO

- Migration aditiva criando `component_types` e `integration_types` (catálogos administráveis).
- Migration aditiva adicionando à tabela `integrations`: `responsibility`, `hosting_location`, `direction` (strings controladas, não FK — ver decisão abaixo).
- Entidades de domínio: `ComponentType`, `IntegrationType` (sem sufixo `Entity`, seguindo convenção do projeto — `Product`, `Integration`, `ComponentDependency` não têm sufixo).
- Repository(s) para os novos catálogos — avalie se cabe em `ICatalogRepository`/`IIntegrationRepository` existentes ou se merece interface própria (`ICatalogTypeRepository` ou equivalente). Decida e documente a decisão na entrega final; não deixe ambíguo.
- Métodos de leitura/escrita administrável dos catálogos (listar, criar, inativar — não permitir exclusão física de um tipo já em uso).
- Seed inicial dos tipos (na própria migration, via `Execute.Sql`, idempotente com `WHERE NOT EXISTS`) usando exatamente os valores já hoje hardcoded na UI (para não quebrar nada que já existe), mais os exemplos do mundo real da empresa citados no pedido original:
  - Tipos de Componente: `Service, Frontend, Database, Worker, Gateway, Integration` (já existentes) + `Módulo Desktop, API, Serviço Windows, Aplicativo Mobile, Adaptador de Integração, Infraestrutura, Outro`.
  - Tipos de Integração: `Sap, Ticketing, Monitoring, Telemetry, Directory, Notification, Repository, Other` (já existentes) + `REST API, SOAP, Webhook, SFTP, Arquivo, Banco de Dados, Fila/Mensageria, SAP RFC, SAP IDoc, EDI`.
- Adicionar `IAuditService` ao `IntegrationService` e auditar todas as operações que já existem hoje (`CreateIntegrationAsync`, `UpdateIntegrationStatusAsync`, `ConfigureHealthCheckAsync`, `RegisterRunAsync`) — isso é uma correção de dívida técnica que pertence a esta fase (schema/domínio/auditoria), não à fase de UX.
- Testes automatizados cobrindo os catálogos novos e a auditoria adicionada ao `IntegrationService`.

## NÃO ESCOPO

- Não alterar `Catalog/Components/Index.cshtml` nem `Integrations/Index.cshtml` para consumir os novos catálogos via `<select>` dinâmico — isso é Fase 02 (Integrações) e Fase 03 (Componentes).
- Não implementar `UpdateIntegrationAsync` (edição completa da integração) — isso é Fase 02.
- Não mexer em `Product`/Sistemas — isso é Fase 04.
- Não mexer em `IntegrationRun`/diagnóstico — isso é Fase 05.
- Não popular/alterar `Docs/sql_mockup/*.sql`.

## DECISÃO CONCEITUAL — CATÁLOGO VS. STRING CONTROLADA

Confirmado no pedido original e reforçado pela análise do código real: nem tudo precisa de tabela.

- `ComponentType` e `IntegrationType` → **catálogo administrável** (tabela própria), porque a empresa precisa poder adicionar novos tipos sem deploy (ex.: "SAP IDoc" hoje nem existe como opção).
- `Responsibility` (NossaEmpresa/Cliente/Terceiro), `HostingLocation` (Empresa/Cliente/Terceiro/Cloud-SaaS/Hibrido), `Direction` (Entrada/Saida/Bidirecional) → **strings controladas simples**, validadas em `Application` (lista de valores aceitos em uma constante, não enum C# fechado, seguindo o padrão já usado em `Integration.Status`/`IntegrationRun.Status`/`ComponentOwner.OwnershipRole`). Não criar tabela para esses três — são poucos valores estáveis, sem necessidade de administração dinâmica pela empresa.

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Domain/Entities/CatalogEntities.cs
src/TraceCore.Domain/Entities/IntegrationEntities.cs
src/TraceCore.Domain/Entities/Technology.cs               (referência de padrão "catálogo simples")
src/TraceCore.Domain/Repositories/ICatalogRepository.cs
src/TraceCore.Domain/Repositories/IIntegrationRepository.cs
src/TraceCore.Application/Services/ICatalogService.cs / CatalogService.cs
src/TraceCore.Application/Services/IIntegrationService.cs / IntegrationService.cs
src/TraceCore.Application/Services/IAuditService.cs
src/TraceCore.Infrastructure/Persistence/Repositories/MySqlCatalogRepository.cs
src/TraceCore.Infrastructure/Persistence/Repositories/MySqlIntegrationRepository.cs
src/TraceCore.Infrastructure/Persistence/InMemory/InMemoryRepositories.cs  (InMemoryDataStore + InMemoryCatalogRepository + InMemoryIntegrationRepository — precisa espelhar tudo que for feito em MySql)
src/TraceCore.Infrastructure/Migrations/M20260918_17_CreateIntegrationsSchemaAndPermission.cs   (schema atual de integrations)
src/TraceCore.Infrastructure/Migrations/M20260918_08_CreateKnowledgeBaseSchema.cs               (padrão de catálogo simples: technologies)
src/TraceCore.Infrastructure/DependencyInjection.cs
src/TraceCore.Application/DependencyInjection.cs
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

Nova migration (descubra o próximo número real antes de codar):

```sql
CREATE TABLE component_types (
  id BIGINT PK AUTO_INCREMENT,
  code VARCHAR(64) UNIQUE NOT NULL,
  name VARCHAR(150) NOT NULL,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME NOT NULL
);

CREATE TABLE integration_types (
  id BIGINT PK AUTO_INCREMENT,
  code VARCHAR(64) UNIQUE NOT NULL,
  name VARCHAR(150) NOT NULL,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME NOT NULL
);

ALTER TABLE integrations
  ADD COLUMN responsibility VARCHAR(50) NULL,
  ADD COLUMN hosting_location VARCHAR(50) NULL,
  ADD COLUMN direction VARCHAR(50) NULL;
```

Seed idempotente (`INSERT ... WHERE NOT EXISTS`) dos valores listados em ESCOPO. Migration deve rodar em banco já populado (a massa de testes em `Docs/sql_mockup` já foi aplicada) sem apagar nada — só `CREATE TABLE`/`ALTER TABLE ADD COLUMN`, nunca `DROP`/`TRUNCATE`.

**Importante:** `ComponentEntity.ComponentType` e `Integration.IntegrationType` continuam sendo `string` (não virar FK obrigatória agora) — o catálogo serve para **alimentar a UI com opções administráveis** (Fase 02/03), não para forçar uma migração de dados imediata dos registros já existentes. Não quebrar compatibilidade com valores hoje já gravados no banco (`Service`, `Sap`, etc. continuam válidos).

### DOMAIN

- `src/TraceCore.Domain/Entities/CatalogTypeEntities.cs` (novo arquivo, ou adicionar em `CatalogEntities.cs` — decida e justifique): `ComponentType(Id, Code, Name, IsActive, CreatedAt)`.
- `src/TraceCore.Domain/Entities/IntegrationEntities.cs`: adicionar `IntegrationType` como entidade de catálogo (nome já ocupado pela propriedade `Integration.IntegrationType` — resolver colisão de nome, ex.: chamar a entidade `IntegrationTypeCatalogItem` ou mover a propriedade string para outro nome interno; decida com clareza e documente).
- `Integration`: adicionar `Responsibility`, `HostingLocation`, `Direction` (todas `string?`, opcionais — não quebrar criação de integrações existentes).
- Validação de valores aceitos para `Responsibility`/`HostingLocation`/`Direction` como constantes estáticas (mesmo padrão de `ProductTechnicalProfile.ValidExternalResearchPolicies` já usado no Prompt 2 do Copiloto — reaproveitar esse padrão).

### APPLICATION

- Métodos novos em `ICatalogService`/`CatalogService`: `GetComponentTypesAsync`, `CreateComponentTypeAsync`, `DeactivateComponentTypeAsync` (ou local equivalente que você decidir).
- Métodos novos em `IIntegrationService`/`IntegrationService`: `GetIntegrationTypesAsync`, `CreateIntegrationTypeAsync`, `DeactivateIntegrationTypeAsync`.
- Injetar `IAuditService` no `IntegrationService` (construtor) e auditar `CreateIntegrationAsync` (`integration.create`), `UpdateIntegrationStatusAsync` (`integration.status_update`), `ConfigureHealthCheckAsync` (`integration.healthcheck_configure`), `RegisterRunAsync` (`integration.run_register`) — seguir exatamente o padrão de `CatalogService` (`before`/`after` no `RecordAsync`, nunca dado sensível).

### INFRASTRUCTURE

- `MySqlCatalogRepository`/`MySqlIntegrationRepository`: métodos CRUD dos catálogos.
- `InMemoryCatalogRepository`/`InMemoryIntegrationRepository` (em `InMemoryRepositories.cs`) — espelhar os mesmos métodos, incluindo `InMemoryDataStore.Reset()` limpando as novas coleções (lição já aprendida no Prompt 2 do Copiloto: esquecer de adicionar ao `Reset()` quebra testes de forma intermitente e difícil de debugar).
- Registrar tudo em `DependencyInjection.cs` (blocos MySql e InMemory).

### WEB

Nada nesta fase — reforçado: **não** altere nenhum `.cshtml` ainda.

### AUDITORIA

Ações novas: `component_type.create`, `component_type.deactivate`, `integration_type.create`, `integration_type.deactivate`, `integration.create`, `integration.status_update`, `integration.healthcheck_configure`, `integration.run_register`.

### PERMISSÕES

Reutilizar `catalogo.gerenciar` (tipos de componente) e `integracao.gerenciar` (tipos de integração) — já existem, não criar permissão nova.

### TESTES

- Criar/inativar tipo de componente e de integração.
- Não permitir inativar um tipo em uso sem aviso (ou permitir e documentar a decisão — decida e teste o comportamento escolhido).
- `IntegrationService` agora grava evento de auditoria em cada uma das 4 operações existentes (teste que hoje inexiste porque a auditoria não existia).
- `Integration` aceita `Responsibility`/`HostingLocation`/`Direction` nulos (compatibilidade) e validados quando informados (rejeita valor fora da lista).
- Migration roda em banco com dados existentes sem erro (teste de integração real via `TraceCoreTestApplicationFactory`, InMemory já cobre o equivalente).

## CRITÉRIOS DE ACEITE

- Build limpo (`dotnet build`), 0 erros.
- Todos os testes existentes continuam passando (rode a suíte inteira antes de terminar, não só os testes novos).
- Testes novos desta fase passando.
- Migration aplicada com sucesso em banco real (rode a aplicação com `Persistence:AutoMigrate=true` e confirme no log `Migrations executadas com sucesso`).
- Nenhum dado da massa de teste (`Docs/sql_mockup`) foi apagado ou alterado.
- `IntegrationService` audita todas as suas operações.

## ENTREGA FINAL

Ao concluir, apresente:
1. arquivos criados/alterados;
2. migration criada (nome exato) e colunas/tabelas resultantes;
3. como `ComponentType`/`IntegrationType` foram modelados (tabela vs. string controlada — confirmando a decisão desta fase);
4. como `Responsibility`/`HostingLocation`/`Direction` foram modelados e quais valores aceitos;
5. decisão tomada sobre onde os novos catálogos vivem (repository próprio ou existente);
6. confirmação de que `IntegrationService` agora audita tudo;
7. resultado do build e dos testes;
8. limitações/pendências que ficam explicitamente para a Fase 02/03.

**NÃO pare após analisar.** Implemente de verdade, execute build, execute testes, corrija erros até passar. Não devolva uma versão reescrita deste prompt.
