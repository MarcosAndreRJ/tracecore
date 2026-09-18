# 07 — Analytics e gestão

## 1. Princípio

Analytics deve apoiar decisões reais: onde há gargalo, o que mais reincide, que conhecimento falta, quais componentes concentram falhas, quanto tempo se perde em handoffs e quais soluções efetivamente reduzem tempo de resolução.

Evitar dashboards decorativos.

## 2. Dashboard executivo

KPIs principais:
- casos abertos/resolvidos por período;
- backlog e envelhecimento;
- MTTA (tempo até primeira atuação);
- MTTR (tempo até resolução);
- reincidência em 7/30/90 dias;
- casos críticos;
- componentes com maior impacto;
- clientes com maior volume/impacto;
- percentual de casos que reutilizaram conhecimento existente;
- economia estimada de tempo por reutilização;
- lacunas críticas de conhecimento.

Gráficos:
- tendência mensal/semanal;
- Pareto de causas;
- Pareto de componentes;
- distribuição de tempo de resolução;
- heatmap por dia/horário;
- fluxo de handoffs;
- top recorrências.

## 3. Dashboard operacional

- casos por estado;
- casos sem responsável;
- casos sem atualização;
- SLA/SLO interno, se existir;
- severidade;
- fila por equipe;
- tempo em cada etapa;
- quantidade de handoffs;
- hipóteses mais frequentes;
- etapas diagnósticas que mais resolvem/eliminam hipóteses;
- casos semelhantes abertos simultaneamente.

## 4. Dashboard de conhecimento

- artigos por estado;
- artigos com revisão vencida;
- conhecimento sem proprietário;
- artigos mais usados;
- artigos com maior sucesso observado;
- artigos com falhas recorrentes;
- artigos que nunca foram usados;
- artigos mais encontrados em busca;
- artigos com feedback negativo;
- soluções por produto/componente;
- lacunas: alta incidência de casos sem solução reutilizável.

Sempre mostrar tamanho da amostra ao falar de taxa de sucesso.

## 5. Dashboard de pesquisa

- volume;
- termos principais;
- zero result;
- zero click;
- consultas reformuladas;
- filtros;
- conteúdo aberto;
- pesquisas que terminaram em solução;
- consultas que geraram escalonamento;
- termos emergentes.

## 6. Dashboard de tecnologia

- incidentes por produto/módulo/componente;
- causa por componente;
- versões com maior incidência;
- incidentes após release;
- integrações com maior falha;
- dependências críticas;
- reincidência pós-correção;
- tempo de resolução por classe técnica.

## 7. Dashboard de cliente

- volume de casos;
- severidade;
- recorrência;
- produtos afetados;
- top sintomas;
- top causas;
- tempo de resolução;
- incidentes específicos do ambiente do cliente versus gerais;
- soluções mais usadas.

## 8. Visão de usuário/colaborador

Para gestores autorizados:
- casos em que participou;
- casos em que foi responsável;
- tempo de permanência em sua etapa, contextualizado;
- contribuições de conhecimento;
- revisões/aprovações;
- soluções utilizadas por outros;
- feedback recebido em conteúdos;
- histórico de ações administrativas/técnicas auditáveis;
- handoffs realizados/recebidos;
- áreas e tecnologias em que mais atuou.

### Importante
Não criar “nota geral do funcionário” ou ranking automático de pessoas. Métricas podem refletir complexidade, alçada, escala de trabalho e perfil de função. A tela deve apoiar gestão e desenvolvimento, não substituir análise humana.

## 9. Dashboard de diagnóstico

- perguntas mais feitas;
- perguntas que mais discriminam hipóteses;
- testes mais usados;
- testes com baixo valor e alto custo;
- caminhos mais comuns;
- momento em que casos são escalados;
- número médio de passos até solução;
- hipóteses frequentemente descartadas;
- caminhos que se repetem e merecem automação.

## 10. Dashboard de IA/RAG

Quando habilitado:
- perguntas realizadas;
- respostas com/sem fonte suficiente;
- latência;
- custo por modelo/provedor;
- tokens;
- taxa de feedback útil;
- taxa de citação aberta;
- casos em que sugestão foi usada;
- respostas rejeitadas;
- falhas de recuperação;
- conteúdos mais recuperados;
- avaliação de groundedness e precisão em dataset interno.

## 11. Modelo analítico

Não executar todos os gráficos diretamente sobre tabelas transacionais complexas. Criar:

- eventos de domínio;
- fatos agregados diários/horários;
- snapshots de backlog;
- tabelas de métricas materializadas por job;
- definições versionadas de KPI.

Exemplo de fatos:
- `fact_case_lifecycle`;
- `fact_case_handoff`;
- `fact_solution_usage`;
- `fact_search_session`;
- `fact_knowledge_review`;
- `fact_ai_interaction`.

## 12. Definições mínimas

**MTTR:** `resolved_at - opened_at`, com segmentação por severidade e exclusões explicitamente documentadas.  
**Reincidência:** novo caso relacionado à mesma causa/componente dentro de janela definida. A janela deve ser configurável e a fórmula versionada.  
**Reutilização de conhecimento:** caso em que uma solução existente foi vinculada e marcada como usada.  
**Sucesso de solução:** número de usos classificados como “funcionou” dividido por usos com resultado classificável; exibir amostra e parciais separadamente.

