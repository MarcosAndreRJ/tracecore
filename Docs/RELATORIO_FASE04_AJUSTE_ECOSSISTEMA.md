# RELATÓRIO — FASE 04: SISTEMAS — CADASTRO, EDIÇÃO E RELACIONAMENTOS NA TELA DE DETALHE (Ajuste do Ecossistema)

> Data: 21/09/2026 · Projeto: TraceCore · Prompt: `Docs/prompts/ajuste_modulos/04_sistemas_cadastro_e_relacionamentos.md`
> Regras seguidas: uma fase por vez, build + testes + validação manual na aplicação antes de avançar, sem commit/push.
> Confirmação de pré-requisito: Fases 01–03 implementadas e com testes verdes (167/167).

---

## 1. Arquivos alterados

**Novos:**
- `src/TraceCore.Infrastructure/Migrations/M20260921_25_AddSystemTypeToProductTechnicalProfiles.cs` — coluna `system_type` VARCHAR(50) NULL em `product_technical_profiles` (aditiva, idempotente, mesmo padrão de `responsibility/hosting_location/direction` da Fase 01).
- `tests/TraceCore.IntegrationTests/ProductCrudAndDetailsTabsIntegrationTests.cs` — 8 testes (ver item 4).

**Alterados:**
- `src/TraceCore.Domain/Entities/ProductTechnicalContextEntities.cs` — propriedade `SystemType` (string aberta) + constante `ValidSystemTypes` (`Desktop, Web, Mobile, Api, Servico, SaaS, Outro`).
- `src/TraceCore.Application/DTOs/ProductTechnicalContextDtos.cs` — `ProductTechnicalProfileDto` e `UpsertProductTechnicalProfileCommand` com `SystemType` **no fim** e default `null` (compat retroativa: construtores posicionais existentes não quebram).
- `src/TraceCore.Application/Services/ProductTechnicalContextService.cs` — validação de `SystemType` (case-insensitive contra `ValidSystemTypes`), passagem no `ToDto`, e **novo método batch** `GetTechnicalProfilesForAsync(IReadOnlyList<long>)` → `IReadOnlyDictionary<long, ProductTechnicalProfileDto>`.
- `src/TraceCore.Domain/Repositories/IProductTechnicalContextRepository.cs` + `MySqlProductTechnicalContextRepository.cs` — `GetProfilesByProductIdsAsync` (1 query `IN`, sem N+1) e `system_type` no SELECT/INSERT/UPSERT; `InMemoryProductTechnicalContextRepository` — impl batch (a entidade inteira já persistia sozinha).
- `src/TraceCore.Application/Services/IIntegrationService.cs` + `IntegrationService.cs` — **novo método** `LinkIntegrationToProductAsync(long id, long productId, long? currentUserId, ct)` com auditoria `integration.link_to_product`, simétrico e idempotente (no-op se já vinculada ao mesmo produto, sem ruído de auditoria).
- `src/TraceCore.Web/Pages/Catalog/Products/Index.cshtml.cs` + `Index.cshtml` — cadastro e edição com **2 seções** (Identificação + Contexto Técnico Básico: Tipo do Sistema, Tecnologia Principal (→ `BackendStack`), Banco Principal, Hospedagem, Finalidade, Resumo da Arquitetura). Handler `OnPostCreateAsync` cria o Sistema e o perfil técnico inicial na **mesma submissão**; `OnPostUpdateAsync` edita Sistema + perfil preservando campos ricos não expostos (carrega o perfil existente — ver item 3.d). Modal de edição migrado de JS compartilhado para **server-rendered por linha** (elimina o risco de injeção do `Html.Raw` legado e já é o padrão das Fases 02/03). Badge de Tipo do Sistema na tabela.
- `src/TraceCore.Web/Pages/Catalog/Products/Details.cshtml.cs` — injetados `IIntegrationService`, `IIntegrationHealthCheckService`, `IDepartmentService`; cargas novas no `OnGetAsync`: `Components` (entidades completas), `ComponentTypesList`, `DepartmentsList`, `Integrations` (DTOs completos com runs, filtradas por `ProductId`), `CandidateIntegrations` (não vinculadas a este sistema), `IntegrationTypesList`; inputs `NewComponent/EditComponent/NewIntegration/EditIntegration`; handlers das abas: `OnPostAddComponentAsync`, `OnPostEditComponentAsync`, `OnPostDeactivateComponentAsync`, `OnPostUnlinkComponentAsync`, `OnPostAddIntegrationAsync` (cria já vinculada), `OnPostEditIntegrationAsync` (merge preserva `ProductId`), `OnPostLinkExistingIntegrationAsync`, `OnPostUnlinkIntegrationAsync`, `OnPostTestIntegrationHealthCheckAsync`; `SystemType`/`ValidResponsibilities`/`ValidHostingLocations`/`ValidDirections` expostos ao Razor; campo `ProfileForm.SystemType` (aba Contexto Técnico).
- `src/TraceCore.Web/Pages/Catalog/Products/Details.cshtml` — abas **Componentes** e **Integrações** operacionais (ver item 2); aba Contexto Técnico com select de Tipo do Sistema.

