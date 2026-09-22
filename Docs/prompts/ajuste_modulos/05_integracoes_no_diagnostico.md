# FASE 05 — TESTE DE INTEGRAÇÃO DURANTE INVESTIGAÇÃO E VALIDAÇÃO DE SOLUÇÃO

> Depende das **Fases 01 e 02** (integração editável, com sistema associado). Esta é a fase mais sensível de modelagem de todo o plano — leia inteira antes de codar uma linha, e não avance para a Fase 06 sem ter certeza de que a rastreabilidade está correta.

---

## CONTEXTO

Hoje existem três formas de "testar/executar uma integração", nenhuma conectada às outras:

1. **Registrar Execução** manual (`IntegrationService.RegisterRunAsync`) — log livre, sem ligação com nenhum caso.
2. **Testar Health-Check** (`IIntegrationHealthCheckService.ExecuteHealthCheckAsync`) — executa HTTP/TCP de verdade, grava `IntegrationRun`, mas só é acionável da tela de Integrações.
3. **Motor de diagnóstico automático** (`DiagnosticEngineService.GetEngineStateAsync`) — quando um `DiagnosticCheck` de um `DiagnosticFlow` tem `CheckType = "AutomatedCheck"` e `IntegrationId` preenchido, ele já chama o health-check de verdade e já grava um `DiagnosticStep` a partir do resultado — mas a ligação entre o `DiagnosticStep` gerado e o `IntegrationRun`/`Integration` que o originou é **só textual** (`InputEvidenceSummary: "Integração #{id}"`), sem FK.

O pedido original é claro: não criar um quarto mecanismo. Usar **uma única execução** (`IntegrationRun`), com contexto/origem, ligada estruturalmente (FK real, não texto) ao que a originou.

## OBJETIVO

1. Dar a `IntegrationRun` um contexto de origem real (`RunContext`) e uma ligação opcional com o caso que a originou (`CaseId`).
2. Ligar estruturalmente `DiagnosticStep` e `CaseEvidence` a um `IntegrationRun`, substituindo o hack textual existente.
3. Permitir "Testar Integração" a partir da investigação de um caso, gerando `IntegrationRun` + `DiagnosticStep` (e, quando o usuário quiser, `CaseEvidence`) numa única ação rastreável.
4. Permitir reaproveitar o mesmo teste para validar uma solução aplicada (antes/depois), sem criar um segundo modelo.

## ESTADO ATUAL CONFIRMADO (investigação profunda realizada nesta análise)

**Entidades e ausência de ligação com Integration:**
- `DiagnosticStep` (`src/TraceCore.Domain/Entities/DiagnosticStep.cs:9-27`) — campos: `Id, DiagnosticSessionId, SequenceNo, StepType, HypothesisId, Title, Objective, Instruction, InputEvidenceSummary, ResultSummary, Outcome, RiskLevel, DurationSeconds, PerformedBy, PerformedAt, MetadataJson`. **Nenhum campo de integração.**
- `CaseEvidence` (`src/TraceCore.Domain/Entities/CaseEvidence.cs:7-20`) — campos: `Id, CaseId, CaseIterationId, DiagnosticStepId, EvidenceType, Description, AttachmentId, CreatedBy, CreatedAt`. **Nenhum campo de integração.**
- `CaseResolution` (`src/TraceCore.Domain/Entities/CaseResolution.cs:8-41`) — `ValidationSummary` é só texto livre, sem ligação estruturada com evidência ou execução de integração.
- `IntegrationRun` (`src/TraceCore.Domain/Entities/IntegrationEntities.cs:94-140`) — campos: `Id, IntegrationId, StartedAt, FinishedAt, Status, RecordsProcessed, ErrorMessage, RecordedBy, RecordedAt, TriggeredBy`. **Nenhum campo de contexto/origem, nenhum `CaseId`.**
- Único vínculo com integração que já existe fora de `IntegrationEntities.cs`: `DiagnosticCheck.IntegrationId` (`src/TraceCore.Domain/Entities/DiagnosticFlowEntities.cs:83`) — mas `DiagnosticCheck` é um **template de fluxo de diagnóstico** (tabela `diagnostic_checks`), não a instância real de investigação (`DiagnosticStep`, tabela `diagnostic_steps`). São conceitos diferentes: um é "pergunta/checagem que pode aparecer num fluxo", outro é "o que de fato aconteceu num caso real".

