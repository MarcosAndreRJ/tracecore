# RELATÓRIO — FASE 03: COMPONENTES E DEPENDÊNCIAS — EDIÇÃO, TIPO E NAVEGAÇÃO (Ajuste do Ecossistema)

> Data: 21/09/2026 · Projeto: TraceCore · Prompt: `Docs/prompts/ajuste_modulos/03_componentes_e_dependencias.md`
> Regras seguidas: uma fase por vez, build + testes + validação manual na aplicação antes de avançar, sem commit/push.
> Confirmação de pré-requisito: Fase 01 (catálogo `component_types`) implementada e com testes verdes.

---

## 1. Arquivos alterados

**Nenhum arquivo novo de produção e nenhuma migration** (nenhuma alteração de dados foi necessária — ver decisões no item 3).

**Alterados:**
- `src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml.cs` — nova propriedade `ComponentTypesList` (`ICatalogService.GetComponentTypesAsync()` carregada no `OnGetAsync`); record `EditComponentInput` (Id, Name, ComponentType, ProductId, Code, Description, OwnerDepartmentId, Status); handler `OnPostUpdateComponentAsync` (repassa para `UpdateComponentAsync` com `GetCurrentUserId()`); handler `OnPostDeactivateComponentAsync(long componentId)` (lê o componente e chama o **mesmo** `UpdateComponentAsync` com `Status="Inactive"`; idempotente — não gera evento se já inativo); helper `GetCurrentUserId()` (mesmo padrão da página de Integrações da Fase 02).
- `src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml` — coluna "Ações" na tabela de componentes (botões Editar `data-bs-target="#editComponentModal-…"` e Inativar com confirm, só quando Ativo); modal "Editar Componente" **por componente** (server-rendered, padrão da Fase 02) com select de tipo vindo do catálogo + fallback para valores legados (ver item 3.b); `<select>` de tipo do modal "Novo Componente" substituído pela lista do catálogo (`ComponentTypesList`, inclui `Module`/"Módulo" e `DesktopModule`/"Módulo Desktop"); select de `DependencyType` do modal de nova dependência corrigido (ver item 3.d).
- `src/TraceCore.Web/Pages/Shared/_Layout.cshtml` — removido o item de menu "Dependências" (que apontava para a **mesma URL** de "Componentes" com a **mesma condição de `active`**); só permanece "Componentes", e a página mantém as seções "Cadeia de Dependências" e "Componentes & Módulos" (ver item 3.c).

**Sem alterações:** nenhuma migration; nenhuma entidade do Domínio; `ComponentDependency` não foi reconstruído; `ComponentOwner` intocado.

## 2. Critérios do prompt × entrega

| Critério | Entrega |
|---|---|
| Edição de componente pela UI | Coluna Ações + modal "Editar Componente" (Nome, Tipo, Produto, Código, Departamento, Descrição). Handler chama `ICatalogService.UpdateComponentAsync` já existente — **sem duplicar lógica de backend** (confirmado: o backend já auditava `component.update`). |
| Inativação sem exclusão física | Botão (ícone pause) visível só em componentes Ativos; `OnPostDeactivateComponentAsync` reenvia o mesmo `UpdateComponentAsync` com `Status="Inactive"`. Dependências e ownership preservados (teste + manual). |
| Select de tipo via catálogo (incl. "Módulo") | Create e Edit iteram `Model.ComponentTypesList`. Catálogo já tem `Module` ("Módulo"), `DesktopModule` ("Módulo Desktop"), além de `Service`, `Api`, `WindowsService`, `Infrastructure`, etc. |
| Corrigir menu duplicado | Item "Dependências" removido; só "Componentes" acende como ativo em `/Catalog/Components/Index`. |
| `dependency_type` vs `Messaging`/`SharedResource` | Investigado na base real (ver item 3.d) — valor em uso é `Runtime`; `SharedResource` da UI não bate com a documentação da entidade. Corrigida a inconsistência de nomenclatura mantendo valor como string aberta. |
| Testes | 4 testes novos (ver item 4). |

## 3. Decisões registradas

**a) Nenhuma migration / normalização de dados — e por quê.** A sondagem real mostrou duas divergências entre dados e catálogo/doc:
- `component_type` real usa `Module, Integracao, Servico, Web, Mobile, API, Core, Seguranca, Infraestrutura` (39 componentes; **nenhum** `Service`/`Frontend`/`Worker`/`Gateway`/`Integration`). O catálogo seed tem `Service, Frontend, Database, Worker, Gateway, Integration, Module, DesktopModule, Api, WindowsService, MobileApp, IntegrationAdapter, Infrastructure, Other`.
- `dependency_type` real = **só `Runtime`** (14 dependências).

Decidi **não** normalizar os 30 componentes de tipo "legado" para códigos do catálogo: o mapeamento é especulativo/lossy (ex.: `Web`→`Frontend`? `Core`→quê? `Servico`→`Service`?) e reescrever semântica de dados sem o dono do produto é reverter o modelo "string aberta" documentado na própria entidade `ComponentType` ("o valor armazenado continua sendo string aberta… sem quebrar compatibilidade com valores já gravados"). O texto do `UNION ALL` da Fase 01 também ratificou isso ao seedar `Module` ("já em uso na base"). **Pendência consciente:** convergir os tipos legados ao catálogo (ou adicioná-los como entradas) é decisão de produto — registrada no item 6.

