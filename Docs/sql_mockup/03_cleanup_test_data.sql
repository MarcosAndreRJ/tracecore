-- ============================================================
-- TRACECORE - LIMPEZA DA MASSA DE TESTES
-- REMOVER SOMENTE DADOS SINTETICOS (SYN-DEMO-2026)
-- NUNCA executar em producao!
-- ============================================================
-- Ordem de remocao respeitando FKs
-- ============================================================

SET FOREIGN_KEY_CHECKS = 0;

-- 1. Searchable content entries (cases sinteticos)
DELETE FROM searchable_content_entries
WHERE source_type = 'HistoricalCase'
  AND source_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 2. Searchable content entries (knowledge sintetico)
DELETE FROM searchable_content_entries
WHERE source_type = 'ValidatedKnowledge'
  AND source_id IN (
    SELECT ki.id FROM knowledge_items ki
    WHERE ki.knowledge_code LIKE 'KB-%' AND ki.created_at >= '2025-10-01'
  );

-- 3. Knowledge usages
DELETE FROM knowledge_usages
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%')
   OR knowledge_item_id IN (
     SELECT id FROM knowledge_items WHERE knowledge_code LIKE 'KB-%'
     AND created_at >= '2025-10-01'
   );

-- 4. Knowledge tags/technologies
DELETE FROM knowledge_tags WHERE knowledge_item_id IN (
  SELECT id FROM knowledge_items WHERE knowledge_code LIKE 'KB-%' AND created_at >= '2025-10-01');
DELETE FROM knowledge_technologies WHERE knowledge_item_id IN (
  SELECT id FROM knowledge_items WHERE knowledge_code LIKE 'KB-%' AND created_at >= '2025-10-01');

-- 5. Knowledge steps, symptoms, applicability
DELETE FROM knowledge_steps WHERE knowledge_version_id IN (
  SELECT kv.id FROM knowledge_versions kv
  JOIN knowledge_items ki ON kv.knowledge_item_id = ki.id
  WHERE ki.knowledge_code LIKE 'KB-%' AND ki.created_at >= '2025-10-01');
DELETE FROM knowledge_symptoms WHERE knowledge_version_id IN (
  SELECT kv.id FROM knowledge_versions kv
  JOIN knowledge_items ki ON kv.knowledge_item_id = ki.id
  WHERE ki.knowledge_code LIKE 'KB-%' AND ki.created_at >= '2025-10-01');
DELETE FROM knowledge_applicability WHERE knowledge_item_id IN (
  SELECT id FROM knowledge_items WHERE knowledge_code LIKE 'KB-%' AND created_at >= '2025-10-01');

-- 6. Knowledge versions and items
DELETE FROM knowledge_versions WHERE knowledge_item_id IN (
  SELECT id FROM knowledge_items WHERE knowledge_code LIKE 'KB-%' AND created_at >= '2025-10-01');
DELETE FROM knowledge_items WHERE knowledge_code LIKE 'KB-%' AND created_at >= '2025-10-01';

-- 7. Case relations
DELETE FROM case_relations
WHERE source_case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%')
   OR target_case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 8. Case hypothesis evidence
DELETE FROM case_hypothesis_evidence
WHERE evidence_id IN (
  SELECT ce.id FROM case_evidences ce
  JOIN cases c ON ce.case_id = c.id
  WHERE c.external_reference LIKE 'SYN-DEMO-2026%');

-- 9. Case resolutions
DELETE FROM case_resolutions
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 10. Diagnostic steps
DELETE FROM diagnostic_steps
WHERE diagnostic_session_id IN (
  SELECT ds.id FROM diagnostic_sessions ds
  JOIN cases c ON ds.case_id = c.id
  WHERE c.external_reference LIKE 'SYN-DEMO-2026%');

-- 11. Diagnostic sessions
DELETE FROM diagnostic_sessions
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 12. Case evidences
DELETE FROM case_evidences
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 13. Case hypotheses
DELETE FROM case_hypotheses
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 14. Case iterations
DELETE FROM case_iterations
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 15. Case symptoms
DELETE FROM case_symptoms
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 16. Case components
DELETE FROM case_components
WHERE case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%');

-- 17. Cases sinteticos
DELETE FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%';

-- 18. Tags sinteticas
DELETE FROM tags WHERE name IN (
  'auth-lock','password-expired','iam-down','api-502','api-504',
  'dns-fail','deploy-regression','mobile-sync','sap-connector',
  'queue-backlog','permission-denied','file-import'
);

-- 19. Root causes sinteticos
DELETE FROM root_causes WHERE code IN (
  'RC-AUTH-LOCK','RC-PWD-EXPIRED','RC-IAM-DOWN','RC-API-TIMEOUT',
  'RC-DB-POOL-EXH','RC-DB-DEADLOCK','RC-DB-SLOW','RC-DNS-FAIL',
  'RC-CERT-EXPIRED','RC-DEPLOY-REG','RC-MOB-SYNC','RC-SAP-INT',
  'RC-PERM-ERR','RC-FW-BLOCK','RC-QUEUE-BACK'
);

-- 20. Components sinteticos
DELETE FROM components WHERE code IN (
  'AUTH-SERVICE','DB-POOL','DNS-RESOLVER','CERT-MGR',
  'SAP-CONNECTOR','QUEUE-MGR','FW-RULES','FILE-IMP'
);

-- 21. Usuarios sinteticos (nao remover admin!)
DELETE FROM user_roles WHERE user_id IN (
  SELECT id FROM users WHERE email LIKE '%@tracecore.local' AND email != 'admin@tracecore.local');
DELETE FROM user_departments WHERE user_id IN (
  SELECT id FROM users WHERE email LIKE '%@tracecore.local' AND email != 'admin@tracecore.local');
DELETE FROM users WHERE email LIKE '%@tracecore.local' AND email != 'admin@tracecore.local';

-- 22. Client units sinteticos
DELETE FROM client_units WHERE client_id IN (
  SELECT id FROM clients WHERE code LIKE 'SYN-CLI-%');

-- 23. Clientes sinteticos
DELETE FROM clients WHERE code LIKE 'SYN-CLI-%';

-- 24. NOTA: case_number_seq NAO e revertido propositalmente
-- para evitar reabertura de janela de colisao.

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- LIMPEZA CONCLUIDA
-- Dados reais preservados: admin, clientes existentes,
-- catalogo tecnico, permissoes, papeis, departamentos.
-- ============================================================