**Mecanismo real de execução já existente:**
- `IIntegrationHealthCheckService.ExecuteHealthCheckAsync` (`src/TraceCore.Infrastructure/Services/IntegrationHealthCheckService.cs:32-156`) já faz HTTP/TCP real (nunca finge sucesso — princípio de falha segura documentado no próprio arquivo) e grava um `IntegrationRun` de verdade.
- `DiagnosticEngineService.GetEngineStateAsync` (`src/TraceCore.Application/Services/DiagnosticEngineService.cs:361-435`) já invoca esse serviço quando o melhor `DiagnosticCheck` recomendado é do tipo `AutomatedCheck` com `IntegrationId`, converte o resultado em `DiagnosticStepOutcome` e chama `RegisterDiagnosticStepAsync` (linhas ~387-398) — mas grava a referência à integração só como texto solto dentro de `InputEvidenceSummary`/`ResultSummary`. **Esse é o ponto exato a corrigir estruturalmente.**
- `DiagnosticStepTypes.AutomatedCheck` (`src/TraceCore.Domain/Enums/InvestigationEnums.cs:53-61`) já é um valor válido de `StepType` — reutilizar, não criar um tipo novo.
- `CaseEvidence.EvidenceType = "DiagnosticTest"` já é usado em `CaseInvestigationService.cs:215` como sugestão automática de tipo de evidência quando ligada a um passo diagnóstico — reutilizar esse valor para evidência originada de teste de integração.

**Serviços envolvidos, métodos e assinaturas (confirme antes de codar — código pode ter mudado):**
- `ICaseInvestigationService.RegisterDiagnosticStepAsync(RegisterDiagnosticStepCommand, long? currentUserId, ct)` — `RegisterDiagnosticStepCommand(long CaseId, long? HypothesisId, string Title, string? Objective, string? Instruction, string? InputEvidenceSummary, string ResultSummary, string Outcome, string? StepType="Verification", string? RiskLevel="Low", int? DurationSeconds)` — **nenhum campo de `IntegrationRunId` hoje**.
- `ICaseInvestigationService.RecordEvidenceAsync(RecordEvidenceCommand, long currentUserId, ct)` — `RecordEvidenceCommand(long CaseId, string EvidenceType, string Description, long? AttachmentId, long? DiagnosticStepId, long? CaseIterationId, List<HypothesisEvidenceRelationInputDto>? HypothesisRelations)` — **nenhum campo de `IntegrationRunId` hoje**.
- `ICaseResolutionService.ResolveCaseAsync(ResolveCaseCommand, long currentUserId, ct)` — não referencia evidência nem execução de integração; `ValidationSummary` é texto livre digitado pelo usuário.

## DECISÃO CONCEITUAL — UMA EXECUÇÃO, CONTEXTO DIFERENTE

```
IntegrationRun
  + RunContext: "Administrative" | "Diagnostic" | "SolutionValidation" | "AutomatedHealthCheck"
  + CaseId: long? (preenchido quando RunContext é Diagnostic ou SolutionValidation)
```

`RunContext` é string aberta validada em código (mesmo padrão de `Status`/`TriggeredBy` já usados em `IntegrationRun`), não enum fechado nem catálogo administrável — é um conceito de domínio interno, não algo que a empresa precisa administrar.

```
DiagnosticStep.IntegrationRunId: long? (FK -> integration_runs, SetNull)
CaseEvidence.IntegrationRunId: long? (FK -> integration_runs, SetNull)
```

