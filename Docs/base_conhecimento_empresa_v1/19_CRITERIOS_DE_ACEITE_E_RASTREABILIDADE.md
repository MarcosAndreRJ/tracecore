# 19 — Critérios de aceite e rastreabilidade

## 1. Convenção

Cada feature deve apontar para:
- regra de negócio (`BR-*`);
- requisito funcional (`FR-*`);
- critério de aceite (`AC-*`);
- testes automatizados/manuais correspondentes.

## 2. Casos

**AC-001** — Ao criar caso, o texto original informado deve permanecer recuperável mesmo após edição do resumo normalizado.  
Relaciona: BR-020, FR-040, FR-041.

**AC-002** — É possível criar caso sem selecionar departamento responsável.  
Relaciona: BR-022, BR-070.

**AC-003** — Cada passo diagnóstico mostra autor, data, tipo e resultado.  
Relaciona: BR-025, FR-046.

**AC-004** — Uma tentativa marcada como “não funcionou” continua visível na timeline após resolução.  
Relaciona: BR-025, P-003.

**AC-005** — Resolver caso exige ação de resolução e forma de validação.  
Relaciona: BR-027, FR-049.

**AC-006** — Reabrir caso não elimina resolução anterior.  
Relaciona: BR-029, FR-050.

## 3. Conhecimento

**AC-020** — Conteúdo em rascunho não aparece como solução oficial publicada.  
Relaciona: BR-040.

**AC-021** — Publicação gera versão e registra aprovador.  
Relaciona: BR-041, BR-042.

**AC-022** — Solução com passo de alto risco apresenta aviso e rollback quando aplicável.  
Relaciona: BR-045.

**AC-023** — Estatística de sucesso é calculada a partir de usos registrados, não de visualizações.  
Relaciona: BR-047.

**AC-024** — Toda taxa exibida mostra quantidade de amostras.  
Relaciona: BR-048.

## 4. Pesquisa

**AC-040** — Busca por código de erro exato prioriza itens que contêm o código exato.  
Relaciona: FR-081.

**AC-041** — Resultado explica fatores principais de correspondência.  
Relaciona: BR-061, FR-085.

**AC-042** — Item explicitamente incompatível com versão corrente não deve ser recomendado como primeira opção sem alerta.  
Relaciona: BR-062.

**AC-043** — Busca sem resultado é registrada para analytics.  
Relaciona: BR-065.

## 5. Diagnóstico

**AC-060** — O primeiro passo não exige selecionar departamento.  
Relaciona: BR-070.

**AC-061** — Responder a uma pergunta pode alterar a próxima pergunta e a ordem das hipóteses.  
Relaciona: BR-071.

**AC-062** — Recomendação mostra objetivo, risco e hipóteses relacionadas.  
Relaciona: BR-074.

**AC-063** — Ação classificada como destrutiva não é executada automaticamente pelo motor.  
Relaciona: BR-076.

## 6. Analytics

**AC-080** — KPI executivo possui drill-down até casos que compõem o valor, respeitando acesso.  
Relaciona: BR-093.

**AC-081** — Mudança da fórmula de KPI cria versão/registro de definição.  
Relaciona: BR-094.

**AC-082** — Tela de usuário não apresenta score opaco de “melhor/pior funcionário”.  
Relaciona: BR-091.

## 7. Segurança

**AC-100** — Usuário sem permissão não acessa conteúdo via endpoint direto mesmo que conheça o ID.  
Relaciona: BR-102.

**AC-101** — Logs/auditoria não armazenam senha ou token em claro.  
Relaciona: BR-101.

**AC-102** — Mudança de papel/permissão gera evento de auditoria.  
Relaciona: BR-004.

## 8. IA

**AC-120** — Resposta interna de IA mostra fontes utilizadas.  
Relaciona: BR-081, BR-084.

**AC-121** — IA não publica artigo sem revisão humana.  
Relaciona: BR-083.

**AC-122** — Usuário sem acesso a um artigo não recebe trecho dele na resposta RAG.  
Relaciona: BR-102 e segurança de RAG.

**AC-123** — Se não houver fonte suficiente, a IA deve indicar falta de evidência em vez de inventar procedimento.  
Relaciona: BR-085.

## 9. Matriz mínima por PR

Todo PR funcional deve citar pelo menos um BR/FR/AC ou explicar por que é puramente técnico.

