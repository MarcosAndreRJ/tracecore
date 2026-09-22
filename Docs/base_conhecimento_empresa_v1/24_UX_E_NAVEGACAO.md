# 24 — UX, navegação e desenho de telas

## 1. Princípio de UX

A plataforma é uma ferramenta de investigação. A UI deve reduzir carga cognitiva e manter contexto. Evitar formulários enormes e dashboards cheios de cards sem hierarquia.

## 2. Navegação principal (sidebar real — `_Layout.cshtml`)

Sidebar lateral retrátil (`tc-sidebar`) com navegação agrupada e itens condicionados a permissões (`User.HasClaim`):

```text
Visão Geral
├── Dashboard                    (/)            — sempre visível
├── Minha Área                   (/Users/Details) — usuário logado
└── Copiloto IA                  (/Copilot/Index) — permissão ia.usar

Operação
├── Casos                        (/Cases/Index)    — caso.visualizar | caso.criar
├── Diagnóstico                  (/Diagnosis/Index) — caso.diagnosticar
└── Pesquisa                     (/Search)         — sempre visível

Conhecimento
├── Soluções                     (/Knowledge/Index) — sempre visível
├── Lições Aprendidas            (/Knowledge/Index?category=lessons)
└── Documentação                 (/Placeholder?module=Documentação) — simulação

Ecossistema  [se catalogo.gerenciar | integracao.gerenciar]
├── Sistemas                     (/Catalog/Products/Index) — catalogo.gerenciar
├── Componentes                  (/Catalog/Components/Index) — catalogo.gerenciar
└── Integrações                  (/Integrations/Index) — integracao.gerenciar

Inteligência  [se analytics.visualizar]
├── Dashboard Geral              (/Analytics/Index)
├── Departamentos                (/Analytics/Departments)
├── Usuários                     (/Analytics/Users)
├── Conhecimento                 (/Analytics/Knowledge)
└── Qualidade & IA               (/ContentQuality/Index)

Gestão
├── Usuários                     (/Users/Index)  — usuario.gerenciar
├── Departamentos                (/Departments/Index) — usuario.gerenciar
├── Clientes                     (/Clients/Index) — cliente.gerenciar
├── Perfis e Permissões          (/Placeholder) — permissao.gerenciar (simulação)
└── Causas Raízes                (/Cases/RootCauses/Index) — caso.encerrar

Administração
├── Auditoria                    (/Audit/Index) — auditoria.visualizar
└── Configurações                (/Settings/Index) — configuracao.gerenciar
```

Topbar: busca global (`/Search?q=`) com atalho Ctrl+K, botão "Novo Caso" (caso.criar) e menu do usuário (Meu Perfil / Sair). Rodapé indica TraceCore v1.0 Enterprise. "Perfis e Permissões" e "Documentação" ainda são placeholders (`/Placeholder`). Design System TraceCore carregado em ordem: `tokens.css` → `base.css` → `layout.css` → componentes (`buttons`, `forms`, `badges`, `cards`, `tables`, `modals`, `alerts`, `empty-state`, `loading`, `tabs`, `filters`, `breadcrumb`, `pagination`) + domínio (`hypothesis`, `investigation`, `evidence`, `knowledge`, `metrics`, `dependency-graph`). Tema claro, tokens CSS, ícones Bootstrap Icons e acessibilidade WCAG 2.2 AA (NFR-007) com skip-link.

## 3. Início

Blocos (Dashboard `/`):
- KPIs de visão geral (casos abertos, resolvidos, MTTR médio/mediana, reincidência);
- séries temporais e componentes mais impactados;
- atalho para "Novo Caso" `(/Cases/Create)`;
- busca global no topo;
- painéis de Inteligência e drill-down para `/Cases/Index`.

## 4. Pesquisa

Tela real: `/Search`. Layout desktop implementado com topo de busca global e filtros laterais combinados (Cliente, Unidade, Tecnologia, entre outros).

Resultado mostra:
- título;
- tipo;
- resumo/snippet;
- compatibilidade;
- status;
- fatores de correspondência (`MatchedFactors` — blocos prioritários para identificadores técnicos exatos);
- uso/sucesso com amostra quando aplicável.

## 5. Caso

Telas reais: `/Cases/Index` (lista com filtros e drill-down de Analytics), `/Cases/Details` (cabeçalho fixo com número, status, severidade, cliente, produto e owner), `/Cases/Create` (relato original + triagem contextual opcional, BR-022/FR-042) e `/Cases/RootCauses/Index` (taxonomia de causa raiz).

Áreas de detalhe implementadas (visão geral, diagnóstico, timeline, evidências, relacionados, resolução) via partiais corporativas (`_InvestigationTimeline`, `_HypothesisCard`, `_EvidenceCard`, `_DiagnosticStepCard`, `_RelatedCaseCard`).

- **Detalhe** diferencia visualmente observação, teste, hipótese, handoff, ação e resolução, sem depender apenas de cor (acessibilidade).

## 6. Diagnóstico

Telas reais: `/Diagnosis/Index` e `/Diagnosis/Flows/Index` (grafo de verificações em banco). A tela apresenta contexto do caso, hipóteses ranqueadas pelo motor (`DiagnosticEngineService`), próxima verificação sugerida, histórico testado e semelhantes.

Cada verificação possui botões rápidos de resultado e integração opcional de health-check automatizado (BR-073).

## 7. Conhecimento

Telas reais: `/Knowledge/Index` (soluções e lições aprendidas com filtro `?category=lessons`) e `/Knowledge/Details`. A visualização do artigo contém status e revisão, aplicabilidade, conteúdo, riscos/rollback destacados, casos que validaram, histórico de versões e ação "usar neste caso" (`KnowledgeUsage`).

## 8. Analytics

Telas reais: `/Analytics/Index` (Dashboard Geral), `/Analytics/Departments`, `/Analytics/Users`, `/Analytics/Knowledge` e `/Analytics/Knowledge`/"Qualidade & IA" (`/ContentQuality/Index`). Padrões:
- filtros globais no topo;
- período sempre explícito;
- definição do KPI acessível;
- drill-down por clique (rolagem para `/Cases/Index`);
- estado "sem dados" diferente de zero;
- exportação conforme permissão.

## 9. Administração

Telas reais agrupadas por domínio:
- Pessoas e acesso: `/Users/Index`, `/Departments/Index`;
- Catálogo técnico: `/Clients/Index`, `/Catalog/Products/Index`, `/Catalog/Components/Index`;
- Integrações: `/Integrations/Index`;
- IA: `/Settings/LlmProviders/Index`, `/ContentQuality/Index`;
- Auditoria: `/Audit/Index`;
- Configurações: `/Settings/Index`;
- Perfis e Permissões: placeholder (`/Placeholder`).
- Organização: `/Departments/Index`.

## 10. Estados da interface

Toda tela assíncrona deve tratar:
- loading;
- vazio;
- erro recuperável;
- sem permissão;
- dado desatualizado/conflito de concorrência;
- indisponibilidade de integração/IA.

## 11. Confirmações

Não pedir confirmação para ações triviais. Exigir confirmação clara para:
- desativação;
- publicação;
- depreciação;
- mudança de permissão;
- encerramento crítico;
- ação irreversível.

