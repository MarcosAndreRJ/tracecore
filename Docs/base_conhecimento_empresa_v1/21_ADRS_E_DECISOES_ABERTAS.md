# 21 — ADRs e decisões abertas

## Decisões já assumidas

### ADR-0001 — .NET/C# end-to-end
Status: aceito.

### ADR-0002 — MySQL como banco principal
Status: aceito.

### ADR-0003 — Monólito modular no início
Status: aceito.

### ADR-0004 — Interface web
Status: **revisado em 2026**. Baseline original era Blazor Web App; a implementação
real (Fases 1–4) seguiu **ASP.NET Core Razor Pages + Bootstrap**, e a tarefa de
revisão de UI/UX de 2026 confirmou manter Razor Pages (sem migrar para Blazor,
React ou Vue). Documentação e código realinhados nesta revisão.

### ADR-0005 — IA desacoplada do núcleo
Status: aceito.

### ADR-0006 — Dapper/MySqlConnector como persistência baseline
Status: proposto nesta documentação para reduzir risco de provider EF Core/MySQL; confirmar no spike inicial.

### ADR-0002 (revisado) / ADR — Persistência — Fase 7.A
Status: **revisado em 2026-09-18**. Descoberta crítica: `Persistence:Provider` nunca esteve
configurado em nenhum `appsettings*.json`, então o DI sempre usava InMemory silenciosamente —
nenhuma fase anterior havia sido validada contra banco real. Corrigido: `Persistence:Provider`
ausente/inválido agora falha explicitamente no startup; InMemory só é aceito quando
explicitamente configurado (ex.: ambiente `Testing` dedicado para testes de integração).
Servidor real disponibilizado para validação é **MariaDB 10.5.29** (Debian 11), não MySQL 8.4
LTS. Mantido como baseline operacional válido por decisão explícita do responsável pelo
produto — `utf8mb4_unicode_ci` usado no lugar de `utf8mb4_0900_ai_ci` (exclusiva do MySQL
8.0+) para compatibilidade. Migrations 01–09 validadas do zero contra esse servidor; bugs
reais só detectáveis contra banco de verdade foram corrigidos (colunas inexistentes em
`roles`, `pv.version_label` vs `version_name`, `c.component_type` vs `c.technology`,
materialização Dapper de records posicionais com colunas nullable).

### ADR-0007 / ADR-P004 — Storage de anexos
Status: aceito (filesystem corporativo/local por trás de IFileStorage para o MVP com caminhos configuráveis e hashing seguro; S3/Azure Blob para revisão futura se necessário).

## Decisões abertas

### ADR-P005 — Sistema de chamados
Definir fonte e sincronização.

### ADR-P006 — Engine vetorial
Somente após benchmark e requisito de IA.

### ADR-P007 — Provedor LLM/embedding
Critérios: segurança, contrato, custo, região, latência, modelos e integração .NET.

### ADR-P008 — Editor de conteúdo
Definir editor Markdown/rich text compatível com Razor Pages (ver ADR-0004) e sanitização.

### ADR-P009 — Notificações
In-app, e-mail, Teams/Slack, ou combinação.

### ADR-P010 — Estratégia de implantação
Windows Service/IIS, Linux/container, plataforma corporativa existente.

### ADR-P011 — Projeto TraceCore.Api e Unificação de Runtime
Status: **Órfão / Reservado para fase técnica futura**.
A arquitetura original (09 §3) previa endpoints REST dedicados no projeto `TraceCore.Api`. No entanto, para unificar a autenticação de sessão corporativa, injeção de dependência e minimizar complexidade operacional no monólito modular (ADR-0003 e ADR-0004), os endpoints HTTP (`/api/cases/...`) foram implementados diretamente no pipeline do `TraceCore.Web/Program.cs`.
O projeto `TraceCore.Api` permanece desativado na inicialização (`TraceCore.slnLaunch.user`) para prevenir conflitos e erro 404 de portas. A separação formal de uma API autônoma é mantida como `TODO` para uma fase técnica dedicada.

### ADR — Administração (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. Papel consolidado como `Admin` único (antigo `Super Admin (Dev)` renomeado na base e código). Implementado guard obrigatório no `UserService` que impede a desativação ou remoção do papel do último usuário `Admin` ativo, prevenindo lockout administrativo.

### ADR — Organização (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. `Departamento` é o único conceito organizacional oficial (`departments`). A duplicidade conceitual com `Team` foi eliminada, removendo as tabelas `teams` e `user_teams` e seu código associado via Migration 10. Testes de não-silo garantem cooperação interdepartamental.

