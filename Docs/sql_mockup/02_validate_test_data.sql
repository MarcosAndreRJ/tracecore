-- ============================================================
-- TRACECORE - VALIDACAO DA MASSA DE TESTES
-- Executar apos 01_populate_test_data.sql
-- ============================================================
-- Todas as queries devem retornar ZERO linhas ou valores
-- esperados. Qualquer resultado indica inconsistencia.
-- ============================================================

-- 1. Contagem total de casos sinteticos
SELECT COUNT(*) AS total_sinteticos FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%';
-- Esperado: 500

-- 2. Distribuicao por status
SELECT status, COUNT(*) AS qtd FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
GROUP BY status ORDER BY qtd DESC;
-- Esperado: Resolved ~82%, Open ~13%, Reopened ~5%

-- 3. Distribuicao por severidade
SELECT severity, COUNT(*) AS qtd FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
GROUP BY severity ORDER BY qtd DESC;
-- Esperado: Critical ~7%, High ~28%, Medium ~50%, Low ~15%

-- 4. Distribuicao mensal
SELECT
  DATE_FORMAT(opened_at, '%Y-%m') AS mes,
  COUNT(*) AS qtd
FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%'
GROUP BY mes ORDER BY mes;
-- Esperado: 12 meses, distribuicao nao uniforme

-- 5. Distribuicao por cliente
SELECT c.name, COUNT(*) AS qtd FROM cases ca
JOIN clients c ON ca.client_id = c.id
WHERE ca.external_reference LIKE 'SYN-DEMO-2026%'
GROUP BY c.name ORDER BY qtd DESC;

-- 6. SourceType correto
SELECT source_type, COUNT(*) AS qtd FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
GROUP BY source_type;
-- Esperado: todos 'Synthetic'

-- ============================================================
-- INCONSISTENCIAS - Todas devem retornar ZERO
-- ============================================================

-- 7. Casos resolvidos sem ResolvedAt
SELECT COUNT(*) AS resolved_sem_data
FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND status = 'Resolved' AND resolved_at IS NULL;
-- Esperado: 0

-- 8. Casos com ResolvedAt < OpenedAt
SELECT COUNT(*) AS datas_incoerentes
FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND resolved_at IS NOT NULL AND resolved_at < opened_at;
-- Esperado: 0

-- 9. FirstResponseAt < OpenedAt
SELECT COUNT(*) AS first_response_anterior
FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND first_response_at IS NOT NULL AND first_response_at < opened_at;
-- Esperado: 0

-- 10. Iteracoes sem caso correspondente
SELECT COUNT(*) AS iteracoes_orfas
FROM case_iterations ci
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = ci.case_id
  AND c.external_reference LIKE 'SYN-DEMO-2026%')
  AND ci.reason LIKE '%sintetico%';
-- Esperado: 0

-- 11. Hipoteses sem caso correspondente
SELECT COUNT(*) AS hipoteses_orfas
FROM case_hypotheses ch
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = ch.case_id
  AND c.external_reference LIKE 'SYN-DEMO-2026%');
-- Esperado: 0

-- 12. Sessoes de diagnostico sem caso correspondente
SELECT COUNT(*) AS sessoes_orfas
FROM diagnostic_sessions ds
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = ds.case_id
  AND c.external_reference LIKE 'SYN-DEMO-2026%');
-- Esperado: 0

-- 13. Passos sem sessao correspondente
SELECT COUNT(*) AS passos_orfos
FROM diagnostic_steps dst
WHERE NOT EXISTS (SELECT 1 FROM diagnostic_sessions ds WHERE ds.id = dst.diagnostic_session_id);
-- Esperado: 0

-- 14. Resolucoes sem caso correspondente
SELECT COUNT(*) AS resolucoes_orfas
FROM case_resolutions cr
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = cr.case_id
  AND c.external_reference LIKE 'SYN-DEMO-2026%');
-- Esperado: 0

-- 15. Relacoes com IDs inexistentes
SELECT COUNT(*) AS relacoes_invalidas
FROM case_relations cr
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = cr.source_case_id)
   OR NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = cr.target_case_id);
-- Esperado: 0

-- 16. Relacao autorreferencia
SELECT COUNT(*) AS autorreferencias
FROM case_relations
WHERE source_case_id = target_case_id;
-- Esperado: 0

-- 17. Sincronizacao case_number_seq
SELECT
  (SELECT MAX(case_number) FROM cases) AS max_case_number_em_uso,
  (SELECT MAX(id) FROM case_number_seq) AS proximo_id_da_sequencia;
-- proximo_id_da_sequencia deve ser >= max_case_number_em_uso

-- 18. Status invalidos
SELECT COUNT(*) AS status_invalidos
FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND status NOT IN ('Open', 'Reopened', 'Resolved');
-- Esperado: 0

-- 19. Severidade invalida
SELECT COUNT(*) AS severidade_invalida
FROM cases
WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND severity NOT IN ('Low', 'Medium', 'High', 'Critical');
-- Esperado: 0

-- 20. Hipoteses com status invalido
SELECT COUNT(*) AS hip_status_invalido
FROM case_hypotheses ch
JOIN cases c ON ch.case_id = c.id
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND ch.status NOT IN ('Proposed', 'Discarded', 'Supported');
-- Esperado: 0

-- 21. Evidencias com hypotese apontando para caso inexistente
SELECT COUNT(*) AS che_hip_invalida
FROM case_hypothesis_evidence che
WHERE NOT EXISTS (SELECT 1 FROM case_hypotheses ch WHERE ch.id = che.hypothesis_id)
   OR NOT EXISTS (SELECT 1 FROM case_evidences ce WHERE ce.id = che.evidence_id);
-- Esperado: 0

-- 22. Knowledge usage com case inexistente
SELECT COUNT(*) AS ku_case_invalido
FROM knowledge_usages ku
WHERE NOT EXISTS (SELECT 1 FROM cases c WHERE c.id = ku.case_id);
-- Esperado: 0

-- 23. Knowledge items com versao inexistente
SELECT COUNT(*) AS ki_sem_versao
FROM knowledge_items ki
WHERE NOT EXISTS (SELECT 1 FROM knowledge_versions kv WHERE kv.knowledge_item_id = ki.id)
  AND ki.id > (SELECT MAX(id) FROM knowledge_items) - 80;
-- Esperado: 0

-- 24. Usuarios sinteticos criados
SELECT COUNT(*) AS usuarios_sinteticos
FROM users WHERE email LIKE '%@tracecore.local' AND email != 'admin@tracecore.local';
-- Esperado: 5

-- 25. Clientes sinteticos criados
SELECT COUNT(*) AS clientes_sinteticos FROM clients WHERE code LIKE 'SYN-CLI-%';
-- Esperado: 8