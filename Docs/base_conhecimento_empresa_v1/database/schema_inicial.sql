-- Plataforma Corporativa de Conhecimento - DDL de referência
-- MySQL 8.4 LTS
-- ATENÇÃO: arquivo HISTÓRICO (mockup de design-time, IDs BINARY(16), database knowledge_platform).
-- O schema real é governado pelas migrations FluentMigrator em src/TraceCore.Infrastructure/Migrations
-- (IDs BIGINT, ver database/README_DB.md e 10_MODELO_DE_DADOS.md). Não usar como fonte da verdade.

CREATE DATABASE IF NOT EXISTS knowledge_platform
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;
USE knowledge_platform;

CREATE TABLE departments (
  id BINARY(16) PRIMARY KEY,
  name VARCHAR(160) NOT NULL,
  code VARCHAR(50) NULL,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_departments_name (name)
) ENGINE=InnoDB;

CREATE TABLE users (
  id BINARY(16) PRIMARY KEY,
  login VARCHAR(180) NOT NULL,
  email VARCHAR(254) NOT NULL,
  display_name VARCHAR(200) NOT NULL,
  password_hash VARCHAR(1000) NULL,
  status VARCHAR(30) NOT NULL,
  last_login_at DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_users_login (login),
  UNIQUE KEY uk_users_email (email),
  KEY ix_users_status (status)
) ENGINE=InnoDB;

CREATE TABLE roles (
  id BINARY(16) PRIMARY KEY,
  name VARCHAR(120) NOT NULL,
  description VARCHAR(500) NULL,
  is_system BOOLEAN NOT NULL DEFAULT FALSE,
  UNIQUE KEY uk_roles_name (name)
) ENGINE=InnoDB;

CREATE TABLE permissions (
  id BINARY(16) PRIMARY KEY,
  code VARCHAR(160) NOT NULL,
  description VARCHAR(500) NULL,
  UNIQUE KEY uk_permissions_code (code)
) ENGINE=InnoDB;

CREATE TABLE user_roles (
  id BINARY(16) PRIMARY KEY,
  user_id BINARY(16) NOT NULL,
  role_id BINARY(16) NOT NULL,
  scope_type VARCHAR(30) NOT NULL DEFAULT 'Global',
  scope_id BINARY(16) NULL,
  assigned_at DATETIME(6) NOT NULL,
  assigned_by BINARY(16) NULL,
  KEY ix_user_roles_user_scope (user_id, scope_type, scope_id),
  KEY ix_user_roles_role (role_id),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id),
  CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(id)
) ENGINE=InnoDB;

CREATE TABLE role_permissions (
  role_id BINARY(16) NOT NULL,
  permission_id BINARY(16) NOT NULL,
  PRIMARY KEY (role_id, permission_id),
  CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) REFERENCES roles(id),
  CONSTRAINT fk_role_permissions_permission FOREIGN KEY (permission_id) REFERENCES permissions(id)
) ENGINE=InnoDB;

CREATE TABLE user_departments (
  user_id BINARY(16) NOT NULL,
  department_id BINARY(16) NOT NULL,
  is_primary BOOLEAN NOT NULL DEFAULT FALSE,
  joined_at DATETIME(6) NOT NULL,
  PRIMARY KEY (user_id, department_id),
  CONSTRAINT fk_ud_user FOREIGN KEY (user_id) REFERENCES users(id),
  CONSTRAINT fk_ud_department FOREIGN KEY (department_id) REFERENCES departments(id)
) ENGINE=InnoDB;

CREATE TABLE clients (
  id BINARY(16) PRIMARY KEY,
  code VARCHAR(80) NULL,
  name VARCHAR(200) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'Active',
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_clients_code (code),
  KEY ix_clients_name (name)
) ENGINE=InnoDB;

CREATE TABLE products (
  id BINARY(16) PRIMARY KEY,
  code VARCHAR(80) NULL,
  name VARCHAR(180) NOT NULL,
  description TEXT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'Active',
  UNIQUE KEY uk_products_code (code),
  KEY ix_products_name (name)
) ENGINE=InnoDB;

CREATE TABLE product_versions (
  id BINARY(16) PRIMARY KEY,
  product_id BINARY(16) NOT NULL,
  version_label VARCHAR(80) NOT NULL,
  released_at DATETIME(6) NULL,
  end_of_support_at DATETIME(6) NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'Active',
  UNIQUE KEY uk_product_versions (product_id, version_label),
  CONSTRAINT fk_pv_product FOREIGN KEY (product_id) REFERENCES products(id)
) ENGINE=InnoDB;

