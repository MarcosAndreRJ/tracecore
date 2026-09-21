-- ============================================================
-- TRACECORE - VALIDACAO DA MASSA REALISTA (UNIVERSSO LOGISTICA)
-- SOMENTE DESENVOLVIMENTO / NAO EXECUTAR EM PRODUCAO
-- ============================================================

SELECT '=== 01. GUARDA DE AMBIENTE ===' AS bloco;
SELECT DATABASE() AS database_atual;

SELECT '=== 02. CASOS E SEQUENCIA ===' AS bloco;
SELECT COUNT(*) AS total_cases FROM cases;
SELECT MAX(id) AS max_seq,
       (SELECT MAX(case_number) FROM cases) AS max_case_number,
       (SELECT COUNT(*) FROM case_number_seq) AS total_seq,
       (SELECT COUNT(*) FROM cases c WHERE EXISTS (SELECT 1 FROM cases c2 WHERE c2.case_number = c.case_number AND c2.id <> c.id)) AS duplicados_case_number
FROM case_number_seq;
SELECT IF((SELECT MAX(id) FROM case_number_seq) >= (SELECT MAX(case_number) FROM cases), 'OK', 'ERRO') AS seq_maior_igual;

SELECT '=== 03. STATUS DOS CASOS (alvo: 75/10/5/5/5) ===' AS bloco;
SELECT status, COUNT(*) AS total FROM cases GROUP BY status ORDER BY status;

SELECT '=== 04. SEVERIDADE (alvo: 7/28/50/15) ===' AS bloco;
SELECT severity, COUNT(*) AS total FROM cases GROUP BY severity ORDER BY severity;

SELECT '=== 05. VOLUME POR MES (18 meses) ===' AS bloco;
SELECT DATE_FORMAT(opened_at, '%Y-%m') AS mes, COUNT(*) AS total
FROM cases GROUP BY DATE_FORMAT(opened_at, '%Y-%m') ORDER BY mes;

SELECT '=== 07. ITERACOES E SESSOES ===' AS bloco;
SELECT (SELECT COUNT(*) FROM case_iterations) AS iteracoes,
       (SELECT COUNT(*) FROM diagnostic_sessions) AS sessoes,
       (SELECT COUNT(*) FROM diagnostic_sessions WHERE status = 'Open') AS sessoes_abertas,
       (SELECT COUNT(*) FROM case_iterations WHERE sequence_number = 2) AS iteracoes_reabertas;

SELECT '=== 08. SINTOMAS / HIPOTESES / PASSOS ===' AS bloco;
SELECT (SELECT COUNT(*) FROM case_symptoms) AS sintomas,
       (SELECT COUNT(*) FROM case_hypotheses) AS hipoteses,
       (SELECT COUNT(*) FROM diagnostic_steps) AS passos;

SELECT '=== 09. EVIDENCIAS E VINCULOS ===' AS bloco;
SELECT (SELECT COUNT(*) FROM case_evidences) AS evidencias,
       (SELECT COUNT(*) FROM case_hypothesis_evidence) AS vinculos_hip_evid;

SELECT '=== 10. RESOLUCOES ===' AS bloco;
SELECT COUNT(*) AS total_resolucoes FROM case_resolutions;
SELECT resolution_type, root_cause_confirmed, COUNT(*) AS total FROM case_resolutions GROUP BY resolution_type, root_cause_confirmed;

SELECT '=== 11. RELACOES ENTRE CASOS ===' AS bloco;
SELECT relation_type, COUNT(*) AS total FROM case_relations GROUP BY relation_type ORDER BY relation_type;
SELECT COUNT(*) AS total_relacoes FROM case_relations;
SELECT COUNT(*) AS duplicadas FROM (
  SELECT source_case_id, target_case_id, relation_type FROM case_relations GROUP BY source_case_id, target_case_id, relation_type HAVING COUNT(*) > 1
) x;

