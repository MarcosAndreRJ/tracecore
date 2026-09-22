# 26 — Catálogo inicial de KPIs

## KPI-001 — MTTA
**Pergunta:** quanto tempo levamos para iniciar atuação?  
**Fórmula:** `first_response_at - opened_at`.  
**Segmentar:** severidade, cliente, produto, departamento, origem.  
**Cuidados:** casos importados podem ter timestamp externo diferente.

## KPI-002 — MTTR
**Pergunta:** quanto tempo até resolver?  
**Fórmula base:** `resolved_at - opened_at`.  
**Cuidados:** definir política para períodos `AwaitingInfo` antes de comparar departamentos.

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
Medir duração por estado/departamento para identificar espera versus investigação ativa.

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

---

## Fórmulas Consolidadas na Fase 16 (Inteligência Analítica Determinística — §31)

### KPI-014-A — Tendência e Variação Pós-Versão de Ajuste
- **Pergunta:** Qual foi o impacto da publicação de uma versão de produto no volume e MTTR dos casos?
- **Fórmula:** 
  - Janela de observação simétrica: $I$ dias antes e $I$ dias depois da `product_versions.released_at` (padrão 90 dias).
  - Variação percentual de volume: $\Delta\% = \frac{N_{depois} - N_{antes}}{N_{antes}} \times 100$ (quando $N_{antes} > 0$).
  - Variação de MTTR: $\Delta\%_{MTTR} = \frac{\text{Mediana}_{depois} - \text{Mediana}_{antes}}{\text{Mediana}_{antes}} \times 100$.
- **Rastreabilidade e Grounding (§31):** O indicador retorna obrigatoriamente $N_{antes}$, $N_{depois}$, $N_{total}$ e `HasSufficientData` (requer $N_{total} \ge 3$). Se $N < 3$, a IA declara expressamente que a amostra é insuficiente para uma inferência estatística, sem inventar percentuais.

### KPI-016 — Associação Factual de Componentes a Sintomas e Falhas
- **Pergunta:** Quais componentes do catálogo técnico concentram a maior proporção de ocorrências de determinado erro ou contexto?
- **Fórmula:** $\text{Proporção}(\text{componente}) = \frac{\text{Casos do Componente}}{\text{Total de Casos Filtrados}} \times 100$.
- **Rastreabilidade e Grounding (§31):** Retorna o ranking determinístico consolidado no banco (`case_components`), com contagem absoluta e percentual arredondado em 1 casa decimal. Requer $N \ge 5$ casos para declarar suficiência amostral.

### KPI-005-A — Comparação de Efetividade de Solução (Mediana de MTTR)
- **Pergunta:** A aplicação desta solução da base de conhecimento reduz o tempo de resolução em relação aos casos similares resolvidos sem ela?
- **Fórmula:**
  - $\text{Mediana Com} = \text{Mediana}(\text{Durações de casos com } \text{knowledge\_usages}(\text{item\_id}))$.
  - $\text{Mediana Sem} = \text{Mediana}(\text{Durações de casos no mesmo escopo técnico sem } \text{knowledge\_usages}(\text{item\_id}))$.
  - $\text{Redução\%} = \frac{\text{Mediana Sem} - \text{Mediana Com}}{\text{Mediana Sem}} \times 100$.
- **Rastreabilidade e Grounding (§31):** Retorna obrigatoriamente $N_{com}$, $N_{sem}$ e escopo técnico considerado. Se $N_{com} < 3$ ou $N_{sem} < 3$, o sistema declara status de suficiência amostral falso (`HasSufficientData = false`), e a IA deve reportar "dados insuficientes" em vez de emitir recomendações definitivas.

