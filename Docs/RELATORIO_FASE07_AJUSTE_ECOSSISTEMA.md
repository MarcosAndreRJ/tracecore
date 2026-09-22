# RELATÓRIO — FASE 07: VALIDAÇÃO FINAL / AUDITORIA (Ajuste do Ecossistema)

> Data: 21/09/2026 · Projeto: TraceCore · Prompt: `Docs/prompts/ajuste_modulos/07_validacao_final.md`
> Natureza: auditoria de ponta a ponta das Fases 01–06, com correção de lacunas de verificação (testes) — sem implementar funcionalidade nova de produto, conforme escopo da própria Fase 07.
> Execução: build real, suíte de testes real, aplicação real contra MySQL real (não InMemory), validação manual no navegador.

---

## 1. Resultado da suíte de testes

- **Antes da auditoria:** 175/175 testes passando (`dotnet test` limpo, sem mocks de shell).
- **Lacuna encontrada:** a Fase 05 (mecanismo de teste de integração durante investigação/validação de solução) tinha 0 testes automatizados cobrindo `TestIntegrationDuringInvestigationAsync` / `TestIntegrationForSolutionValidationAsync`.
- **Correção aplicada nesta fase:** novo arquivo `tests/TraceCore.IntegrationTests/IntegrationTestDuringInvestigationTests.cs` (5 testes) cobrindo:
  1. Criação de `IntegrationRun` (RunContext=Diagnostic) + `DiagnosticStep` (AutomatedCheck) vinculados estruturalmente.
  2. `recordAsEvidence=true` cria `CaseEvidence` (DiagnosticTest) vinculada ao mesmo `IntegrationRunId`.
  3. Regra de negócio: integração não pertencente ao sistema do caso lança `BusinessRuleValidationException` (BR-073).
  4. Validação de solução cria `IntegrationRun` (RunContext=SolutionValidation) + `CaseEvidence`.
  5. Auditoria `integration.run_register` gravada com `runContext` e `caseId` corretos.
- **Depois da auditoria:** **180/180 testes passando**, 0 falhas, build limpo (0 erros, apenas warnings pré-existentes de NuGet/nullable não relacionados).

Nenhum teste pré-existente foi alterado ou quebrado.

## 2. Migrations e integridade do banco (MySQL real)

Subida real da aplicação (`dotnet run`, ambiente Development, banco `192.168.0.5:3306/TraceCoreDb`) aplicou com sucesso as 3 migrations pendentes:

| Migration | Efeito | Resultado |
|---|---|---|
| `M20260921_24_AddComponentAndIntegrationTypeCatalogs` | Cria `component_types`/`integration_types`, adiciona `responsibility/hosting_location/direction` em `integrations`, seed idempotente | ✅ Aplicada |
| `M20260921_25_AddSystemTypeToProductTechnicalProfiles` | Adiciona `system_type` em `product_technical_profiles` | ✅ Aplicada |
| `M20260921_26_AddRunContextAndIntegrationRunLinks` | Adiciona `run_context`/`case_id` em `integration_runs`, `integration_run_id` (FK, `ON DELETE SET NULL`) em `diagnostic_steps`/`case_evidences`, backfill idempotente | ✅ Aplicada |

Revisão de código de todas as 3 migrations: **100% aditivas**, guardas de existência (idempotentes), nenhuma altera migration histórica, nenhum `DROP`/`DELETE` destrutivo, FKs novas usam `ON DELETE SET NULL` (preservam histórico ao invés de cascata).

**Massa de teste (`Docs/sql_mockup`):** arquivos não tocados (confirmado por `git status`). Dados confirmados intactos após as migrations: 11 usuários, 542 eventos de auditoria pré-existentes, 50 casos, 7 produtos originais (TMS Desktop, TMS Mobile, Portal Web TMS, Serviços de Integração Delphi, API Comercial, Conectores e Integrações + 1 criado nesta auditoria), integrações SAP/SEFAZ/EDI etc. — nada apagado ou corrompido.

## 3. Validação manual na aplicação real (navegador, MySQL real)