CREATE TABLE environments (
  id BINARY(16) PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  environment_type VARCHAR(30) NOT NULL,
  UNIQUE KEY uk_environments_name (name)
) ENGINE=InnoDB;

CREATE TABLE components (
  id BINARY(16) PRIMARY KEY,
  product_id BINARY(16) NULL,
  code VARCHAR(100) NULL,
  name VARCHAR(200) NOT NULL,
  component_type VARCHAR(50) NOT NULL,
  description TEXT NULL,
  owner_department_id BINARY(16) NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'Active',
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_components_product_code (product_id, code),
  KEY ix_components_type (component_type),
  KEY ix_components_name (name),
  CONSTRAINT fk_components_product FOREIGN KEY (product_id) REFERENCES products(id),
  CONSTRAINT fk_components_department FOREIGN KEY (owner_department_id) REFERENCES departments(id)
) ENGINE=InnoDB;

CREATE TABLE component_dependencies (
  id BINARY(16) PRIMARY KEY,
  source_component_id BINARY(16) NOT NULL,
  target_component_id BINARY(16) NOT NULL,
  dependency_type VARCHAR(60) NOT NULL,
  criticality VARCHAR(30) NOT NULL,
  description VARCHAR(1000) NULL,
  valid_from DATETIME(6) NULL,
  valid_to DATETIME(6) NULL,
  UNIQUE KEY uk_component_dependency (source_component_id, target_component_id, dependency_type),
  CONSTRAINT fk_cd_source FOREIGN KEY (source_component_id) REFERENCES components(id),
  CONSTRAINT fk_cd_target FOREIGN KEY (target_component_id) REFERENCES components(id)
) ENGINE=InnoDB;

CREATE TABLE cases (
  id BINARY(16) PRIMARY KEY,
  case_number BIGINT UNSIGNED NOT NULL AUTO_INCREMENT UNIQUE,
  external_reference VARCHAR(160) NULL,
  source_type VARCHAR(50) NOT NULL,
  client_id BINARY(16) NULL,
  product_id BINARY(16) NULL,
  product_version_id BINARY(16) NULL,
  environment_id BINARY(16) NULL,
  original_report MEDIUMTEXT NOT NULL,
  normalized_summary TEXT NULL,
  expected_behavior TEXT NULL,
  observed_behavior TEXT NULL,
  error_code VARCHAR(180) NULL,
  error_message TEXT NULL,
  scope_type VARCHAR(40) NULL,
  severity VARCHAR(20) NOT NULL,
  impact_level VARCHAR(30) NULL,
  status VARCHAR(40) NOT NULL,
  current_owner_user_id BINARY(16) NULL,
  current_department_id BINARY(16) NULL,
  root_cause_status VARCHAR(40) NOT NULL DEFAULT 'NotEvaluated',
  opened_at DATETIME(6) NOT NULL,
  first_response_at DATETIME(6) NULL,
  resolved_at DATETIME(6) NULL,
  closed_at DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL,
  created_by BINARY(16) NULL,
  updated_at DATETIME(6) NOT NULL,
  updated_by BINARY(16) NULL,
  row_version BIGINT UNSIGNED NOT NULL DEFAULT 1,
  KEY ix_cases_status_opened (status, opened_at),
  KEY ix_cases_client_opened (client_id, opened_at),
  KEY ix_cases_product_opened (product_id, opened_at),
  KEY ix_cases_error_code (error_code),
  FULLTEXT KEY ftx_cases_text (original_report, normalized_summary, error_message),
  CONSTRAINT fk_cases_client FOREIGN KEY (client_id) REFERENCES clients(id),
  CONSTRAINT fk_cases_product FOREIGN KEY (product_id) REFERENCES products(id),
  CONSTRAINT fk_cases_version FOREIGN KEY (product_version_id) REFERENCES product_versions(id),
  CONSTRAINT fk_cases_environment FOREIGN KEY (environment_id) REFERENCES environments(id),
  CONSTRAINT fk_cases_owner FOREIGN KEY (current_owner_user_id) REFERENCES users(id),
  CONSTRAINT fk_cases_department FOREIGN KEY (current_department_id) REFERENCES departments(id)
) ENGINE=InnoDB;