Fluxo de investigação (Diagnostic):
```
Caso → Hipótese → "Testar Integração" → IntegrationRun (RunContext=Diagnostic, CaseId=X)
                                       → DiagnosticStep (StepType=AutomatedCheck, IntegrationRunId=Y)
                                       → [opcional, ação explícita do usuário] CaseEvidence (EvidenceType=DiagnosticTest, IntegrationRunId=Y)
```

Fluxo de validação de solução (SolutionValidation) — **reaproveita exatamente o mesmo mecanismo**, sem tabela nova:
```
Solução aplicada → "Testar Integração" (mesma ação, contexto diferente) → IntegrationRun (RunContext=SolutionValidation, CaseId=X)
                                                                          → CaseEvidence (EvidenceType=DiagnosticTest, IntegrationRunId=Y, CaseIterationId=iteração atual)
```
`CaseResolution` continua sem FK direta para evidência — a ligação acontece por `CaseIterationId` (que já existe tanto em `CaseResolution` quanto em `CaseEvidence`), preservando o modelo já existente em vez de criar uma tabela nova de "evidência de resolução". `ValidationSummary` (texto livre) pode, opcionalmente, referenciar a evidência criada (ex.: "Ver evidência de validação registrada") — decida durante a implementação se isso deve ser automático ou manual.

Health-check automático continua existindo com `RunContext=AutomatedHealthCheck` (sem `CaseId`) — é o comportamento de hoje, só rotulado.

## ESCOPO

- Migration aditiva: `integration_runs.run_context`, `integration_runs.case_id` (FK opcional → `cases`), `diagnostic_steps.integration_run_id` (FK opcional → `integration_runs`), `case_evidences.integration_run_id` (FK opcional → `integration_runs`).
- Corrigir `DiagnosticEngineService.GetEngineStateAsync` para gravar `DiagnosticStep.IntegrationRunId` de verdade (estrutural), mantendo o texto em `InputEvidenceSummary`/`ResultSummary` como está (não é obrigatório remover o texto, só parar de depender só dele) e marcar `RunContext=AutomatedHealthCheck` na execução gerada por ele. Confirme durante a implementação se esse fluxo já cria o `IntegrationRun` diretamente ou se passa por `IIntegrationHealthCheckService` — ajuste o ponto exato onde o `RunContext`/`CaseId` deve ser setado.
- Novo endpoint de aplicação para "testar integração no contexto de um caso": recebe `CaseId`, `IntegrationId`, `HypothesisId?`, executa via `IIntegrationHealthCheckService` (reaproveitar, não duplicar a lógica HTTP/TCP), grava `IntegrationRun` com `RunContext`/`CaseId`, cria `DiagnosticStep` automaticamente (sempre) e `CaseEvidence` (quando o usuário confirmar explicitamente que quer registrar como evidência — não criar evidência automaticamente sem ação humana, para não poluir o caso).
- UI: ação "Testar Integração" acessível de dentro de `Cases/Details.cshtml` (verifique a estrutura de abas já existente — provavelmente cabe na aba de diagnóstico/evidências), listando só as integrações associadas ao `Product` do caso (reaproveitar `IIntegrationRepository.GetIntegrationsByProductIdAsync`, já existente).
- UI: no fluxo de resolução de caso (`ResolveCaseCommand`/tela de resolução), oferecer a mesma ação "Testar Integração" com `RunContext=SolutionValidation`, permitindo comparar execução anterior (Diagnostic) com a nova (SolutionValidation) do mesmo `IntegrationId`.

## NÃO ESCOPO

