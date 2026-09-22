# RELATÓRIO — FASE 02: INTEGRAÇÕES — CADASTRO, EDIÇÃO E RELACIONAMENTO COM SISTEMA (Ajuste do Ecossistema)

> Data: 21/09/2026 · Projeto: TraceCore · Prompt: `Docs/prompts/ajuste_modulos/02_integracoes_cadastro_edicao.md`
> Regras seguidas: uma fase por vez, build + testes + validação manual na aplicação antes de avançar, sem commit/push.

---

## 1. Arquivos alterados

**Nenhum arquivo novo de produção** — a Fase 02 reutilizou o CRUD e os catálogos da Fase 01 (nenhuma migration nova).

**Alterados:**
- `src/TraceCore.Application/DTOs/IntegrationDtos.cs` — novo record `UpdateIntegrationCommand` (`Id`, `Code`, `Name`, `IntegrationType`, `ProductId`, `TargetSystemDescription`, `OwnerDepartmentId`, `ContractNotes`, `Responsibility`, `HostingLocation`, `Direction`).
- `src/TraceCore.Application/Services/IIntegrationService.cs` — `UpdateIntegrationAsync(UpdateIntegrationCommand, long? currentUserId = null, CancellationToken)` e `UnlinkIntegrationFromProductAsync(long id, long? currentUserId = null, CancellationToken)`.
- `src/TraceCore.Application/Services/IntegrationService.cs` — implementação dos dois métodos; validação estrutural extraída para `ValidateStructuralFields` (reutilizada no create e no update); auditoria `integration.update` (before/after completos) e `integration.unlink_from_product` (antes + `{ productId: null }`; no-op silencioso se já desvinculada — não gera evento).
- `src/TraceCore.Web/Pages/Integrations/Index.cshtml.cs` — injeta `ICatalogService`; `ProductsList` (catálogo de sistemas) e `IntegrationTypesList` (catálogo de tipos); records `CreateIntegrationInput` (ganhou `ProductId`, `Responsibility`, `HostingLocation`, `Direction`; `IntegrationType` default `"Other"`) e `EditIntegrationInput`; handlers `OnPostCreateIntegrationAsync` (repassa os 4 campos novos), `OnPostUpdateIntegrationAsync`, `OnPostUnlinkIntegrationAsync`; `OnGetAsync` carrega as 4 listas.
- `src/TraceCore.Web/Pages/Integrations/Index.cshtml` — modal "Editar Integração" por card (server-rendered, padrão do modal de health-check); botão "Desvincular do Sistema" visível só quando `ProductId != null`; badges `Resp`/`Hosp`/`Direção` e legenda do sistema associado; `<select>` de tipo agora iterado do catálogo (`IntegrationTypesList`) no modal de criação; novo `<select>` de Sistema (`ProductsList`) + 3 selects dos campos estruturais (com opção "— Não informado —" + valores de `Integration.Valid*`); seção de histórico renomeada para "Histórico de Execuções e Verificações" e badge de origem `Automated` → "Verificação".

**Sem alterações:** nenhuma migration; `product`/sistemas, `IntegrationRun` intocados.

## 2. Critérios do prompt × entrega

| Critério | Entrega |
|---|---|
| Edição completa da integração | Modal `EditIntegration*` por card; `UpdateIntegrationAsync` persiste todos os campos editáveis (Código, Nome, Tipo, Sistema, Descrição, Departamento, Notas, Resp/Hosp/Direção). Status e health-check **não** entram nesse fluxo — têm handlers próprios (decisão de escopo). |
| Seleção de Sistema no cadastro | `<select>` com `ProductsList` no create e no edit (`ProductId`); opcional ("Sem sistema associado"). Nenhum produto é criado — apenas associação. |
| Select de tipo originado do catálogo | Create e Edit iteram `Model.IntegrationTypesList` (catálogo `integration_types` da Fase 01), em vez do `<select>` fixo em código. |
| Campos Responsabilidade/Hospedagem/Direção | Presentes no create e no edit, com opção vazia + valores das constantes `Integration.Valid*`. Persistidos (colunas da Fase 01). |
| Renomear histórico | Seção agora "Histórico de Execuções e Verificações"; origem `Automated` exibida como "Verificação" (badge `tc-badge-verified`, ícone heart-pulse); `Manual` continua "Manual". |
| Ação desvincular do sistema | Botão "Desvincular" quando associada; `UnlinkIntegrationFromProductAsync` zera `ProductId` **sem apagar a integração nem seus runs**; auditado como `integration.unlink_from_product`; idempotente. |
| Teste de função de contexto do Copiloto | Novo teste cobrindo `GetInvestigationContextAsync` refletindo a edição (ver item 5). |

## 3. Decisões registradas

**a) Fluxo de health-check NÃO usa `ExternalUrlSafetyValidator` (pendência documentada, não pendência ignorada).**
O prompt autoriza documentar explicitamente (não implementar sem justificar). `ExternalUrlSafetyValidator` (usado em *ExternalResearch*) resolve o host via `Dns.GetHostAddressesAsync` e bloqueia IP privado/loopback/link-local — exatamente o alvo legítimo de um health-check de integração **on-prem** (ex.: `http://192.168.0.10:8080/health` de um sistema ERP na rede interna). Aplicar esse validator ao health-check reverteria o modelo de ameaça (o inimigo é a injeção de URL *externa* pelo operador; aqui o operador valida o endpoint da própria infraestrutura). Controle existente mantido: `[Authorize(Policy = "integracao.gerenciar")]` na página. **Se as fases seguintes puserem health-check automático em malha de internet pública, reavaliar com uma variante que permita privado/loopback e bloqueie apenas resolução remota.**

