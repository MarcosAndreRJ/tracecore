# 15 — Roadmap de implementação

## 0. Estratégia geral

Construir valor em camadas. A plataforma deve ser útil antes da IA. O erro a evitar é iniciar pela LLM e deixar para depois o modelo de conhecimento, autorização e qualidade dos dados.

## Fase 0 — Fundação técnica

Entregas:
- solution .NET;
- padrões de projeto;
- autenticação básica;
- autorização;
- MySQL e migrations;
- auditoria base;
- observabilidade;
- CI;
- ambientes;
- feature flags;
- outbox/worker.

Saída: aplicação vazia, mas operacionalmente sólida.

## Fase 1 — Organização e catálogo

Entregas:
- usuários;
- departamentos/equipes;
- perfis/permissões;
- clientes;
- produtos;
- versões;
- componentes;
- tecnologias;
- integrações;
- dependências;
- responsáveis.

Critério de saída: empresa consegue modelar o ecossistema técnico real.

## Fase 2 — Casos e linha do tempo

Entregas:
- abertura de caso;
- relato original;
- sintomas;
- evidências;
- hipóteses;
- passos diagnósticos;
- handoffs;
- resolução;
- causa raiz;
- relacionamentos;
- timeline completa.

Critério de saída: um incidente real pode ser documentado do início ao fim sem ferramenta paralela para memória técnica.

## Fase 3 — Conhecimento e soluções

Entregas:
- artigos estruturados;
- versionamento;
- revisão/publicação;
- aplicabilidade;
- steps;
- rollback;
- uso em casos;
- estatística de resultado;
- revisão periódica.

Critério de saída: caso resolvido pode virar conhecimento reutilizável e governado.

## Fase 4 — Busca e filtros

Entregas:
- busca exata;
- FULLTEXT;
- filtros;
- ranking inicial;
- “por que apareceu”;
- casos relacionados;
- histórico de consulta;
- feedback.

Critério de saída: usuário encontra casos e soluções sem conhecer previamente a área responsável.

## Fase 5 — Diagnóstico guiado

Entregas:
- sessões;
- perguntas adaptativas;
- hipóteses;
- verificações;
- reordenação;
- escalonamento com pacote de contexto;
- primeira biblioteca de fluxos.

Começar por 5–10 famílias de sintomas mais recorrentes, não tentar modelar toda a empresa de uma vez.

## Fase 6 — Analytics e gestão

Entregas:
- agregações;
- dashboards;
- drill-down;
- visões por usuário/equipe/cliente/tecnologia;
- pesquisa e qualidade de conhecimento;
- exportações.

Critério de saída: decisões operacionais podem ser tomadas com dados rastreáveis.

## Fase 7 — Integrações

Priorizar conforme valor:
- chamados;
- monitoramento;
- telemetria dos produtos;
- SAP;
- diretório corporativo.

## Fase 8 — IA semântica

- embeddings;
- índice vetorial;
- busca híbrida;
- dataset de avaliação;
- suggestion de similares.

## Fase 9 — RAG

- assistente;
- fontes;
- feedback;
- sumarização;
- rascunhos;
- governança de prompts/modelos.

## Fase 10 — Copiloto de diagnóstico

Somente após métricas e segurança:
- geração de perguntas;
- proposta de próximos testes;
- correlação de evidências;
- automações de baixo risco explicitamente aprovadas.

## Estratégia de entregas

Cada fase deve produzir um incremento usável. Evitar branch de desenvolvimento de meses sem uso real. Inserir usuários-piloto cedo para validar taxonomia e fluxo.

