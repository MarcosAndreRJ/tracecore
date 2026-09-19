# 03 — Módulos do sistema

## M01 — Identidade e Acesso

Responsável por autenticação, usuários, papéis, permissões, sessões e políticas de segurança.

Telas principais:
- Meu perfil.
- Usuários.
- Perfis e permissões.
- Sessões/dispositivos, se habilitado.
- Eventos de segurança.

## M02 — Estrutura Organizacional

Departamentos (`departments`), gestores, responsáveis técnicos e matrizes de escalonamento. O conceito oficial único de organização é o **Departamento** (a duplicação com `teams` foi extinta na Fase 7.A). Estruturas transversais são suportadas sem silos rígidos entre áreas.

## M03 — Catálogo Técnico

Representa o ecossistema suportado com telas de gestão integrada (`/Catalog/Products`, `/Catalog/Components`):

- clientes (`clients`) com `external_crm_id`, `notes`, unidades (`client_units`) e contextos técnicos (`client_technical_contexts`);
- produtos (`products`) com suporte a produtos externos (`is_external`);
- componentes técnicos (`components`);
- versões de produto (`product_versions`);
- ambientes (`environments`);
- tecnologias e tags;
- dependências direcionadas entre componentes (`component_dependencies`) com tipo e criticidade;
- responsáveis por componente (`component_owners`) atribuídos a Departamentos com papéis Primário, Secundário e Escalonamento.

O catálogo é essencial porque o mecanismo de similaridade depende do contexto técnico, não apenas do texto.

## M04 — Casos e Incidentes

É a memória factual do que aconteceu. Deve registrar o problema desde a entrada até o encerramento, incluindo linha do tempo.

Submódulos:
- abertura com relato original imutável e resumo normalizado editável;
- triagem e contextualização técnica (cliente, unidade, produto, versão, componente, erro);
- iterações e ciclo de reabertura (`case_iterations`), preservando histórico integral de hipóteses e diagnósticos;
- investigação e hipóteses (`case_hypotheses`);
- passos diagnósticos estruturados (`diagnostic_steps`) com sugestão contextual de evidências;
- evidências estruturadas (`case_evidences`) com relação N:N tipificada com hipóteses (`case_hypothesis_evidence`);
- escalonamentos e sessões de diagnóstico;
- resolução (`case_resolutions`) associada à iteração ativa do caso;
- taxonomia de causa raiz corporativa (`root_causes`);
- relacionamento entre casos (`case_relations`);
- revisão pós-incidente.

## M05 — Base de Conhecimento e Soluções

É a camada de conhecimento reutilizável. Diferente de um caso, uma solução deve ser generalizada e declarar quando se aplica.

Tipos previstos:
- solução/troubleshooting;
- procedimento operacional;
- artigo técnico;
- FAQ;
- runbook;
- alerta conhecido/known issue;
- referência de integração;
- lição aprendida.

## M06 — Pesquisa e Similaridade

Responsável por consulta global, filtros, ranking, sugestões, histórico de busca e explicabilidade.

Deve possuir uma interface única capaz de consultar:
- soluções;
- casos;
- documentação;
- componentes;
- mensagens/códigos de erro;
- causas raiz.

## M07 — Diagnóstico Guiado

Camada operacional que conduz investigação a partir do sintoma. Não é uma árvore fixa gigante. O modelo recomendado é um **grafo de verificações**, em que cada verificação possui custo, risco, pré-condições e relação com hipóteses.

## M08 — Analytics e Inteligência Gerencial

Consolida fatos operacionais e estratégicos através de queries agregadas eficientes no banco (`IManagementAnalyticsRepository`), eliminando o carregamento de tabelas inteiras para memória e cálculos divergentes.

Páginas e Módulos Entregues (Fases 10 e 16):
- **Dashboard Geral (`/Analytics/Index`)**: KPIs de casos abertos (`Open`/`Reopened`), resolvidos, MTTR por iteração (`AVG(ClosedAt - OpenedAt)`), mediana, incidentes recorrentes (`Recurrence`/`CommonCause`), sem causa raiz confirmada e sem conhecimento publicado. Séries temporais, sistemas e componentes mais impactados e tabela de casos que requerem atenção com drill-down unificado para `/Cases/Index`.
- **Inteligência Analítica Determinística (Fase 16 / §31)**: Consultas analíticas puras no banco via `IManagementAnalyticsService`:
  - `GetTrendAfterVersionAsync`: variação de volume e MTTR antes vs. depois da publicação de uma versão de produto, com cálculo determinístico de mediana e suficiência de amostra.
  - `GetComponentAssociationPercentageAsync`: distribuição percentual exata de componentes associados a causas e sintomas técnicos.
  - `GetSolutionEffectivenessComparisonAsync`: comparação factual de mediana de MTTR de casos resolvidos com vs. sem uso de uma solução oficial, declarando expressamente insuficiência estatística quando a amostra é reduzida ($N < 3$).
- **Departamentos (`/Analytics/Departments`)**: Visão transversal sem silos ou rankings pejorativos, mensurando volume ativo, resolvido, MTTR, reaberturas, reincidências e autoria de conhecimento cruzando casos diretos, donos de componentes afetados e operadores de diagnóstico.
- **Usuários (`/Analytics/Users`)**: Métricas de engajamento técnico individual e colaboração (casos, resoluções, passos diagnósticos, hipóteses, evidências, autoria e reutilização de artigos), além de perfil técnico emergente baseado em dados reais de atuação recente, sem pontuações artificiais de desempenho.
- **Conhecimento (`/Analytics/Knowledge`)**: Eficácia factual baseada em `KnowledgeUsage` (desfechos `Worked`, `PartiallyWorked`, `DidNotWork`), ciclo de revisão (nunca revisados, revisões vencidas) e lacunas de documentação (resolvidos sem artigo e recorrentes sem causa raiz).