SELECT '=== 12. CONHECIMENTO ===' AS bloco;
SELECT status, COUNT(*) AS total FROM knowledge_items GROUP BY status ORDER BY status;
SELECT knowledge_type, COUNT(*) AS total FROM knowledge_items GROUP BY knowledge_type ORDER BY knowledge_type;
SELECT (SELECT COUNT(*) FROM knowledge_versions) AS versoes,
       (SELECT COUNT(*) FROM knowledge_steps) AS passos,
       (SELECT COUNT(*) FROM knowledge_symptoms) AS sintomas,
       (SELECT COUNT(*) FROM knowledge_applicability) AS aplicabilidades,
       (SELECT COUNT(*) FROM knowledge_tags) AS tags,
       (SELECT COUNT(*) FROM knowledge_technologies) AS tecnologias,
       (SELECT COUNT(*) FROM knowledge_usages) AS usos;

SELECT '=== 13. INDICE CONTEUDO BUSCAVEL ===' AS bloco;
SELECT source_type, validation_status, COUNT(*) AS total FROM searchable_content_entries GROUP BY source_type, validation_status ORDER BY source_type;
SELECT COUNT(*) AS total_entradas FROM searchable_content_entries;
SELECT COUNT(*) AS duplicadas_entradas FROM (
  SELECT source_type, source_id, COALESCE(source_version_id, 0) FROM searchable_content_entries GROUP BY source_type, source_id, COALESCE(source_version_id, 0) HAVING COUNT(*) > 1
) x;

SELECT '=== 14. BUSCA ===' AS bloco;
SELECT (SELECT COUNT(*) FROM search_sessions) AS sessoes,
       (SELECT COUNT(*) FROM search_queries) AS consultas,
       (SELECT COUNT(*) FROM search_result_interactions) AS interacoes;

SELECT '=== 15. IA ===' AS bloco;
SELECT (SELECT COUNT(*) FROM ai_interactions) AS interacoes_ia,
       (SELECT COUNT(*) FROM ai_sources) AS fontes,
       (SELECT COUNT(*) FROM ai_interaction_feedback) AS feedbacks;

SELECT '=== 16. AUDITORIA ===' AS bloco;
SELECT COUNT(*) AS eventos FROM audit_events;

SELECT '=== 17. CLIENTES / PRODUTOS / AMBIENTES ===' AS bloco;
SELECT (SELECT COUNT(*) FROM clients) AS clientes,
       (SELECT COUNT(*) FROM client_units) AS unidades,
       (SELECT COUNT(*) FROM products) AS produtos,
       (SELECT COUNT(*) FROM product_versions) AS versoes_produtos,
       (SELECT COUNT(*) FROM components) AS componentes,
       (SELECT COUNT(*) FROM component_dependencies) AS dependencias,
       (SELECT COUNT(*) FROM environments) AS ambientes;

SELECT '=== 18. CATALOGO BASE (RBAC) ===' AS bloco;
SELECT (SELECT COUNT(*) FROM users) AS usuarios,
       (SELECT COUNT(*) FROM users WHERE email = 'admin@tracecore.local') AS admin_preservado,
       (SELECT COUNT(*) FROM roles) AS roles,
       (SELECT COUNT(*) FROM permissions) AS permissoes,
       (SELECT COUNT(*) FROM departments) AS departamentos;

SELECT '=== 19. INTEGRACOES ===' AS bloco;
SELECT (SELECT COUNT(*) FROM integrations) AS integracoes,
       (SELECT COUNT(*) FROM integration_runs) AS runs;

SELECT '=== 20. FLUXOS DE DIAGNOSTICO ===' AS bloco;
SELECT (SELECT COUNT(*) FROM diagnostic_flows) AS fluxos,
       (SELECT COUNT(*) FROM diagnostic_flow_hypotheses) AS hipoteses_fluxos,
       (SELECT COUNT(*) FROM diagnostic_checks) AS checks,
       (SELECT COUNT(*) FROM diagnostic_check_options) AS opcoes,
       (SELECT COUNT(*) FROM diagnostic_check_impacts) AS impactos;