CREATE TABLE case_symptoms (
  id BINARY(16) PRIMARY KEY,
  case_id BINARY(16) NOT NULL,
  symptom_code VARCHAR(100) NULL,
  symptom_text VARCHAR(1000) NOT NULL,
  source VARCHAR(30) NOT NULL DEFAULT 'Human',
  confirmed BOOLEAN NOT NULL DEFAULT TRUE,
  KEY ix_case_symptoms_case (case_id),
  KEY ix_case_symptoms_code (symptom_code),
  CONSTRAINT fk_case_symptoms_case FOREIGN KEY (case_id) REFERENCES cases(id)
) ENGINE=InnoDB;

CREATE TABLE case_components (
  case_id BINARY(16) NOT NULL,
  component_id BINARY(16) NOT NULL,
  relation_type VARCHAR(30) NOT NULL,
  confidence_label VARCHAR(30) NULL,
  PRIMARY KEY (case_id, component_id, relation_type),
  CONSTRAINT fk_case_components_case FOREIGN KEY (case_id) REFERENCES cases(id),
  CONSTRAINT fk_case_components_component FOREIGN KEY (component_id) REFERENCES components(id)
) ENGINE=InnoDB;

CREATE TABLE case_hypotheses (
  id BINARY(16) PRIMARY KEY,
  case_id BINARY(16) NOT NULL,
  component_id BINARY(16) NULL,
  title VARCHAR(300) NOT NULL,
  description TEXT NULL,
  status VARCHAR(30) NOT NULL,
  source_type VARCHAR(30) NOT NULL,
  justification TEXT NULL,
  created_at DATETIME(6) NOT NULL,
  created_by BINARY(16) NULL,
  updated_at DATETIME(6) NOT NULL,
  KEY ix_case_hypotheses_case_status (case_id, status),
  CONSTRAINT fk_hyp_case FOREIGN KEY (case_id) REFERENCES cases(id),
  CONSTRAINT fk_hyp_component FOREIGN KEY (component_id) REFERENCES components(id)
) ENGINE=InnoDB;