**b) "Verificação" vs "Manual" (nomenclatura).** Hoje `TriggeredBy="Automated"` só é escrito por `IntegrationHealthCheckService.ExecuteHealthCheckAsync`; `"Manual"` vem de "Registrar Execução". Portanto mgly a badge `Automated` == health-check — sem alteração de dados necessária.

**c) Modal de edição por card (server-rendered)** em vez de modal compartilhado por JS (padrão de Products). Os campos `TargetSystemDescription`/`ContractNotes` têm texto livre (aspas/acentos); renderizar valores via `value="@integration.X"` — em atributo HTML — requer escape elemento-a-elemento. O modal de health-check já existente segue o padrão por card; manter o mesmo padrão na edição evita inconsistência de fuga de atributos e retrocompatibilidade com o padrão vigente. Trade-off: mais HTML por card (aceitável para volume de integrações).

**d) Edição não mexe em status nem em health-check.** Estes têm fluxos dedicados (ações "Marcar como Ativo/Inativo", "Testar Health-Check"); mesclá-los à edição aumentaria a superfície de regressão sem benefício.

## 4. Riscos/modelação verificados

- `UpdateIntegrationAsync` repassa para `IntegrationRepository.UpdateIntegrationAsync` (MySql já gravava todos os campos — base da Fase 01; InMemory persiste a entidade inteira). `ToDto` propaga os campos novos.
- Validação de edição: `Code` normalizado para maiúsculas (`Trim().ToUpperInvariant()`), `Name/TargetSystemDescription/ContractNotes/Responsibility/HostingLocation/Direction` trimados (vazios → `null`); `Responsibility`/`HostingLocation`/`Direction` validados contra `Integration.Valid*` (case-insensitive) via `ValidateStructuralFields`; departamento validado só quando informado; `IntegrationType` apenas obrigatório (o catálogo é a fonte de verdade, não uma lista fixa).
- A consulta do Copiloto (`ProductTechnicalContextService.GetInvestigationContextAsync`) lê do repositório em cada chamada — o teste de contexto edição→consulta cobre exatamente essa continuidade.

## 5. Testes

Novo arquivo `tests/TraceCore.IntegrationTests/IntegrationsCrudAndProductLinkIntegrationTests.cs` (5 testes, padrão `CatalogTypesAndIntegrationAuditTests`):

1. `Integration_CreateWithProduct_PersistsProductAssociation` — create com `ProductId` + 3 campos estruturais persiste no DTO.
2. `Integration_Edit_PersistsAllEditableFields` — edição de todos os campos reflete no DTO.
3. `Integration_Update_IsAuditedWithBeforeAfter` — `integration.update` com `ActorUserId`, `BeforeJson` (nome/tipo originais) e `AfterJson` (novos).
4. `Integration_UnlinkFromProduct_ZeroesProductKeepsRunsAndAudits` — desvincula: `ProductId` null, runs preservados, evento `integration.unlink_from_product`; segunda chamada idempotente (1 único evento).
5. `Integration_Edit_DoesNotBreakCopilotInvestigationContext` — após editar nome/tipo, `GetInvestigationContextAsync` reflete o novo nome/tipo.

**Resultado:** build Release 0 erros (19 avisos, todos NU1903/NU1904 pré-existentes de pacotes DataProtection/Cryptography.Xml); suíte completa **163/163 aprovados** (158 da Fase 01 + 5 novos), 0 falhas.

## 6. Validação manual (app real)

- App iniciada em Development (`http://localhost:5098`), migrations executadas sem pendências, login `admin@tracecore.local`.
- `GET /Integrations` 200; verificado no HTML: título "Histórico de Execuções e Verificações", botões "Editar" (`data-bs-target="#editIntegrationModal-…`), modais de edição, "Desvincular", selects de tipo vindos do catálogo, selects de Sistema/Resp/Hosp/Direção, badge "Verificação".
- **POST E2E** no handler `UpdateIntegration` (integração 6 — INT-BANK): 302 → reload reflete nome novo + badges `Resp: NossaEmpresa`, `Hosp: Empresa`, `Direção: Bidirecional`.
- Banco real: linha id=6 com tipo `Ticketing`, `NossaEmpresa/Empresa/Bidirecional`, `updated_at` gravado. Audit event id=525 `integration.update` com `before_json` (estado original completo) e `after_json` (novo estado).
- **Dado restaurado** (UPDATE devolvendo id=6 a `Conciliacao Bancaria`/`Directory`/product 6/dept 7) — a edição era validação, não alteração real.
- App encerrada ao final (porta 5098 livre).

## 7. Pendências para fases seguintes

- Decisão (a) acima: se health-check virar produto público/internet, reavaliar validação de URL com variante que preserve destinos privados/loopback.
- Fase 03+ deve cobrir manutenção das telas de *Catalog Types* (UI de ComponentType/IntegrationType) e/ou demais módulos do prompt original.

---

**Próximo passo:** aguardando confirmação para iniciar a **Fase 03** (`Docs/prompts/ajuste_modulos/03_*.md`), seguindo as mesmas regras (build + testes + validação manual + relatório, sem commit/push).