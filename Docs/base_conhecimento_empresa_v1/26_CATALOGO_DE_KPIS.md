# 26 — Catálogo inicial de KPIs

## KPI-001 — MTTA
**Pergunta:** quanto tempo levamos para iniciar atuação?  
**Fórmula:** `first_response_at - opened_at`.  
**Segmentar:** severidade, cliente, produto, equipe, origem.  
**Cuidados:** casos importados podem ter timestamp externo diferente.

## KPI-002 — MTTR
**Pergunta:** quanto tempo até resolver?  
**Fórmula base:** `resolved_at - opened_at`.  
**Cuidados:** definir política para períodos `AwaitingInfo` antes de comparar equipes.

## KPI-003 — Reincidência
**Pergunta:** o mesmo problema está voltando?  
**Fórmula:** casos ligados por mesma causa/componente em janela configurada / casos resolvidos elegíveis.  
**Cuidados:** exige boa qualidade de causa/relacionamento.

## KPI-004 — Reutilização de conhecimento
**Pergunta:** quanto do trabalho usa aprendizado anterior?  
**Fórmula:** casos com `knowledge_usage` / casos resolvidos elegíveis.

## KPI-005 — Sucesso observado de solução
**Fórmula:** usos `Worked` / usos classificáveis.  
**Exibir:** sucessos, parciais, falhas e N total.  
**Nunca:** mostrar percentual isolado.

## KPI-006 — Zero Result Rate
**Fórmula:** consultas com 0 resultados / consultas totais.

## KPI-007 — Zero Click Rate
**Fórmula:** consultas com resultados, mas sem abertura de item / consultas com resultados.  
**Interpretação:** pode indicar baixa relevância, mas também consulta exploratória; não concluir sozinho.

## KPI-008 — Handoffs por caso
**Fórmula:** quantidade média/mediana de transferências por caso.  
**Uso:** encontrar roteamento ruim e fronteiras problemáticas.

## KPI-009 — Tempo por etapa
Medir duração por estado/equipe para identificar espera versus investigação ativa.

## KPI-010 — Conhecimento vencido
Itens publicados com `review_due_at < agora` / itens publicados.

## KPI-011 — Cobertura de conhecimento
Percentual de famílias recorrentes de incidentes que possuem solução publicada vinculada. Requer definição de cluster/família.

## KPI-012 — Tempo até primeiro resultado útil
Do início da busca até abertura/marcação de item posteriormente classificado como útil.

## KPI-013 — Passos diagnósticos até resolução
Mediana de passos classificáveis por família de sintoma.

## KPI-014 — Falha pós-release
Casos ligados a versão/release em janela definida. Não afirmar causalidade sem evidência.

## KPI-015 — Adoção do RAG
Interações com RAG, fontes abertas, feedback, uso posterior em caso. Não confundir uso com qualidade.

## Governança

Cada KPI terá:
- ID;
- nome;
- fórmula;
- versão;
- proprietário;
- data de vigência;
- filtros válidos;
- exclusões;
- fonte de dados;
- observações de interpretação.

---

## Fórmulas Consolidadas na Fase 10 (M08 — Analytics & Inteligência Gerencial)

### KPI-002-A — Casos Abertos
- **Fórmula:** `COUNT(DISTINCT cases.id)` onde `cases.status IN ('Open', 'Reopened')` no filtro.
- **Cuidados:** O domínio do TraceCore não utiliza o estado 'Investigating'. Abertos incluem ocorrências novas e reabertas.

### KPI-002-B — Casos Resolvidos
- **Fórmula:** `COUNT(DISTINCT cases.id)` onde `cases.status = 'Resolved'` com iteração atual resolvida.

### KPI-002-C — MTTR Operacional por Iteração
- **Fórmula:** `AVG(TIMESTAMPDIFF(MINUTE, case_iterations.opened_at, case_iterations.closed_at))` sobre iterações com `status = 'Resolved'` e `closed_at IS NOT NULL`.
- **Interpretação:** Mede a velocidade de resolução do ciclo ativo. Não acumula o tempo total de casos multi-iteração (evitando distorcer o tempo médio por reabertura pontual).

### KPI-002-D — Mediana de Resolução
- **Fórmula:** Elemento central da distribuição ordenada de durações em minutos das iterações resolvidas.
- **Implementação:** Compatível com MySQL/MariaDB 10.5+ e In-Memory via projeção de durações escalares e cálculo determinístico central.

### KPI-003-A — Incidentes Recorrentes (Critério Determinístico)
- **Fórmula:** Casos que possuem ao menos um vínculo em `case_relations` com `relation_type IN ('Recurrence', 'CommonCause')`.
- **Exclusão:** Vínculos do tipo `Similar` (computados por correspondência textual) **não** caracterizam recorrência formal.

### KPI-010-A — Governança de Conhecimento
- **Nunca Revisado:** Itens de conhecimento publicados onde `last_reviewed_at IS NULL`.
- **Revisão Vencida:** Itens publicados onde `review_due_at < AGORA()`.
- **Sem Solução Documentada:** Casos com `status = 'Resolved'` onde não existe `knowledge_items.provenance_case_id = cases.id`.