## 2. Critérios do prompt × entrega

| Critério | Entrega |
|---|---|
| **Criar** sistema passa a ter contexto técnico básico | Aba "Contexto Técnico Básico" no modal Novo Sistema (6 campos). `OnPostCreateAsync` cria Sistema **e** perfil técnico inicial na mesma submissão (perfil vazio é válido). |
| **Editar == criar** (edição nunca expõe menos campos) | Modal Editar Sistema por linha **espelha** as mesmas 2 seções e **carrega** o perfil existente (mapa batch de perfis). |
| Aba **Componentes** operacional | Novo Componente (modal), Editar (modal por linha, catálogo + fallback legado), Inativar, Desvincular — tudo com `ProductId` = sistema atual. |
| Aba **Integrações** operacional | Nova Integração (modal; **`ProductId` pré-preenchido** = sistema atual), Editar, **Vincular Existente** (lista candidatas preservando histórico), Desvincular, **Ver histórico** (runs inline) e **Testar** (health-check real reaproveitando a Fase 15/M10). |
| Testes | 8 testes novos (ver item 4). |

## 3. Decisões registradas

**a) `SystemType` como string controlada, sem catálogo.** Reaproveita o modelo das Fases 01–03: campo curto de baixa cardinalidade, validado em Application (`ProductTechnicalProfile.ValidSystemTypes`, case-insensitive), sem tabela/catálogo — evita a complexidade de catálogo (que só se justifica quando o domínio pede gestão de entradas). Fallback "(legado)" não é necessário aqui (não havia dados pré-existentes), mas a validação tolera apenas o conjunto fechado.

**b) Orquestração no page model** (recomendação explícita do prompt) — o `Products/Index` injeta `IProductTechnicalContextService` e orquestra Sistema + perfil, sem cruzar serviços no backend nem criar "factory de cadastro completo".

**c) `LinkIntegrationToProductAsync` dedicado em `IIntegrationService`** — simétrico ao `UnlinkIntegrationFromProductAsync` da Fase 02, com auditoria própria `integration.link_to_product` (before/after = `productId`), idempotente e sem apagar histórico de runs. Não foi reaproveitada `UpdateIntegrationAsync` para esse fluxo: vincular é uma operação de relacionamento com semântica e auditoria distintas (a Fase 02 já separou vínculo de edição ao desvincular).

**d) Preservação de campos ricos ao editar pelo modal básico.** O upsert sobrescreve as colunas enviadas; o modal de edição expõe só os 6 campos básicos. Se o `OnPostUpdateAsync` mandasse `null` nos demais, edições pela lista apagariam `FrontendStack/RuntimePlatform/…/ExternalResearchPolicy` preenchidos na aba Contexto Técnico. Solução: o handler carrega o perfil existente e **carrega adiante** os campos não expostos (`existingProfile?.…`). Validado E2E (ver item 5: `runtime_platform` setado fora do modal sobreviveu à edição).

**e) Gate único da tela de detalhe.** Mantido `[Authorize(Policy = "catalogo.gerenciar")]` na `Details` para as ações de componentes/integrações: quem gerencia o catálogo gerencia os relacionamentos, e a página já estava sob esse gate. Não foi criada política nova (decisão consciente — pode ser revisitada se o produto pedir segregação).

**f) "Testar" e "Ver histórico" reaproveitam o que a Fase 02/M10 construiu** — health-check via `IIntegrationHealthCheckService.ExecuteHealthCheckAsync` (grava run determinística, falha segura) e histórico dos `Runs` do próprio DTO (sem nova query).