### ADR — Compartilhamento e Transversalidade (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. Conhecimento e casos são patrimônio corporativo transversal; o pertencimento a um departamento não cria silo de visibilidade técnica, resguardados os níveis de confidencialidade aplicáveis.

### ADR — Cliente, Unidades e Contextos Técnicos (Fase 7.A — Bloco 7.A.1 e 7.A.6)
Status: **aceito**. Modelo de Clientes estendido com `external_crm_id` e `notes`, além das tabelas `client_units` (unidades físicas/filiais) e `client_technical_contexts` (matriz de versões e produtos contratados por unidade com vigência). Tabela `cases` estendida com `client_unit_id` para precisão contextual na triagem e pesquisa.

### ADR — Catálogo Técnico e Dependências (Fase 7.A — Bloco 7.A.2)
Status: **aceito**. Modelagem de dependências direcionadas entre componentes técnicos (`component_dependencies`) com tipo de dependência, criticidade e restrição de autorreferência (source != target). Atribuição de responsabilidade (`component_owners`) vinculada a Departamentos com papéis Primary/Secondary/Escalation. Suporte a componentes e produtos de terceiros via flag `is_external` em `products`.

### ADR — CaseIteration e Ciclo de Reabertura (Fase 7.A — Bloco 7.A.3)
Status: **aceito**. Implementação da entidade `CaseIteration` com sequência incremental. Reabertura formal permitida exclusivamente a partir do status `Resolved`, transitando para `Reopened` e criando uma nova iteração ativa. Histórico de iterações anteriores (passos, hipóteses, evidências e resoluções) permanece preservado e imutável. Constraint de unicidade de resolução migrada de `case_id` para `case_iteration_id`.

### ADR — Evidência Estruturada e Relação com Hipóteses (Fase 7.A — Bloco 7.A.4)
Status: **aceito**. Evidências passam a ser vinculadas a `case_iteration_id` e opcionalmente a `diagnostic_step_id`. Relação N:N estruturada entre evidências e hipóteses formalizada na tabela `case_hypothesis_evidence`, tipificada por `EvidenceRelationType` (`Supports`, `Contradicts`, `Inconclusive`, `Confirms`). Sugestão estruturada gerada sem persistência implícita nos passos diagnósticos.

### ADR — Pesquisa Híbrida e Identificadores Técnicos (Fase 7.A — Bloco 7.A.6)
Status: **aceito**. Busca federada enriquecida com detecção determinística de identificadores técnicos (número do caso, código de erro, padrões prefixados), destacando correspondências exatas em bloco prioritário no topo. Fatores de pontuação (`MatchedFactors`) calculados dinamicamente para catálogo de produtos, componentes e causas raiz, eliminando rótulos estáticos. Suporte a filtros combinados de Cliente, Unidade e Tecnologia.

### ADR — Casos Relacionados Determinísticos e Insight Agregado (Fase 8)
Status: **aceito**. 
1. **Princípio P-006**: Similaridade entre casos é calculada deterministicamente através de pesos sobre sinais contextuais reais (mesmo produto, componente, versão, código de erro e sobreposição léxica ponderada). O score normalizado (0 a 100) nunca é apresentado como probabilidade de causa raiz, e a interface obrigatoriamente exibe os `MatchedFactors` que justificam a correspondência.
2. **Modelo de Relações (BR-030)**: Tabela `case_relations` suporta vínculos computados (`Similar`) e asserções manuais do usuário (`Duplicate`, `Recurrence`, `CommonCause`, `Dependency`, `Reference`). Idempotência garantida via restrição `UNIQUE (source_case_id, target_case_id, relation_type)`. Relações manuais são auditadas em `audit_events` e protegidas pela permissão `caso.relacionar`.
3. **Insight Agregado (BR-048)**: Para casos similares resolvidos com amostra confiável $M \ge 3$, o sistema agrega a ação investigativa bem-sucedida mais frequente (passo com `Outcome = Worked`) ou ação de resolução/componente predominante, gerando recomendação prescritiva no padrão `"N de M casos semelhantes foram resolvidos verificando/agindo sobre: <ação/componente>"`.