**b) Fallback de tipo legado no modal de edição.** Como o valor é string aberta, um componente com `Servico`/`Web`/`Core`... não teria opção marcada num select puramente de catálogo (e salvar sem seleção falharia o campo obrigatório). O modal renderiza o valor atual como opção extra quando ele **não** existe no catálogo (case-insensitive), rotulada "(valor legado — não está no catálogo)". Isso garante round-trip sem perda de dados. Validado E2E: edição do componente `WEB-API` (tipo `Web`) preservou o tipo no before/after da auditoria.

**c) Menu: opção escolhida = remover o item duplicado.** A sugestão primária do prompt (manter só "Componentes", com "Dependências" como seção dentro da página — que já existe) foi seguida, pois é a que elimina a duplicidade sem mudar rotas. A alternativa `#anchor` (`/Catalog/Components/Index#dependencias`) foi descartada: exigiria rolagem automática e mudaria a UX sem ganho, já que a página é curta.

**d) `dependency_type`: corrigida a nomenclatura, sem virar catálogo.** Conjunto real em uso = `{Runtime}`. A UI oferecia `SharedResource` (não documentado na entidade) e não oferecia `Runtime`. Ajuste (string aberta, baixa cardinalidade — mantida sem catálogo): `SharedResource` → `Messaging` (alinhado ao comentário da entidade) e **adicionado `Runtime`** (valor real em uso). Nenhum dado existente foi alterado.

**e) Nenhum método novo backend.** O prompt autorizava criar `DeactivateComponentAsync` **se** o genérico não bastasse — não foi necessário: = chamada a `UpdateComponentAsync(Status:"Inactive")` (Já audita `component.update`, e o before/after captura exatamente a mudança de status). Idempotência tratada no handler (no-op se já inativo).

## 4. Testes

Novo arquivo `tests/TraceCore.IntegrationTests/ComponentEditAndDeactivateTests.cs` (4 testes):

1. `Component_Edit_PersistsAllEditableFields` — tipo legado `Servico` → `Module` (catálogo), produto, código, descrição, departamento e status persistidos.
2. `Component_Update_IsAuditedWithBeforeAfter` — `component.update` com `ActorUserId` e before/after completos.
3. `Component_Deactivate_KeepsDependenciesAndAudits` — inativação muda status, **mantém** a dependência, e audita (`after_json` contém `Inactive`).
4. `ComponentTypes_Catalog_FeedsUiOptions` — catálogo contém `Module`, `DesktopModule` e `Service`, todos `IsActive` (fonte das opções do select).

**Resultado:** build Release 0 erros (apenas avisos pré-existentes — NU1903/NU1904 + o CS8602 de `Diagnosis/Index.cshtml:102` que volta a aparecer quando o Razor recompila views por causa da mudança no layout, pré-existente e fora do escopo desta fase); suíte completa **167/167 aprovados** (163 da Fase 02 + 4 novos), 0 falhas.

## 5. Validação manual (app real)

- App em Development (`http://localhost:5098`), migrations OK, login admin.
- `GET /Catalog/Components/Index` 200; verificados no HTML: botões "Editar"/"Inativar", modais `editComponentModal-…` (39), fallback "(valor legado)" presente, opção `Module`/`DesktopModule` no select do catálogo, `SharedResource` ausente, `Runtime` presente, item de menu "Dependências" removido.
- **POST E2E edição** (componente 20 `WEB-API`, tipo legado `Web`): 302 → reload refletiu o novo nome e o tipo legado voltou selecionado; banco: linha atualizada + audit id 529 (`before`/`after` com `ComponentType:"Web"` preservado).
- **POST E2E inativação** (componente 11 `CORE-ACESSO`): status `Inactive` no banco; audit id 531 com `before Status Active` → `after Status Inactive`; nenhuma dependência/owner perdida.
- Dados restaurados (SQL devolveu `API Laravel` e `Active`); app encerrada, porta 5098 livre.

## 6. Pendências para fases seguintes

- **Tipos legados vs catálogo (decisão de produto):** convergir `Integracao/Servico/Web/Mobile/Core/Seguranca/Infraestrutura` para códigos do catálogo (complexidade: mapeamento semântico) **ou** promover os códigos em uso a entradas do catálogo (aditiva, zero risco) — escolher na Fase 07 (Validação Final) ou com o PO.
- Fase 04 deve reutilizar o modal de edição de componente para vínculo automático de `ProductId` a partir da tela de detalhe do Sistema (deixa do prompt da Fase 03).

---

**Próximo passo:** aguardando confirmação para iniciar a **Fase 04** (`Docs/prompts/ajuste_modulos/04_sistemas_cadastro_e_relacionamentos.md`), seguindo as mesmas regras.