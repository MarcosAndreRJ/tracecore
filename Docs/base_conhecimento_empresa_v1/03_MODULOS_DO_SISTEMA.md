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

Páginas e Módulos Entregues (Fase 10):
- **Dashboard Geral (`/Analytics/Index`)**: KPIs de casos abertos (`Open`/`Reopened`), resolvidos, MTTR por iteração (`AVG(ClosedAt - OpenedAt)`), mediana, incidentes recorrentes (`Recurrence`/`CommonCause`), sem causa raiz confirmada e sem conhecimento publicado. Séries temporais, sistemas e componentes mais impactados e tabela de casos que requerem atenção com drill-down unificado para `/Cases/Index`.
- **Departamentos (`/Analytics/Departments`)**: Visão transversal sem silos ou rankings pejorativos, mensurando volume ativo, resolvido, MTTR, reaberturas, reincidências e autoria de conhecimento cruzando casos diretos, donos de componentes afetados e operadores de diagnóstico.
- **Usuários (`/Analytics/Users`)**: Métricas de engajamento técnico individual e colaboração (casos, resoluções, passos diagnósticos, hipóteses, evidências, autoria e reutilização de artigos), além de perfil técnico emergente baseado em dados reais de atuação recente, sem pontuações artificiais de desempenho.
- **Conhecimento (`/Analytics/Knowledge`)**: Eficácia factual baseada em `KnowledgeUsage` (desfechos `Worked`, `PartiallyWorked`, `DidNotWork`), ciclo de revisão (nunca revisados, revisões vencidas) e lacunas de documentação (resolvidos sem artigo e recorrentes sem causa raiz).

## M09 — Auditoria e Governança

Trilha de ações, histórico de alterações, publicação/revisão de conteúdo, eventos de segurança, exportações e acesso a dados restritos.

## M10 — Integrações

Integração com:
- sistema de chamados;
- SAP;
- APIs corporativas;
- telemetria/logs;
- ferramentas de monitoramento;
- diretório corporativo;
- e-mail/notificação;
- pipelines/repositórios, se necessário.

Todo conector deve possuir isolamento e contrato próprio.

## M11 — IA e RAG

Fica desacoplado do núcleo. Consome interfaces de pesquisa e conhecimento. Pode ser desabilitado sem impedir funcionamento básico da plataforma.

Responsabilidades:
- embeddings/indexação semântica;
- recuperação híbrida;
- montagem de contexto;
- geração fundamentada;
- sumarização;
- classificação assistida;
- avaliação/feedback;
- governança de prompts e modelos.

## M12 — Administração da Plataforma

Configurações gerais, taxonomias, jobs, retenção, parâmetros, integrações, status de indexação, feature flags e manutenção.

