# RELATORIO - MASSA REALISTA DE LOGISTICA (TraceCore DEV)

Ambiente: `TraceCoreDb` em MariaDB 10.5.29 (`192.168.0.5:3306`, root/root).
Executor: console runner que divide os scripts por `;` e aborta no primeiro erro.
Scripts: `00_reset_development_database.sql`, `01_populate_realistic_test_data.sql`, `02_validate_realistic_test_data.sql`.

## 1. Arquivos criados
- `00_reset_development_database.sql` (65 statements): guarda de ambiente + limpeza completa preservando admin/RBAC, truncando todas as tabelas funcionais/catalogo/auditoria (FK checks desligados durante os TRUNCATE).
- `01_populate_realistic_test_data.sql` (73 statements, secoes 1-27): dados deterministicos universo logistica 2025-04 a 2026-09.
- `02_validate_realistic_test_data.sql` (76 statements): validacao de volumes, distribuicoes, consistencia e integridade.

## 2. Guarda de ambiente
- `SELECT DATABASE()` confirma `TraceCoreDb`; scripts exigem esse banco (nunca executam em producao).

## 3. Reset preserva corpo de base
- Admin `admin@tracecore.local` preservado; RBAC intacto: 7 roles, 20 permissoes, 8 departamentos; 11 usuarios totais (10 novos + admin) com perfis tecnicos e vinculos.

## 4. Casos e sequencia
- 1.280 casos inseridos (1..1280) + 1 caso E2E = **1.281**; `case_number_seq` max = 1281; 0 duplicidades de `case_number`; seq >= case_number (OK).

## 5. Status (alvo 75/10/5/5/5)
- Resolved 975 (76,2%), Investigating 125 (9,8%), Open 60 (4,7%), Reopened 60 (4,7%), Closed 60 (4,7%). E2E adiciona 1 Open.

## 6. Severidade (alvo 7/28/50/15)
- Critical 91 (7,1%), High 364 (28,4%), Medium 645 (50,4%), Low 180 (14,1%).

## 7. Volume por mes (18 meses)
- 2025-04..2026-09: 40/35/40/60/70/90/55/60/75/95/70/75/85/90/95/115/60/70 (min 35, max 115, total 1.280).

## 8. Familias causais
- 55 faixas determinísticas em `tmp_fam` cobrindo todos os 1.280 casos (~23 casos/familia), unificando relacoes CommonCause/Recurrence/Duplicate/Similar.

## 9. Governanca de casos
- 0 casos sem dono; 0 casos sem departamento.

## 10. Consistencia de datas
- 0 abertos_apos_resolucao; 0 Resolved/Closed/Reopened sem `resolved_at`; 0 Open com `resolved_at`. Capes: opened <= 2026-09-18, resolved <= 2026-09-19 08:00, reopened <= 2026-09-14, closed <= 2026-09-19 22:00.

## 11. Iteracoes e sessões
- Iteracoes 1.340 (1.280 iter.1 + 60 iter.2) -> 1.341 com E2E; 60 Reopened possuem iteracao 2 (0 sem); 0 passos sem sessao.

## 12. Sintomas
- 3.841 registros (3/por caso + extras de iteracao/reabertura), modulo status-real da maquina.

## 13. Hipoteses
- 3.961 (3.841 baseline + 120 da 2a iteracao), statuses Propose/Discarded/Supported coerentes com conclusao.

## 14. Passos de diagnostico
- 5.337 passos em 1.340 sessões (max 14/caso), outcomes distribuidos (Worked/PartiallyWorked/DidNotWork/NotApplicable/Inconclusive).

## 15. Evidencias
- 2.561 evidencias; 4.269 vinculos `case_hypothesis_evidence`; 0 evidencias sem hipotese.

## 16. Resolucoes
- 1.095 (829 causa confirmada / 266 sem confirmacao), todas Definitive, todas com iteracao valida.

## 17. Relacoes entre casos
- 560: 440 CommonCause, 55 Recurrence, 20 Duplicate, 45 Similar; 0 duplicadas (chave unica satisfeita).

## 18. Base de conhecimento
- 200 itens: 160 Published, 20 InReview, 12 Draft, 6 Deprecated, 2 Superseded; 40 de cada tipo (FAQ/Guide/Runbook/InvestigationTemplate/Solution).

## 19. Detalhes de conhecimento
- 200 versoes, 900 passos, 601 sintomas, 400 aplicabilidades, 601 tags, 200 tecnologias, 320 usos; 201 sintomas validados trabalhados no motor de busca.

## 20. Indice de conteudo buscavel
- 1.195 entradas: 160 ValidatedKnowledge + 1.035 HistoricalCase (829 Validated + 206 Draft); 0 duplicadas.

## 21. Busca
- 120 sessoes, 300 consultas, 500 interacoes em resultado (dual source Knowledge/Case, 1-3 por consulta).

## 22. IA / Copiloto
- 120 interacoes, 300 fontes, 40 feedbacks de avaliacao, coerentes com sessoes de diagnostico (motivos abertos/sessoes CLI).

## 23. Auditoria
- 500 eventos (login, abertura, investigacao, seguranca) -> 502 com E2E (login + abertura auditados).

## 24. Catalogo maestro
- 12 clientes, 24 unidades, 6 produtos, 19 versoes, 39 componentes, 14 dependencias (chave unica src/tgt/type), 3 ambientes, 6 perfis tecnicos, 24 produto-tecnologia, 6 fontes tecnicas.

## 25. Integracoes
- 8 integracoes + 18 runs (link de fila falhou/last_read automotivo de 2026-04).

## 26. Fluxos de diagnostico
- 4 fluxos, 12 hipoteses de fluxo, 12 checks, 25 opcoes, 15 impactos.

## 27. Base de apoio
- 30 root causes, 24 tags, 16 tecnologias; usuarios distribuidos por departamentos da rede.

## 28. Validacao E2E na aplicacao
- Login `admin@tracecore.local` (cookie TraceCore.Auth) OK; `POST /api/cases` -> **201** com `caseNumber: 1281` (proximo numero, sem colisao); `NextCaseNumberAsync` confirmado (INSERT auto-increment em `case_number_seq`).

## 29. Correcoes aplicadas durante a execucao
- `clients_r`/`prods` com colunas code/name; `ELT` da busca com indice valido; `ai_sources` com source_id/title; `tmp_fam` removida do 02 (escopo de sessao); reset com `SET FOREIGN_KEY_CHECKS=0` antes do DELETE de usuarios nao-admin (fk_che_user). Sintaxe confirmada para MariaDB 10.5: `INSERT INTO ... WITH ... SELECT` ok; `WITH ... INSERT` nao.

## 30. Reproducao
1. `00_reset_development_database.sql` (65/65 OK).
2. `01_populate_realistic_test_data.sql` (73/73 OK).
3. `02_validate_realistic_test_data.sql` (76/76 OK, 0 inconsistencias).
4. Subir `TraceCore.Web` (Development) e criar ocorrencia: proximo case = 1281.