SELECT '=== 21. ROOT CAUSES / TAGS / TECNOLOGIAS ===' AS bloco;
SELECT (SELECT COUNT(*) FROM root_causes) AS root_causes,
       (SELECT COUNT(*) FROM tags) AS tags,
       (SELECT COUNT(*) FROM technologies) AS tecnologias;

SELECT '=== 22. CASOS SEM DONO / SEM DEPARTAMENTO ===' AS bloco;
SELECT (SELECT COUNT(*) FROM cases WHERE current_owner_user_id IS NULL) AS sem_dono,
       (SELECT COUNT(*) FROM cases WHERE current_department_id IS NULL) AS sem_departamento;

SELECT '=== 23. CONSISTENCIA DE DATAS (esperado 0) ===' AS bloco;
SELECT COUNT(*) AS abertos_apos_resolucao FROM cases WHERE opened_at > resolved_at;
SELECT COUNT(*) AS resolved_sem_data FROM cases WHERE status IN ('Resolved','Closed','Reopened') AND resolved_at IS NULL;
SELECT COUNT(*) AS aberto_com_data_resolved FROM cases WHERE status = 'Open' AND resolved_at IS NOT NULL;

SELECT '=== 24. CONSISTENCIA DE VINCULOS (esperado 0) ===' AS bloco;
SELECT COUNT(*) AS evid_sem_hipotese FROM case_evidences ce WHERE NOT EXISTS (SELECT 1 FROM case_hypothesis_evidence che WHERE che.evidence_id = ce.id);
SELECT COUNT(*) AS resolucao_sem_iteracao FROM case_resolutions r WHERE NOT EXISTS (SELECT 1 FROM case_iterations ci WHERE ci.id = r.case_iteration_id);
SELECT COUNT(*) AS passos_sem_sessao FROM diagnostic_steps ds WHERE NOT EXISTS (SELECT 1 FROM diagnostic_sessions dss WHERE dss.id = ds.diagnostic_session_id);
SELECT COUNT(*) AS uso_sem_versao FROM knowledge_usages ku WHERE NOT EXISTS (SELECT 1 FROM knowledge_versions kv WHERE kv.id = ku.knowledge_version_id);

SELECT '=== 25. DISTRIBUICAO STATUS X SEVERIDADE ===' AS bloco;
SELECT status, severity, COUNT(*) AS total FROM cases GROUP BY status, severity ORDER BY status, severity;

SELECT '=== 26. IMPACTO ===' AS bloco;
SELECT impact_level, COUNT(*) AS total FROM cases GROUP BY impact_level ORDER BY impact_level;
SELECT scope_type, COUNT(*) AS total FROM cases GROUP BY scope_type ORDER BY scope_type;

SELECT '=== 27. CONHECIMENTO PUBLICADO INDEXADO ===' AS bloco;
SELECT COUNT(*) AS knowledge_publicado FROM knowledge_items WHERE status = 'Published';
SELECT COUNT(*) AS publicado_indexado FROM searchable_content_entries WHERE source_type = 'ValidatedKnowledge';
SELECT COUNT(*) AS cases_resolvidos_indexados FROM searchable_content_entries WHERE source_type = 'HistoricalCase';

SELECT '=== 28. REABERTOS COM NOVA ITERACAO (esperado 64) ===' AS bloco;
SELECT COUNT(*) AS reabertos FROM cases WHERE status = 'Reopened';
SELECT COUNT(*) AS reabertos_sem_iteracao2 FROM cases c WHERE c.status = 'Reopened' AND NOT EXISTS (SELECT 1 FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 2);

SELECT '=== 29. AMOSTRAS ===' AS bloco;
SELECT case_number, external_reference, client_id, product_id, severity, status, root_cause_status, opened_at, resolved_at
FROM cases ORDER BY case_number LIMIT 3;
SELECT case_number, external_reference, client_id, product_id, severity, status, root_cause_status, opened_at, resolved_at
FROM cases ORDER BY case_number DESC LIMIT 2;

SELECT '=== 30. FIM DA VALIDACAO ===' AS bloco;