CREATE TABLE diagnostic_sessions (
  id BINARY(16) PRIMARY KEY,
  case_id BINARY(16) NOT NULL,
  status VARCHAR(30) NOT NULL,
  started_at DATETIME(6) NOT NULL,
  started_by BINARY(16) NOT NULL,
  ended_at DATETIME(6) NULL,
  KEY ix_diag_sessions_case (case_id, started_at),
  CONSTRAINT fk_ds_case FOREIGN KEY (case_id) REFERENCES cases(id),
  CONSTRAINT fk_ds_user FOREIGN KEY (started_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE diagnostic_steps (
  id BINARY(16) PRIMARY KEY,
  diagnostic_session_id BINARY(16) NOT NULL,
  sequence_no INT NOT NULL,
  step_type VARCHAR(40) NOT NULL,
  hypothesis_id BINARY(16) NULL,
  title VARCHAR(300) NOT NULL,
  objective TEXT NULL,
  instruction MEDIUMTEXT NULL,
  input_evidence_summary TEXT NULL,
  result_summary MEDIUMTEXT NULL,
  outcome VARCHAR(40) NOT NULL,
  risk_level VARCHAR(30) NOT NULL DEFAULT 'Low',
  duration_seconds INT NULL,
  performed_by BINARY(16) NULL,
  performed_at DATETIME(6) NOT NULL,
  metadata_json JSON NULL,
  UNIQUE KEY uk_diag_step_sequence (diagnostic_session_id, sequence_no),
  KEY ix_diag_steps_hypothesis (hypothesis_id),
  CONSTRAINT fk_dstep_session FOREIGN KEY (diagnostic_session_id) REFERENCES diagnostic_sessions(id),
  CONSTRAINT fk_dstep_hyp FOREIGN KEY (hypothesis_id) REFERENCES case_hypotheses(id),
  CONSTRAINT fk_dstep_user FOREIGN KEY (performed_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE root_causes (
  id BINARY(16) PRIMARY KEY,
  code VARCHAR(100) NULL,
  name VARCHAR(250) NOT NULL,
  category VARCHAR(100) NULL,
  description TEXT NULL,
  UNIQUE KEY uk_root_causes_code (code)
) ENGINE=InnoDB;

CREATE TABLE case_resolutions (
  id BINARY(16) PRIMARY KEY,
  case_id BINARY(16) NOT NULL,
  resolution_summary MEDIUMTEXT NOT NULL,
  validation_summary MEDIUMTEXT NOT NULL,
  root_cause_id BINARY(16) NULL,
  root_cause_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
  resolved_by BINARY(16) NOT NULL,
  resolved_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_case_resolution_case (case_id),
  CONSTRAINT fk_cr_case FOREIGN KEY (case_id) REFERENCES cases(id),
  CONSTRAINT fk_cr_root FOREIGN KEY (root_cause_id) REFERENCES root_causes(id),
  CONSTRAINT fk_cr_user FOREIGN KEY (resolved_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE knowledge_items (
  id BINARY(16) PRIMARY KEY,
  knowledge_code VARCHAR(80) NOT NULL,
  knowledge_type VARCHAR(50) NOT NULL,
  title VARCHAR(400) NOT NULL,
  summary TEXT NULL,
  status VARCHAR(30) NOT NULL,
  confidentiality VARCHAR(30) NOT NULL DEFAULT 'Internal',
  owner_user_id BINARY(16) NULL,
  owner_department_id BINARY(16) NULL,
  current_version_no INT NOT NULL DEFAULT 0,
  review_due_at DATETIME(6) NULL,
  published_at DATETIME(6) NULL,
  deprecated_at DATETIME(6) NULL,
  replacement_knowledge_id BINARY(16) NULL,
  created_at DATETIME(6) NOT NULL,
  created_by BINARY(16) NULL,
  updated_at DATETIME(6) NOT NULL,
  UNIQUE KEY uk_knowledge_code (knowledge_code),
  KEY ix_knowledge_status_review (status, review_due_at),
  FULLTEXT KEY ftx_knowledge_title_summary (title, summary),
  CONSTRAINT fk_ki_owner FOREIGN KEY (owner_user_id) REFERENCES users(id),
  CONSTRAINT fk_ki_department FOREIGN KEY (owner_department_id) REFERENCES departments(id),
  CONSTRAINT fk_ki_replacement FOREIGN KEY (replacement_knowledge_id) REFERENCES knowledge_items(id)
) ENGINE=InnoDB;

CREATE TABLE knowledge_versions (
  id BINARY(16) PRIMARY KEY,
  knowledge_item_id BINARY(16) NOT NULL,
  version_no INT NOT NULL,
  content_markdown LONGTEXT NOT NULL,
  change_summary VARCHAR(1000) NULL,
  status VARCHAR(30) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  created_by BINARY(16) NOT NULL,
  approved_at DATETIME(6) NULL,
  approved_by BINARY(16) NULL,
  content_hash CHAR(64) NOT NULL,
  UNIQUE KEY uk_knowledge_version (knowledge_item_id, version_no),
  FULLTEXT KEY ftx_knowledge_content (content_markdown),
  CONSTRAINT fk_kv_item FOREIGN KEY (knowledge_item_id) REFERENCES knowledge_items(id),
  CONSTRAINT fk_kv_created_by FOREIGN KEY (created_by) REFERENCES users(id),
  CONSTRAINT fk_kv_approved_by FOREIGN KEY (approved_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE knowledge_applicability (
  id BINARY(16) PRIMARY KEY,
  knowledge_item_id BINARY(16) NOT NULL,
  product_id BINARY(16) NULL,
  product_version_id BINARY(16) NULL,
  component_id BINARY(16) NULL,
  environment_id BINARY(16) NULL,
  applicability_type VARCHAR(30) NOT NULL DEFAULT 'Applies',
  notes VARCHAR(1000) NULL,
  KEY ix_ka_item (knowledge_item_id),
  KEY ix_ka_context (product_id, product_version_id, component_id, environment_id),
  CONSTRAINT fk_ka_item FOREIGN KEY (knowledge_item_id) REFERENCES knowledge_items(id),
  CONSTRAINT fk_ka_product FOREIGN KEY (product_id) REFERENCES products(id),
  CONSTRAINT fk_ka_version FOREIGN KEY (product_version_id) REFERENCES product_versions(id),
  CONSTRAINT fk_ka_component FOREIGN KEY (component_id) REFERENCES components(id),
  CONSTRAINT fk_ka_environment FOREIGN KEY (environment_id) REFERENCES environments(id)
) ENGINE=InnoDB;

CREATE TABLE knowledge_usages (
  id BINARY(16) PRIMARY KEY,
  knowledge_item_id BINARY(16) NOT NULL,
  knowledge_version_id BINARY(16) NOT NULL,
  case_id BINARY(16) NOT NULL,
  used_by BINARY(16) NOT NULL,
  used_at DATETIME(6) NOT NULL,
  outcome VARCHAR(40) NOT NULL,
  notes TEXT NULL,
  context_match_json JSON NULL,
  KEY ix_ku_item_outcome (knowledge_item_id, outcome, used_at),
  KEY ix_ku_case (case_id),
  CONSTRAINT fk_ku_item FOREIGN KEY (knowledge_item_id) REFERENCES knowledge_items(id),
  CONSTRAINT fk_ku_version FOREIGN KEY (knowledge_version_id) REFERENCES knowledge_versions(id),
  CONSTRAINT fk_ku_case FOREIGN KEY (case_id) REFERENCES cases(id),
  CONSTRAINT fk_ku_user FOREIGN KEY (used_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE case_relations (
  id BINARY(16) PRIMARY KEY,
  source_case_id BINARY(16) NOT NULL,
  target_case_id BINARY(16) NOT NULL,
  relation_type VARCHAR(40) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  created_by BINARY(16) NOT NULL,
  UNIQUE KEY uk_case_relation (source_case_id, target_case_id, relation_type),
  CONSTRAINT fk_crel_source FOREIGN KEY (source_case_id) REFERENCES cases(id),
  CONSTRAINT fk_crel_target FOREIGN KEY (target_case_id) REFERENCES cases(id),
  CONSTRAINT fk_crel_user FOREIGN KEY (created_by) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE search_sessions (
  id BINARY(16) PRIMARY KEY,
  user_id BINARY(16) NULL,
  started_at DATETIME(6) NOT NULL,
  context_json JSON NULL,
  CONSTRAINT fk_search_session_user FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE search_queries (
  id BINARY(16) PRIMARY KEY,
  search_session_id BINARY(16) NOT NULL,
  query_text TEXT NOT NULL,
  filters_json JSON NULL,
  result_count INT NOT NULL,
  duration_ms INT NULL,
  executed_at DATETIME(6) NOT NULL,
  KEY ix_search_queries_time (executed_at),
  CONSTRAINT fk_sq_session FOREIGN KEY (search_session_id) REFERENCES search_sessions(id)
) ENGINE=InnoDB;

CREATE TABLE audit_events (
  id BINARY(16) PRIMARY KEY,
  occurred_at DATETIME(6) NOT NULL,
  actor_user_id BINARY(16) NULL,
  actor_type VARCHAR(30) NOT NULL,
  action VARCHAR(120) NOT NULL,
  entity_type VARCHAR(120) NOT NULL,
  entity_id BINARY(16) NULL,
  correlation_id VARCHAR(100) NULL,
  source_ip VARCHAR(64) NULL,
  before_json JSON NULL,
  after_json JSON NULL,
  metadata_json JSON NULL,
  KEY ix_audit_entity (entity_type, entity_id, occurred_at),
  KEY ix_audit_actor (actor_user_id, occurred_at),
  KEY ix_audit_action_time (action, occurred_at),
  CONSTRAINT fk_audit_user FOREIGN KEY (actor_user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE outbox_messages (
  id BINARY(16) PRIMARY KEY,
  event_type VARCHAR(200) NOT NULL,
  aggregate_type VARCHAR(120) NULL,
  aggregate_id BINARY(16) NULL,
  payload_json JSON NOT NULL,
  occurred_at DATETIME(6) NOT NULL,
  processed_at DATETIME(6) NULL,
  attempt_count INT NOT NULL DEFAULT 0,
  next_attempt_at DATETIME(6) NULL,
  last_error TEXT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'Pending',
  KEY ix_outbox_pending (status, next_attempt_at, occurred_at)
) ENGINE=InnoDB;

CREATE TABLE attachments (
  id BINARY(16) PRIMARY KEY,
  entity_type VARCHAR(120) NOT NULL,
  entity_id BINARY(16) NOT NULL,
  file_name VARCHAR(500) NOT NULL,
  mime_type VARCHAR(200) NOT NULL,
  size_bytes BIGINT UNSIGNED NOT NULL,
  sha256 CHAR(64) NOT NULL,
  storage_key VARCHAR(1000) NOT NULL,
  confidentiality VARCHAR(30) NOT NULL DEFAULT 'Internal',
  uploaded_by BINARY(16) NOT NULL,
  uploaded_at DATETIME(6) NOT NULL,
  KEY ix_attachments_entity (entity_type, entity_id),
  CONSTRAINT fk_attachment_user FOREIGN KEY (uploaded_by) REFERENCES users(id)
) ENGINE=InnoDB;