Fluxos executados de ponta a ponta com a aplicação rodando de verdade (login `admin@tracecore.local`):

| Fluxo | Resultado |
|---|---|
| Criar Sistema com Identificação + Contexto Técnico Básico (Tipo/Tecnologia/Banco/Hospedagem) | ✅ Modal em 2 seções conforme Fase 04; persistiu e refletiu corretamente na aba Contexto Técnico |
| Adicionar Componente pela aba do Sistema (catálogo de tipos, incl. "Módulo") | ✅ Select 100% catalog-driven (14 tipos seedados), Editar/Inativar/Desvincular presentes |
| Criar Integração pela aba do Sistema (já vinculada, tipo via catálogo, Responsabilidade/Hospedagem/Direção) | ✅ Criada e vinculada; badges exibidos corretamente na listagem |
| Editar Integração (modal completo com seletor de Sistema) | ✅ Todos os campos, incluindo Sistema, prefiled corretamente |
| "Histórico de Execuções e Verificações" renomeado | ✅ Confirmado no card da integração |
| "Vincular Integração Existente" (reuso sem duplicar) | ✅ Tabela lista integrações de outros sistemas com ação "Vincular" |
| Menu Componentes/Dependências sem duplicidade | ✅ Item duplicado removido de `_Layout.cshtml`; sobra 1 item "Componentes" |
| Auditoria de `IntegrationService` (inexistente antes da Fase 01) | ✅ Confirmado na tela `/Audit`: `integration.create`, `integration.update`, `integration.link_to_product`, `integration.unlink_from_product`, `integration.run_register` todos presentes com ator, before/after |
| Permissões | ✅ `catalogo.gerenciar` (Produtos/Componentes), `integracao.gerenciar` (Integrações), `caso.diagnosticar`/`caso.encerrar` nos handlers de teste de integração — nenhuma exige `integracao.gerenciar` para testar integração durante um caso, conforme decisão da Fase 05 |
| Padrão visual | ✅ Todos os modais/telas novas usam `tc-card`/`tc-btn`/`tc-input`/`tc-select`/`tc-badge`; nenhuma tabela HTML crua nova |
| Razor Pages preservado | ✅ Nenhuma fase introduziu SPA/Blazor/React/Angular |
| Motor de Diagnóstico Guiado (automated check) grava `IntegrationRunId` real | ✅ `DiagnosticEngineService.cs` corrigido: era texto solto `"Integração #{id}"`, agora usa `IntegrationRunId: run.Id` (FK real) |
| Copiloto — contexto enriquecido (`SystemType`, `Responsibility`, `HostingLocation`, `Direction`) | ⚠️ Verificado por código e por teste (`ExecuteGetProductContextAsync` serializa os campos novos) — **não foi possível testar uma resposta real do LLM**: nenhum provedor de IA está configurado neste ambiente (sem chave em User Secrets/appsettings). Limitação de ambiente, não falha funcional. |

## 4. Pendência explícita encontrada (não corrigida nesta fase, por estar fora do escopo da Fase 07)

**Teste de integração "sob demanda" durante investigação/validação de solução não tem entrada na UI.**

