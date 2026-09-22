# 15 — Roadmap de implementação

## 0. Estratégia geral

Construir valor em camadas. A plataforma deve ser útil antes da IA. O erro a evitar é iniciar pela LLM e deixar para depois o modelo de conhecimento, autorização e qualidade dos dados.

## Status geral (atualizado conforme implementação real)

O projeto está na **Fase 17 (arquitetura de provedores IA desacoplados)**: tabelas `llm_providers`/`llm_model_configs`, protocolos `OpenAICompatible`/`AnthropicMessages`, catálogo dinâmico de modelos no painel `Settings/LlmProviders` e segredos criptografados em `ISecretStore` (`ProtectedFileSecretStore`). São 29 migrations FluentMigrator (M20260917_01 a M20260922_29) e 105 testes de integração aprovados em `tests/TraceCore.IntegrationTests`. As fases abaixo marcam o estado real de cada entrega.

## Fase 0 — Fundação técnica — ✅ Concluída

Entregas:
- solution .NET (TraceCore.Web, Domain, Application, Infrastructure, Api, Contracts, Shared, Worker);
- padrões de projeto (Clean Architecture / Fase-17);
- autenticação e autorização por claims/permissões granulares;
- MySQL e migrations FluentMigrator (29 migrations);
- auditoria base (append-only);
- observabilidade básica;
- Worker para processos em background.

## Fase 1 — Organização e catálogo — ✅ Concluída

Entregas:
- usuários;
- departamentos (conceito oficial único; `teams`/`user_teams` dropados na Migration 10);
- perfis/permissões;
- clientes (com unidades `client_units` e contextos técnicos `client_technical_contexts`);
- produtos (com suporte a `is_external`);
- versões (`product_versions`);
- componentes;
- ambientes;
- tecnologias e tags;
- integrações (`integrations`);
- dependências direcionadas (`component_dependencies`);
- responsáveis por componente (`component_owners`).

Critério de saída: empresa consegue modelar o ecossistema técnico real. ✅ atendido.

## Fase 2 — Casos e linha do tempo — ✅ Concluída

Entregas:
- abertura de caso (relato original imutável + resumo normalizado editável);
- sintomas;
- evidências (`case_evidences` com relação N:N tipificada `case_hypothesis_evidence`);
- hipóteses (`case_hypotheses`);
- passos diagnósticos (`diagnostic_steps`);
- iterações e reabertura (`case_iterations`);
- resolução (`case_resolutions` vinculada à iteração ativa);
- causa raiz (`root_causes`);
- relacionamentos (`case_relations`);
- revisão pós-incidente.

Critério de saída: um incidente real pode ser documentado do início ao fim. ✅ atendido.

## Fase 3 — Conhecimento e soluções — ✅ Concluída

Entregas:
- artigos estruturados (`knowledge_items`);
- versionamento;
- revisão/publicação;
- aplicabilidade;
- steps/rollback/validação;
- uso em casos (`knowledge_usage`);
- estatística de resultado (`Worked`/`PartiallyWorked`/`DidNotWork`);
- revisão periódica (`review_due_at`).

## Fase 4 — Busca e filtros — ✅ Concluída

Entregas:
- busca exata + FULLTEXT + híbrida ponderada (`SearchService`);
- filtros;
- ranking e fatores de correspondência;
- explicabilidade ("por que apareceu");
- casos relacionados;
- histórico de consulta.

## Fase 5 — Diagnóstico guiado — ✅ Concluída (Fase 7)

Entregas:
- sessões de diagnóstico;
- grafo de verificações em banco (`diagnostic_checks`, `diagnostic_check_steps`);
- hipóteses por domínio/componente;
- reordenação/ranqueamento determinístico (heurística);
- escalonamento com pacote de contexto;
- checks automatizados vinculados a integrações (BR-073, health-check real HTTP/TCP).

## Fase 6 — Analytics e gestão — ✅ Concluída (Fases 10 e 16)

Entregas:
- Dashboard Geral (`/Analytics/Index`) com KPIs e séries temporais;
- visões Departamentos, Usuários e Conhecimento;
- Inteligência Analítica Determinística (`IManagementAnalyticsService`): tendência pós-versão, associação percentual de componentes e eficácia comparada de soluções;
- Qualidade & IA (`/ContentQuality/Index`);
- drill-down unificado para `/Cases/Index`.

## Fase 7 — Integrações — ✅ Concluída (Fases 8 e 15)

Entregas:
- catálogo de integrações (`integrations`);
- health-check real HTTP/GET/POST/HEAD e TCP socket com timeout;
- princípio de falha segura (nunca fingir sucesso);
- origem auditada (`TriggeredBy` Automated/Manual);
- diagnóstico automatizado via `DiagnosticCheck` ↔ `IntegrationId` (BR-073).
Pendente na vida real: conectores vendor-specific (ticketing/CRM/SAP) — ADRs P005 e P010 permanecem em aberto.

## Fase 8 — IA semântica / preparação estrutural — ✅ Concluída (Fase 12)

Entregas:
- `searchable_content_entries` com hash SHA-256 e status de prontidão explicável;
- pipeline determinístico `ContentPreparationService` com preservação técnica;
- painel de qualidade e prontidão.

## Fase 9 — RAG — ✅ Concluída (Fases 13, 14 e 16)

Entregas:
- Copiloto RAG grounded (`InvestigationCopilotService`) com pré-filtro híbrido, ranking de cosseno e citação obrigatória de fontes;
- tool calling estrito de leitura (`AnalyzeManagementTrend`);
- provedores desacoplados (`ILlmProviderResolver`, protocolos OpenAICompatible/AnthropicMessages);
- `ISecretStore` para API keys (`llm_apikey_{providerCode}`, criptografadas em repouso via ASP.NET Core Data Protection);
- catálogo dinâmico de modelos (`ILlmModelCatalog`) com busca de modelos via API no painel `Settings/LlmProviders`;

## Fase 10 — Copiloto de diagnóstico — Parcial

Somente após métricas e segurança:
- geração de perguntas: parcial (Copiloto responde com base em RAG grounded);
- proposta de próximos testes: planejado (tool calling de leitura já existente);
- correlação de evidências: planejado;
- automações de baixo risco: somente via health-check integrado (BR-073).

## Estratégia de entregas

Cada fase produz um incremento usável. Usuários-piloto validam taxonomia e fluxo cedo. Roadmap original detalhado e fases futuras (Copiloto de diagnóstico completo, conectores vendor-specific, notificações — ADR-P009) permanecem como backlog.