### ADR — Diagnóstico Guiado e Motor Heurístico Sem Pseudo-Probabilidades (Fase 9)
Status: **aceito**.
1. **Grafo em Banco vs. Árvore em Código (05 §2 e §9)**: O grafo de perguntas, opções, impactos e hipóteses candidatas é 100% persistido e gerenciável em banco de dados (`diagnostic_flows`, `diagnostic_flow_hypotheses`, `diagnostic_checks`, `diagnostic_check_options`, `diagnostic_check_impacts`). Proibida qualquer árvore rígida em código C#.
2. **Heurística de Triagem vs. Certeza Estatística (05 §4 e §7, Princípio P-006)**: Em estrito respeito às diretrizes corporativas, termos como "probabilidade científica" ou porcentagens ilusórias ("88% de precisão") foram totalmente extirpados do motor e das telas. O algoritmo calcula um ranking causal determinístico baseado na relação entre o poder discriminativo do teste, confiabilidade das evidências e os custos/riscos da checagem:
   $$\text{prioridade} = \frac{\text{poder\_discriminativo} \times \text{confiabilidade}}{\text{custo} + \text{risco} + 1}$$
   Apresentado ao operador sob níveis ordinais (`Alta`, `Média`, `Baixa`) com a justificativa de qual hipótese o teste elucida ou descarta.
3. **Persistência na Linha do Tempo e Imutabilidade (Fase 4, BR-071 e BR-075)**: Hipóteses sugeridas são criadas formalmente como instâncias de `CaseHypothesis` com `SourceType = "Guided"`. Cada pergunta respondida gera um registro auditado de `DiagnosticStep` com `StepType = GuidedQuestion` e resultados válidos do enum (`Worked`, `DidNotWork`, `Inconclusive`). Quando uma opção atinge impacto conclusivo acumulado ($\ge 2.0m$), a hipótese é transicionada imutavelmente via método de domínio `Evaluate`. Se o operador optar por ignorar uma checagem recomendada, a justificativa técnica é obrigatória e gravada na linha do tempo sob o tipo `RecommendationIgnored` (BR-075).
4. **Escalonamento Textual Inteligente (05 §8)**: Ao atingir 5 ou mais checagens consecutivas sem resolução ou descarte efetivo de hipóteses, o sistema emite um alerta explícito sugerindo transferência ou escalonamento para nível 3/especialista com empacotamento do histórico de testes já realizados, evitando retrabalho na linha de frente.

### ADR — Analytics Operacional, Inteligência Gerencial e Métricas Éticas (Fase 10)
Status: **aceito**.
1. **Camada Dedicada de Analytics e Queries Agregadas (M08)**: Criação de `IManagementAnalyticsRepository` com implementações específicas e otimizadas em MySQL/Dapper e InMemory. Proibido carregar tabelas completas para memória ou dispersar fórmulas SQL em Razor Pages. A camada `IManagementAnalyticsService` é a única fonte de verdade para as métricas.
2. **Definições Conceituais das Métricas e MTTR por Iteração**:
   - **Casos Abertos**: `status IN ('Open', 'Reopened')`. Não existe o estado inventado 'Investigating'.
   - **Casos Resolvidos**: iteração mais recente com `status = 'Resolved'` e caso com `status = 'Resolved'`.
   - **MTTR Operacional por Iteração**: $\text{AVG}(ClosedAt - OpenedAt)$ sobre iterações resolvidas. O tempo não acumula iterações antigas para não penalizar artificialmente o tempo de resposta do ciclo reaberto.
   - **Mediana Determinística**: Projeção ordenada de durações em minutos calculada deterministicamente via código central sobre a amostra filtrada, assegurando compatibilidade total com MariaDB 10.5+ e In-Memory.
   - **Critério Determinístico de Recorrência**: Um caso conta como recorrente exclusivamente quando existir vínculo em `case_relations` com `relation_type IN ('Recurrence', 'CommonCause')`. Vínculos do tipo `Similar` (computados textualmente) não caracterizam reincidência formal.
3. **Princípios Éticos de Gestão (Sem Ranqueamento e Sem Score)**:
   - Extirpados quaisquer leaderboards, notas pejorativas ou comparações competitivas entre profissionais. A visão de usuários reflete atuação contextual e perfil técnico emergente observado nas ocorrências reais.
   - A visão de departamentos é transversal e colaborativa, integrando responsabilidade direta do caso, donos de componentes afetados e operadores de diagnóstico.
4. **Governança Factual da Base de Conhecimento**: Métricas de eficácia extraídas exclusivamente dos desfechos auditados em `KnowledgeUsage` (`Worked`, `PartiallyWorked`, `DidNotWork`), sem porcentagens artificiais de "redução de tempo". Mapeamento explícito de artigos nunca revisados (`LastReviewedAt IS NULL`) e casos resolvidos sem solução documentada.
5. **Drill-down Unificado**: Todo card e gráfico de KPI compartilha o mesmo modelo de filtro (`AnalyticsFilterDto`) e direciona diretamente para `/Cases/Index` com os parâmetros equivalentes na query string.