- Backend, contrato e regra de negócio **100% implementados e corretos** (`ICaseInvestigationService.TestIntegrationDuringInvestigationAsync`/`TestIntegrationForSolutionValidationAsync`, `CaseInvestigationService.cs:765-885`), com handlers Razor prontos (`Details.cshtml.cs:346-419`: `OnPostTestIntegrationAsync`, `OnPostTestIntegrationForValidationAsync`), permissões corretas (`caso.diagnosticar`/`caso.encerrar`, sem exigir `integracao.gerenciar`), agora com **cobertura de teste real** (item 1 acima).
- Porém `src/TraceCore.Web/Pages/Cases/Details.cshtml` **não contém nenhuma referência** a `TestIntegration`/`CaseIntegrations`/`IntegrationRunId` — os handlers existem mas não há botão/formulário na página que os acione. `CanTestIntegrations`/`CanTestIntegrationsForValidation` (as flags que controlariam a visibilidade dessa seção) também não são lidas em nenhum lugar do `.cshtml`.
- Isso significa: o caminho **automático** (motor de Diagnóstico Guiado testando integração como parte de um fluxo adaptativo) funciona e é acessível pela UI; o caminho **manual** ("testar esta integração agora", incluindo a validação de solução) só é acionável via chamada direta ao serviço/POST cru — **não é utilizável por um usuário real na tela do Caso**.
- **Por que não foi corrigido nesta fase:** a Fase 07 é explicitamente uma fase de auditoria/verificação ("não implemente funcionalidade nova aqui... registre como pendência explícita" — texto do próprio prompt da fase). Adicionar a seção na UI é escopo da Fase 05 (Web), não revisado/planejado nesta auditoria (form, ligação com `CaseIntegrations`, i18n de mensagens, etc.).
- **Recomendação:** pequeno complemento à Fase 05 (Web) antes de considerar a funcionalidade "utilizável por usuários finais": adicionar à aba "Diagnóstico & Hipóteses" (ou seção equivalente) do `Cases/Details.cshtml` um formulário/lista para `TestIntegrationIdInput` + `TestIntegrationHypothesisIdInput` + checkbox "registrar como evidência" (usa `CanTestIntegrations`/`CaseIntegrations`, já existentes no `.cshtml.cs`), e simétrico na aba de Encerramento/Validação (`CanTestIntegrationsForValidation`).

## 5. Risco pré-existente reconfirmado (fora do escopo de todas as 7 fases)

`IntegrationHealthCheckService.cs` continua sem qualquer proteção SSRF (nenhuma validação de IP/DNS antes do `HttpClient.GetAsync`), apesar de `ExternalUrlSafetyValidator` (construído em trabalho anterior à este plano) ser reaproveitável para isso. Este risco já estava documentado como risco #3 em `00_LEIA_ME.md` desde o planejamento inicial e permanece **intencionalmente fora do escopo** das Fases 01–07 — não é uma regressão introduzida por este trabalho.

## 6. Confirmações finais

- ✅ Nenhuma migration histórica foi alterada (apenas 3 novas migrations, todas aditivas).
- ✅ A massa de teste em `Docs/sql_mockup` está intacta (arquivos não tocados; dados no banco preservados).
- ✅ 180/180 testes automatizados passando; build limpo (0 erros).
- ✅ Nenhum código de produto foi alterado nesta fase além de: (a) 1 teste novo de verificação e (b) este relatório. Nenhuma tela, serviço, repositório ou entidade de produto foi modificada — a auditoria confirmou o trabalho das Fases 01–06 por leitura, execução real e teste, sem "consertos" de escopo.

## 7. Parecer final (na conclusão da auditoria original)

**O conjunto das Fases 01–04 e 06 está pronto para uso** — implementado, testado e validado manualmente contra o banco real, sem regressões.

**A Fase 05 está pronta no backend/dados/testes, mas com 1 bloqueador de usabilidade:** o teste manual de integração durante investigação e durante validação de solução não é acionável pela UI (item 4). Até essa lacuna ser fechada, essa capacidade específica só existe "por baixo do capô" — o motor de Diagnóstico Guiado automático continua funcionando normalmente e não depende dela.

---

## 8. COMPLEMENTO — Formulário da Fase 05 implementado (pós-auditoria)

A pendência do item 4 foi fechada. Alterações:

**`src/TraceCore.Web/Pages/Cases/Details.cshtml`:**
- Botão **"Testar Integração"** no cabeçalho (visível quando `CanTestIntegrations`), abrindo modal `#modalTestIntegration`: seleciona a Integração (`TestIntegrationIdInput`), a Hipótese relacionada (opcional), checkbox "registrar como evidência formal" e o tipo de relação com a hipótese — reaproveita exatamente os campos já existentes no `.cshtml.cs`, sem novo código de aplicação.
- Botão **"Validar Integração"** no cabeçalho (visível quando `CanTestIntegrationsForValidation`, junto a "Encerrar Caso"), abrindo modal `#modalTestIntegrationValidation`: seleciona a Integração e chama `OnPostTestIntegrationForValidationAsync`.
- Ambos os modais seguem o padrão visual do projeto (`tc-card`/`tc-modal-*`/`tc-select`/`tc-check-*`), com `id`/`for` explícitos para não colidir com o `id` gerado por `asp-for="TestIntegrationIdInput"` (usado nos dois modais).

