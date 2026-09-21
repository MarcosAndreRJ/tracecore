-- ============================================================
-- TRACECORE - RESET DA BASE DE DESENVOLVIMENTO (DEV)
-- ============================================================
-- DESTRUTIVO - SOMENTE DESENVOLVIMENTO
-- NAO EXECUTAR EM PRODUCAO / STAGING
--
-- PRESERVA:
--   * VersionInfo (registro de migrations aplicadas)
--   * Admin (admin@tracecore.local) e seus vinculos (user_roles, user_departments)
--   * RBAC: departments, roles, permissions, role_permissions
--
-- ZERA (TRUNCATE, com reset de AUTO_INCREMENT):
--   * Todo o catalogo tecnico (clientes, produtos, versoes, componentes,
--     ambientes, causas raiz, tecnologias, tags, flows, integracoes, LLM)
--   * Todos os dados transacionais (casos, investigacao, diagnostico,
--     knowledge base, search, AI/RAG, auditoria, sessoes, tokens)
-- ============================================================
-- Guarda de ambiente: confirma que conectamos no DEV
-- Executar: dotnet run --no-build -c Release -- "00_reset_development_database.sql"
--   (runner usa connection string de DEV: 192.168.0.5 / TraceCoreDb / root)
-- ============================================================

-- ------------------------------------------------------------
-- 0) GUARDA DE AMBIENTE
-- Verificacao de sanidade: a base de DEV deve ter exatamente
-- 1 usuario (admin@tracecore.local) quando chegamos aqui.
-- Se o resultado for diferente (ex.: varios usuarios reais ou
-- zero admins), PARAR e abortar manualmente antes de prosseguir.
-- ------------------------------------------------------------
SELECT
  DATABASE()                                                          AS database_atual,
  (SELECT COUNT(*) FROM users)                                        AS total_users,
  (SELECT COUNT(*) FROM users WHERE email = 'admin@tracecore.local')  AS admins_encontrados,
  (SELECT COUNT(*) FROM VersionInfo)                                  AS total_migrations_aplicadas,
  (SELECT COUNT(*) FROM roles)                                        AS total_roles,
  (SELECT COUNT(*) FROM permissions)                                  AS total_permissions,
  (SELECT COUNT(*) FROM departments)                                  AS total_departments;

-- ------------------------------------------------------------
-- Limpeza de vinculos que NAO pertencem ao admin (usuarios que
-- porventura existam nao sao preservados neste reset).
-- FK checks desligados desde o inicio para permitir remover
-- usuarios que aparecem como criadores em outras tabelas.
-- ------------------------------------------------------------
SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM user_roles
WHERE user_id NOT IN (SELECT id FROM users WHERE email = 'admin@tracecore.local');

DELETE FROM user_departments
WHERE user_id NOT IN (SELECT id FROM users WHERE email = 'admin@tracecore.local');

-- Remove usuarios nao-admin (sessoes/tokens vinculados sao
-- truncados logo abaixo, com FK checks desligados).
DELETE FROM users
WHERE email <> 'admin@tracecore.local';

-- ------------------------------------------------------------
-- 1) ZERAR DADOS FUNCIONAIS E DE CATALOGO
-- TRUNCATE reseta AUTO_INCREMENT -> numeracao recomeca em 1.
-- FK checks desligados para permitir truncar em qualquer ordem.
-- ------------------------------------------------------------
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE user_sessions;
TRUNCATE TABLE password_reset_tokens;

TRUNCATE TABLE clients;
TRUNCATE TABLE client_units;
TRUNCATE TABLE client_technical_contexts;

TRUNCATE TABLE products;
TRUNCATE TABLE product_versions;
TRUNCATE TABLE product_technical_profiles;
TRUNCATE TABLE product_technologies;
TRUNCATE TABLE product_technical_sources;
TRUNCATE TABLE product_external_research_domains;

TRUNCATE TABLE environments;
TRUNCATE TABLE components;
TRUNCATE TABLE component_dependencies;
TRUNCATE TABLE component_owners;

TRUNCATE TABLE root_causes;
TRUNCATE TABLE technologies;
TRUNCATE TABLE tags;

TRUNCATE TABLE diagnostic_flows;
TRUNCATE TABLE diagnostic_flow_hypotheses;
TRUNCATE TABLE diagnostic_checks;
TRUNCATE TABLE diagnostic_check_options;
TRUNCATE TABLE diagnostic_check_impacts;

TRUNCATE TABLE integrations;
TRUNCATE TABLE integration_runs;

TRUNCATE TABLE llm_providers;
TRUNCATE TABLE llm_model_configs;
TRUNCATE TABLE llm_provider_configs;

TRUNCATE TABLE cases;
TRUNCATE TABLE case_number_seq;
TRUNCATE TABLE case_symptoms;
TRUNCATE TABLE case_components;
TRUNCATE TABLE case_iterations;
TRUNCATE TABLE case_evidences;
TRUNCATE TABLE case_hypotheses;
TRUNCATE TABLE case_hypothesis_evidence;
TRUNCATE TABLE diagnostic_sessions;
TRUNCATE TABLE diagnostic_steps;
TRUNCATE TABLE case_relations;
TRUNCATE TABLE case_resolutions;
TRUNCATE TABLE attachments;

TRUNCATE TABLE knowledge_items;
TRUNCATE TABLE knowledge_versions;
TRUNCATE TABLE knowledge_applicability;
TRUNCATE TABLE knowledge_symptoms;
TRUNCATE TABLE knowledge_steps;
TRUNCATE TABLE knowledge_technologies;
TRUNCATE TABLE knowledge_tags;
TRUNCATE TABLE knowledge_usages;

TRUNCATE TABLE search_sessions;
TRUNCATE TABLE search_queries;
TRUNCATE TABLE search_result_interactions;
TRUNCATE TABLE searchable_content_entries;

TRUNCATE TABLE ai_interactions;
TRUNCATE TABLE ai_sources;
TRUNCATE TABLE ai_interaction_feedback;

TRUNCATE TABLE audit_events;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- RESET CONCLUIDO
-- Preservados: VersionInfo, departments, roles, permissions,
-- role_permissions, users (admin) + vinculos do admin.
-- ============================================================