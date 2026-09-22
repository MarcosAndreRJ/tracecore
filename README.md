# TraceCore

**TraceCore** é uma plataforma corporativa de conhecimento, investigação e diagnóstico técnico criada para transformar problemas resolvidos em conhecimento reutilizável.

O sistema centraliza casos, hipóteses, evidências, soluções, contexto técnico e relações entre incidentes para ajudar equipes técnicas a investigar problemas com mais contexto, menos tentativa e erro e maior rastreabilidade.

> **Objetivo:** registrar o que aconteceu, como foi investigado, quais evidências influenciaram cada hipótese, qual solução funcionou e como esse conhecimento pode orientar ocorrências futuras.

---

## Estado do projeto

O desenvolvimento está implementado até a:

**Fase 18 — Versionamento Inteligente Completo (Fases 1 a 5 Consolidadas)**

São **31 migrations FluentMigrator** (`M20260917_01` a `M20260922_31`) e **228 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (com cobertura ponta a ponta dos cenários de catálogo, casos, investigações, similaridade, rollout e versionamento inteligente).

Módulos implementados: Identidade e Acesso, Estrutura Organizacional, Catálogo Técnico, Casos e Incidentes, Base de Conhecimento, Pesquisa/Similaridade, Diagnóstico Guiado, Analytics e Indicadores Gerenciais, Auditoria, Integrações, IA/RAG e Copiloto de Investigação com Tool Calling e **Versionamento Inteligente**.

---

## Versionamento Inteligente (Fases 1 a 5)

O subsistema de **Versionamento Inteligente** transforma números de versão em inteligência operacional e investigativa para atendimento, sustentação e engenharia de software:

1. **Fase 1 — Modelo Central & Release Order**:
   - `release_order` numérico único por produto (`product_versions.release_order`), garantindo ordenação cronológica real mesmo com nomenclaturas arbitrárias ou não-léxicas (ex: `5.9` lançado antes de `5.10`).
   - Alterações por versão (`product_version_changes`: Fix, Feature, Improvement, Internal, Security).
   - Vínculo alteração $\leftrightarrow$ casos (`product_version_change_cases`: `FixedBy`, `Related`, etc.), sem jamais mutar a versão original do caso.
   - Destinação/Rollout (`product_version_assignments`: Planned, Scheduled, Deployed, Failed, Skipped).

2. **Fase 2 — Contexto Técnico no Tempo**:
   - Histórico de versões ativas por cliente, unidade e ambiente com períodos de vigência (`EffectiveFrom` e `EffectiveTo`).
   - Atualização manual fora do rollout e transição automática com encerramento de vigência anterior.

3. **Fase 3 — Ranking & Sugestão Determinística**:
   - Motor determinístico de sugestão de casos para correções (`SuggestCasesForChangeAsync`), pontuando por cliente, produto, componente, código de erro e termos significativos, sem uso de LLM probabilístico.

4. **Fase 4 — Versão do Cliente e Correções Posteriores na Abertura**:
   - Detecção automática de versão vigente em `Cases/Create`.
   - Sugestão imediata de correções publicadas em versões posteriores à do cliente.
   - Distinção explícita na UI entre `OccurredInVersion` (onde ocorreu) e `FixedByVersion` (onde foi corrigido).

5. **Fase 5 — Histórico Versões $\times$ Casos, Analytics, Copiloto e Consolidação**:
   - **Visão Versão $\rightarrow$ Casos**: Aba dedicada em detalhes da versão listando todas as alterações, casos vinculados com status, cliente, versão de ocorrência e data.
   - **Indicadores por Versão**: Casos ocorridos, correções publicadas, casos vinculados, clientes planejados, implantados e pendentes.
   - **Observação Factual de Recorrência**: Métricas de casos semelhantes pós-release com redação estritamente factual ("*N ocorrência(s) semelhante(s) foi(ram) registrada(s) após a adoção da versão*"), sem acusações causais infundadas.
   - **Histórico Cliente $\times$ Versão**: Linha do tempo visual (`5.17.9 -> 5.18.2 -> 5.18.4`) com agrupamento de casos abertos no período de vigência de cada versão.
   - **Copiloto IA**: Ferramentas estruturadas `GetClientVersionContext` e `SearchVersionFixes`. O backend fornece os dados numéricos e fatos; o LLM interpreta com links internos markdown diretos (`/Catalog/Products/Details?id=...`, `/Cases/Details?id=...`).
   - **Dev Seed & Testes Automatizados**: Migration `M20260922_31` com cenário Atlas Transportes e 10 cenários de teste obrigatórios (A a I + Copiloto Tools).

---

## Funcionalidades implementadas

### Identidade e acesso

- autenticação;
- usuários;
- departamentos;
- perfis;
- permissões granulares;
- administrador do sistema;
- auditoria de operações.

O papel **Admin** representa o administrador raiz do TraceCore.

Departamentos representam contexto e responsabilidade, mas não funcionam como silos de conhecimento por padrão.

---

## Clientes

O TraceCore possui um cadastro operacional de clientes, sem tentar substituir o CRM da empresa.

São suportados:

- cliente;
- identificação externa no CRM;
- unidades;
- status;
- observações operacionais;
- contexto técnico histórico.

O contexto técnico permite relacionar um cliente a:

- produto/sistema;
- versão;
- ambiente;
- unidade.

Isso permite preservar, por exemplo, qual versão o cliente utilizava quando determinado incidente ocorreu.

---

## Catálogo técnico

O catálogo representa o ecossistema suportado pela empresa.

Atualmente estão modelados:

- produtos/sistemas;
- versões;
- ambientes;
- componentes;
- sistemas externos;
- dependências entre componentes;
- departamentos responsáveis.

Exemplo:

```text
Desktop
   ↓
API
   ↓
Serviço de autenticação
   ↓
Banco de dados
   ↓
Sistema externo