**`src/TraceCore.Web/Pages/Cases/Details.cshtml.cs`:**
- Corrigido o parâmetro de retorno de aba dos dois handlers: usavam `activeTab` (`RedirectToPage(new { id, activeTab = "diagnosis" })`), mas a view só lê `Request.Query["tab"]` — o redirecionamento nunca reabria a aba certa. Trocado para `tab` (`"diagnosis"` para o teste durante investigação; `"overview"`, onde vive o card de encerramento, para a validação de solução).

**Bug real encontrado e corrigido ao validar contra o MySQL real** (não é um problema da UI nova — é um defeito pré-existente na criação de sessão de diagnóstico, nunca antes exercitado num caso "do zero" sem nenhuma sessão aberta):
- `CaseInvestigationService.EnsureOpenSessionAsync` criava uma nova `DiagnosticSession` sem popular `CaseIterationId`, e `MySqlDiagnosticRepository.CreateSessionAsync` **omitia a coluna `case_iteration_id`** do `INSERT` — coluna que a migration `M20260918_12_CaseIterationAndReopenSupport` tornou `NOT NULL` sem default. Resultado real ao testar: `Field 'case_iteration_id' doesn't have a default value` — quebrava **qualquer** primeiro passo diagnóstico (`Registrar Teste` normal, não só o teste de integração) num caso recém-aberto.
- Corrigido em dois pontos: `EnsureOpenSessionAsync` agora resolve `_caseRepository.GetCurrentIterationAsync` e popula `session.CaseIterationId` antes de criar a sessão; `CreateSessionAsync`/`GetOpenSessionByCaseIdAsync`/`GetSessionByIdAsync` (Infrastructure) passaram a incluir `case_iteration_id` no INSERT/SELECT.
- Por que os 180 testes não pegaram isso: a suíte roda 100% contra repositórios InMemory (`Persistence:Provider=InMemory`), que nunca aplicaram essa restrição de schema — só o MySQL real acusa.
- Novo teste de regressão: `IntegrationTestDuringInvestigationTests.TestIntegrationDuringInvestigation_FirstStepOnFreshCase_CreatesSessionWithCurrentIterationId`, fixando a expectativa na camada de Application (`CaseIterationId` da sessão > 0 e igual à iteração atual), independente do schema real do MySQL.

**Validação manual real (MySQL, não InMemory):** caso #1282 criado do zero (sistema "Conectores e Integrações", com integrações reais cadastradas), fluxo completo testado:
- "Testar Integração" → INT-SAP: falha real de rede (`sapgw.cliente.local` não resolve) registrada como `DiagnosticStep` (`Não Funcionou`), sem simular sucesso (falha segura, §26).
- "Testar Integração" com "registrar como evidência" → INT-BANK: `DiagnosticStep` + `CaseEvidence` (`DiagnosticTest`) criados e vinculados ao mesmo `IntegrationRunId`.
- "Validar Integração" → INT-EDI: `CaseEvidence` de validação de solução (RunContext=SolutionValidation) criada.
- `/Audit` confirma os 3 eventos `integration.run_register` (com `runContext`/`caseId` corretos) pareados com `DiagnosticStepRegistered`/`EvidenceRecorded`.
- Suíte final: **181/181 testes passando**, build limpo.

## 9. Parecer final atualizado

**O conjunto das 6 fases (01–06) está pronto para uso**, incluindo agora o teste manual de integração durante investigação e validação de solução (Fase 05), acessível pela UI e validado contra o banco real — sem bloqueadores conhecidos além do risco pré-existente do item 5 (SSRF em `IntegrationHealthCheckService`, fora de escopo) e da limitação de ambiente do item 3 (Copiloto sem provedor de LLM configurado, não testável nesta máquina).