- Não alterar o formulário de "Registrar Execução"/"Testar Health-Check" já existente na tela de Integrações — eles continuam existindo como estão (`RunContext=Administrative`/`AutomatedHealthCheck` respectivamente, atribuído automaticamente sem exigir escolha do usuário nesses fluxos específicos).
- Não criar uma tabela nova de "evidência de resolução" — reaproveitar `CaseEvidence` via `CaseIterationId`, conforme decisão conceitual acima.
- Não mexer no motor de recomendação de hipóteses/`DiagnosticFlow` além do necessário para gravar a FK estrutural.
- Não implementar correção de SSRF em `IntegrationHealthCheckService` nesta fase (mesma observação da Fase 02 — reaproveitar `ExternalUrlSafetyValidator` se for mexer no fluxo de qualquer forma, senão documentar como pendência).

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Domain/Entities/DiagnosticStep.cs
src/TraceCore.Domain/Entities/CaseEvidence.cs
src/TraceCore.Domain/Entities/IntegrationEntities.cs
src/TraceCore.Domain/Enums/InvestigationEnums.cs
src/TraceCore.Application/Services/ICaseInvestigationService.cs / CaseInvestigationService.cs
src/TraceCore.Application/Services/ICaseResolutionService.cs / CaseResolutionService.cs
src/TraceCore.Application/Services/DiagnosticEngineService.cs (linhas ~361-435, ponto exato do hack textual)
src/TraceCore.Application/Services/IIntegrationHealthCheckService.cs / IntegrationHealthCheckService.cs
src/TraceCore.Application/DTOs/InvestigationDTOs.cs
src/TraceCore.Application/DTOs/CaseResolutionDtos.cs
src/TraceCore.Infrastructure/Migrations/M20260917_04_CreateTechnicalCatalogAndCasesSchema.cs (schema base de case_evidences)
src/TraceCore.Infrastructure/Migrations/M20260917_06_CreateInvestigationAndDiagnosticSchema.cs (schema base de diagnostic_steps)
src/TraceCore.Infrastructure/Migrations/M20260918_13_StructuredEvidenceAndHypothesisRelations.cs (última alteração de case_evidences)
src/TraceCore.Web/Pages/Cases/Details.cshtml / Details.cshtml.cs
resultado da Fase 02 (Integration editável/associada a produto)
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

```sql
ALTER TABLE integration_runs
  ADD COLUMN run_context VARCHAR(50) NULL,
  ADD COLUMN case_id BIGINT NULL,
  ADD CONSTRAINT fk_intrun_case FOREIGN KEY (case_id) REFERENCES cases(id) ON DELETE SET NULL;

ALTER TABLE diagnostic_steps
  ADD COLUMN integration_run_id BIGINT NULL,
  ADD CONSTRAINT fk_diagstep_intrun FOREIGN KEY (integration_run_id) REFERENCES integration_runs(id) ON DELETE SET NULL;

ALTER TABLE case_evidences
  ADD COLUMN integration_run_id BIGINT NULL,
  ADD CONSTRAINT fk_caseev_intrun FOREIGN KEY (integration_run_id) REFERENCES integration_runs(id) ON DELETE SET NULL;
```
Backfill (idempotente, via `UPDATE ... WHERE`): dados existentes de `run_context` podem ficar `NULL` — não é obrigatório inferir contexto retroativamente para execuções já gravadas, a menos que seja trivial (ex.: tudo que tem `triggered_by='Automated'` e nenhum caso associado pode virar `AutomatedHealthCheck`). Decida e documente.

### DOMAIN

- `IntegrationRun`: `RunContext` (string?), `CaseId` (long?).
- `DiagnosticStep`: `IntegrationRunId` (long?).
- `CaseEvidence`: `IntegrationRunId` (long?).
- Constante de valores válidos de `RunContext` (mesmo padrão de `ValidExternalResearchPolicies`).

### APPLICATION