## M09 — Auditoria e Governança

Trilha imutável de ações, alterações estruturais e eventos de conformidade corporativa (Fase 11):
- **Modelo Append-Only Imutável**: Entidade `AuditEvent` sem endpoints ou métodos de remoção/modificação (`IAuditEventRepository`).
- **Sanitização de Dados Sensíveis (BR-101)**: Máscara automática de senhas, tokens, hashes e segredos (`***REDACTED***`) via `AuditService.RecordAsync`.
- **Convenção Corporativa Unificada**: Nomenclatura no formato `entidade.verbo[_objeto]` (ex.: `user.create`, `product.create`, `component.dependency_create`, `case.reopen`).
- **Fechamento de Gaps**: Auditoria em Catálogo Técnico (`CatalogService` com produtos, versões, componentes, dependências e owners), Casos (`CaseService`), Investigação (`CaseInvestigationService` com vínculo de evidência a hipótese) e Conhecimento.
- **Painel de Auditoria (`/Audit/Index`)**: Consulta paginada com filtros superiores por período, ator/usuário, ação, entidade, ID e busca livre em diffs e metadados, com modal de inspeção de payload (`BeforeJson`, `AfterJson`, `MetadataJson`) e drill-down para entidades navegáveis.

## M10 — Integrações

Módulo de catálogo e conexões operacionais do ecossistema TraceCore (Fases 8 e 15):
- **Catálogo Administrativo**: Cadastro centralizado de integrações (`integrations`) com status manual (`Configured`, `Active`, `Inactive`, `Error`), tipo e notas de contrato. As ADRs P005 (conectores ticketing vendor-specific) e P010 (conectores SAP vendor-specific) permanecem em aberto.
- **Health-Check Real HTTP / TCP (Fase 15)**: Serviço `IIntegrationHealthCheckService` com suporte a sondagem determinística via HTTP (GET/POST/HEAD) e ping de socket TCP com timeout configurável.
- **Princípio de Falha Segura (§26)**: Falhas de conexão, rede inacessível, timeout ou status code inesperado registram expressamente `Status = "Failed"` no histórico (`integration_runs`), NUNCA fingindo sucesso.
- **Origem Auditada**: Histórico de execuções com coluna/badge de origem (`TriggeredBy = "Automated"` vs `"Manual"`).
- **Diagnóstico Guiado Automatizado (BR-073)**: Vinculação de `DiagnosticCheck` (`CheckType = AutomatedCheck`) a uma `IntegrationId`. Quando o motor de diagnóstico alcança esse passo, o health-check executa automaticamente sem intervenção humana, gravando o passo como `AutomatedCheck` e avançando a investigação. Se não houver integração vinculada, recai suavemente para pergunta manual ao operador.

## M11 — IA e Preparação Estrutural de Dados

Módulo desacoplado do núcleo da plataforma, responsável pela estruturação, governança e preparação de dados corporativos para inteligência assistiva e semântica (Fase 12):
- **Entidade `SearchableContentEntry`**: Tabela `searchable_content_entries` com chave natural (`source_type`, `source_id`, `source_version_id`), normalização textual, hash SHA-256 e status explicáveis.
- **Normalização com Preservação Técnica**: Pipeline determinístico em `ContentPreparationService` que preserva termos de engenharia literais (`ORA-12541`, `HTTP 500`, `/api/...`, `v8.2.1`).
- **Ingestão Estruturada de Casos e Conhecimento**:
  - Casos: iteração atual, sintomas, componentes afetados, evidências estruturadas com tipo e hipóteses vinculadas, resolução e causa raiz confirmada.
  - Conhecimento: código, resumo, problema, causa raiz, validação, riscos, rollback, aplicabilidades e tecnologias vinculadas.
- **Prontidão Explicável (Sem Scores Probabilísticos)**: Classificação transparente de prontidão para IA (`Ready`, `NeedsMetadata`, `NeedsReview`, `NotEligible`) baseada em integridade factual, sem scores numéricos ou LLMs nesta fase.
- **Visibilidade de Segurança Desacoplada**: Acesso derivado da confidencialidade real (`Public`, `Internal`, `Confidential`, `Restricted`), sem isolamento artificial por departamento.
- **Painel de Qualidade e Prontidão (`/ContentQuality/Index`)**: Métricas de elegibilidade em tempo real, sincronização em lote de fontes e inspeção de payloads estruturados reaproveitando os componentes visuais corporativos (`_AIContentBadge`, `_KnowledgeProvenance`, `_SourceReferenceChip`, `_ConfidenceIndicator`).

## M12 — Administração e Copiloto Operacional

Configurações gerais, taxonomias, jobs, parâmetros, provedores de IA e Copiloto Operacional (Fases 13, 14 e 16):
- **Copiloto RAG Grounded (BR-080 a BR-086)**: Pipeline assistivo disparado por ação explícita do usuário, com pré-filtro híbrido por visibilidade, ranking de cosseno e citação obrigatória de fontes oficiais.
- **Tool Calling de Leitura (Fase 16)**: Ferramenta de leitura estrita `AnalyzeManagementTrend` em `AiToolDefinitions.ReadingTools`, permitindo ao Copiloto consultar métricas e tendências determinísticas do TraceCore.
- **Preservação Factual (§31)**: Apresentação transparente de painel com os dados brutos calculados (`ToolResults`) e instrução de sistema que proíbe o LLM de inventar indicadores, garantindo que o número exibido venha exclusivamente da base primária do TraceCore.
- **Atalho Contextual**: Acesso direto via query string `?Question=...` a partir das telas de Analytics e Diagnóstico.