## 4. Testes

Novo arquivo `tests/TraceCore.IntegrationTests/ProductCrudAndDetailsTabsIntegrationTests.cs` (8 testes):

1. `CreateProduct_WithBasicTechnicalContext_CreatesProfileWithSystemType` — cadastro com contexto cria perfil; `SystemType` aparece no perfil **e** no contexto consolidado do Copiloto.
2. `CreateProduct_IdentificationOnly_DoesNotRequireTechnicalFields` — só identificação continua funcionando; nenhum campo novo é obrigatório.
3. `UpsertTechnicalProfile_WithInvalidSystemType_Throws` — valor fora do conjunto → `ArgumentException`.
4. `GetTechnicalProfilesFor_ReturnsMapOnlyForExistingProfiles` — batch retorna só produtos com perfil (base do modal de edição).
5. `LinkIntegrationToProduct_LinksPreservesRunsAndAudits` — vincula, preserva runs, audita `integration.link_to_product` com `ActorUserId` e `productId`.
6. `LinkIntegrationToProduct_ThenUnlink_RoundTripPreservesHistory` — vincular + desvincular; histórico de execuções preservado.
7. `LinkIntegrationToProduct_Idempotent_DoesNotDuplicateAudit` — segundo vínculo ao mesmo produto não gera evento (1 no total).
8. `Component_AddEditDeactivateUnlink_FromProductPersist` — criar com `ProductId`, editar preservando vínculo, inativar mantendo na aba, desvincular saindo da aba mas permanecendo no catálogo geral.

**Resultado:** build Release 0 erros (avisos pré-existentes NU1903/NU1904 apenas); suíte completa **175/175 aprovados** (167 da Fase 03 + 8 novos), 0 falhas.

## 5. Validação manual (app real)

- App em Development (`http://localhost:5098`), **migration `2026092125` aplicada** no startup, login admin.
- `GET /Catalog/Products/Index` 200 — modais Novo/Editar com "Contexto Técnico Básico" (select `newProdSystemType`), modais `editProductModal-…`, badge de Tipo do Sistema na tabela.
- **POST E2E criar Sistema com contexto** (PRD-F4-01): 302 → página refletiu; banco: produto id 7 + perfil com `system_type=Web`, `backend_stack`, `primary_database`, `hosting_model`, `business_purpose`, `architecture_summary`.
- **POST E2E editar** via modal da lista (Web→Api, nome v2): persistido **e** `runtime_platform='.NET 10 RUNTIME'` (setado fora do modal) **preservado** — valida a decisão 3.d.
- `GET /Catalog/Products/Details/7` — abas com estrutura operacional (modais de adicionar/editar, "Vincular Integração Existente", histórico, testar).
- **E2E aba Componentes**: Novo Componente (`CMP-F4-01`, tipo `Api`) → Editar (nome v2) → banco refletido; auditoria `component.create`/`component.update` (ids 64/7, actor 1).
- **E2E aba Integrações**: Nova Integração (`INT-F4-NOVA`) criada **já vinculada** ao sistema; Editar (tipo→`Webhook`, direção→`Saida`, notas de contrato); **Testar** (sem URL configurada → run Failed "URL de health-check não configurada", trigger `Automated`); **Desvincular → Vincular existente** (round-trip). Banco: integração id 16 re-vinculada; auditoria `integration.create`/`integration.update`/`integration.unlink_from_product`/`integration.link_to_product`.
- Dados de teste restaurados (produto 7 + perfil + componente 64 + integração 16 e runs removidos); app encerrada, porta 5098 livre.

## 6. Pendências para fases seguintes

- Fase 05 (`Docs/prompts/ajuste_modulos/05_integracoes_no_diagnostico.md`): integrações no diagnóstico.
- Pendência herdada da Fase 03: convergir tipos legados de componente ao catálogo (decisão de produto) — item 6 do relatório Fase 03.
- `tc-badge-warning` segue **sem estilo CSS** (usada na UI de Fases anteriores; fora do escopo desta fase — uso neste relatório foi substituído por `tc-badge-medium` na nova aba).

---

**Próximo passo:** aguardando confirmação para iniciar a **Fase 05** (`Docs/prompts/ajuste_modulos/05_integracoes_no_diagnostico.md`), seguindo as mesmas regras.