- Novo método (nome sugerido) `ICaseInvestigationService.TestIntegrationDuringInvestigationAsync(long caseId, long integrationId, long? hypothesisId, bool recordAsEvidence, long currentUserId, ct)` ou equivalente — decida se cabe em `ICaseInvestigationService` (mais próximo do fluxo de investigação) ou num serviço novo dedicado à orquestração; justifique a escolha.
- Esse método: chama `IIntegrationHealthCheckService.ExecuteHealthCheckAsync` (ou o equivalente que resultar da integração escolhida), marca `RunContext`/`CaseId` no `IntegrationRun` resultante, chama `RegisterDiagnosticStepAsync` internamente (reaproveitar, não duplicar a lógica de criação de step), e condicionalmente `RecordEvidenceAsync`.
- `RegisterDiagnosticStepCommand`/`RecordEvidenceCommand`: adicionar `IntegrationRunId` opcional.
- Corrigir `DiagnosticEngineService.GetEngineStateAsync` para propagar `IntegrationRunId` real no `DiagnosticStep` gerado pelo motor automático.
- Mesma ação reaproveitada no fluxo de resolução, com `RunContext=SolutionValidation`.

### INFRASTRUCTURE

- `MySqlDiagnosticRepository`/`InMemoryDiagnosticRepository` (ou onde `DiagnosticStep`/`CaseEvidence` são persistidos — confirme o nome real do repositório) — incluir `IntegrationRunId` no INSERT/SELECT.
- `MySqlIntegrationRepository`/`InMemoryIntegrationRepository` — incluir `RunContext`/`CaseId` no INSERT/SELECT de `IntegrationRun`.

### WEB

- `Cases/Details.cshtml`/`.cshtml.cs`: ação "Testar Integração" (modal ou seção inline), listando integrações do produto do caso.
- Tela/fluxo de resolução de caso: mesma ação, contexto `SolutionValidation`.
- `Integrations/Index.cshtml`: exibir `RunContext` na tabela de histórico (complementa a coluna "Origem" já existente, que hoje só distingue Manual/Automated).

### AUDITORIA

`integration_run.executed_for_diagnosis`, `integration_run.executed_for_validation` (ou reaproveitar `integration.run_register` da Fase 01 com `RunContext` no `after` — decida e documente).

### PERMISSÕES

Ação de testar integração durante investigação deve respeitar `caso.editar` (ou a permissão já usada para registrar evidência/passo diagnóstico em `Cases/Details.cshtml` — confirme qual é) **e não** exigir `integracao.gerenciar` (o usuário está investigando um caso, não administrando o catálogo de integrações) — confirme contra o código real e documente a decisão.

### TESTES

- Testar integração a partir de um caso cria `IntegrationRun` com `RunContext=Diagnostic` e `CaseId` corretos.
- O `DiagnosticStep` resultante tem `IntegrationRunId` preenchido (ligação estrutural, não só texto).
- Confirmar explicitamente como evidência cria `CaseEvidence` com `IntegrationRunId` e `EvidenceType=DiagnosticTest`.
- Testar a mesma integração em contexto de validação de solução cria um segundo `IntegrationRun` (`RunContext=SolutionValidation`) sem confundir com a execução da fase de investigação.
- `DiagnosticEngineService` (motor automático) continua funcionando e agora grava `IntegrationRunId` estrutural no step que gera.
- Execução via "Registrar Execução"/"Testar Health-Check" (fluxos já existentes, fora do contexto de caso) continua funcionando sem exigir `CaseId`.

## CRITÉRIOS DE ACEITE

- Build limpo, suíte completa passando.
- Teste manual: abrir um caso com produto associado a uma integração, testar a integração pela investigação, confirmar que o passo diagnóstico e (se escolhido) a evidência aparecem corretamente vinculados na timeline do caso.
- Nenhuma regressão no motor de diagnóstico automático nem nos fluxos administrativos de integração já existentes.

## ENTREGA FINAL

1. arquivos criados/alterados;
2. migration criada e colunas/FKs resultantes;
3. decisão tomada sobre onde vive o novo método de orquestração;
4. confirmação de que `DiagnosticEngineService` foi corrigido para usar a FK estrutural em vez do texto solto;
5. como ficou o fluxo de validação de solução reaproveitando o mesmo mecanismo;
6. resultado do build e dos testes;
7. confirmação de teste manual na aplicação rodando;
8. riscos/limitações que ficam para a Fase 06.

**NÃO pare após analisar. Implemente, execute build, execute testes, corrija erros até passar.**
