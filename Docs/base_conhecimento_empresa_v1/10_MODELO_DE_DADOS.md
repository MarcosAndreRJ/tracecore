# 10 — Modelo de dados

## 1. Diretrizes

- MySQL é a fonte de verdade transacional.
- IDs: `BIGINT` padronizado em todas as tabelas e chaves estrangeiras.
- Timestamps em UTC.
- `created_at`, `created_by`, `updated_at`, `updated_by` onde fizer sentido.
- dados flexíveis podem usar JSON, mas dimensões importantes para busca/analytics devem ser normalizadas.
- chaves estrangeiras e índices devem refletir integridade e consultas reais.
- não usar JSON para esconder um modelo que deveria ser relacional.

## 2. Grupos de tabelas

### Identidade
- `users`
- `roles`
- `permissions`
- `user_roles`
- `role_permissions`
- `user_sessions`

### Organização (Fase 7.A: Departamento é conceito oficial único; `teams`/`user_teams` dropados)
- `departments`
- `user_departments`

### Catálogo
- `clients`
- `client_units`
- `client_technical_contexts`
- `products` (com `is_external`)
- `product_modules`
- `product_versions`
- `environments`
- `components`
- `component_dependencies`
- `component_owners`
- `technologies`
- `component_technologies`
- `integrations`

### Casos
- `cases` (com `client_unit_id`)
- `case_iterations`
- `case_symptoms`
- `case_components`
- `case_hypotheses`
- `case_hypothesis_evidence`
- `diagnostic_sessions`
- `diagnostic_steps`
- `case_evidences`
- `case_handoffs`
- `case_relations`
- `root_causes`
- `case_resolutions`

### Conhecimento
- `knowledge_items`
- `knowledge_versions`
- `knowledge_applicability`
- `knowledge_symptoms`
- `knowledge_steps`
- `knowledge_relations`
- `knowledge_reviews`
- `knowledge_comments`
- `knowledge_usages`
- `tags`
- `knowledge_tags`

### Busca/IA
- `search_sessions`
- `search_queries`
- `search_result_interactions`
- `ai_interactions`
- `ai_sources`
- `embedding_index_state` (estado canônico, não necessariamente vetor bruto)

### Auditoria/Plataforma
- `audit_events`
- `outbox_messages`
- `attachments`
- `system_settings`
- `feature_flags`
- `integration_runs`

### Analytics
- `agg_case_daily`
- `agg_solution_usage_daily`
- `agg_search_daily`
- `agg_component_daily`
- `agg_user_activity_daily`

## 3. Entidade `cases`

Campos principais:

```text
id
case_number
external_reference
source_type
client_id
client_unit_id
product_id
product_version_id
environment_id
original_report
normalized_summary
expected_behavior
observed_behavior
error_code
error_message
scope_type
severity
impact_level
status
current_owner_user_id
current_team_id
opened_at
first_response_at
resolved_at
closed_at
root_cause_status
created_at / created_by
updated_at / updated_by
row_version
```

## 4. `diagnostic_steps`

```text
id
diagnostic_session_id
sequence_no
step_type
hypothesis_id nullable
title
objective
instruction
input_evidence_summary
result_summary
outcome
risk_level
duration_seconds
performed_by
performed_at
metadata_json
```

`outcome`: `Worked`, `PartiallyWorked`, `DidNotWork`, `NotApplicable`, `Inconclusive`, `ObservationOnly`.

## 5. `knowledge_items`

```text
id
knowledge_code
type
title
summary
status
confidentiality
owner_user_id
owner_team_id
current_version_id
review_due_at
published_at
deprecated_at
replacement_knowledge_id nullable
created_at / created_by
```

O corpo versionado fica em `knowledge_versions`.

## 6. `knowledge_usages`

Registra aplicação real em um caso:

```text
id
knowledge_item_id
knowledge_version_id
case_id
used_by
used_at
outcome
notes
context_match_json
```

É a base correta para estatística de sucesso.

## 7. `component_dependencies`

```text
id
source_component_id
target_component_id
dependency_type
criticality
description
valid_from
valid_to
```

A direção deve ser inequívoca: `source depende de target`.

## 8. `audit_events`

```text
id
occurred_at
actor_user_id nullable
actor_type
action
entity_type
entity_id
correlation_id
ip_hash_or_address conforme política
user_agent_summary
before_json nullable
after_json nullable
metadata_json
```

Segredos devem ser mascarados antes da gravação.

## 9. `outbox_messages`

```text
id
event_type
aggregate_type
aggregate_id
payload_json
occurred_at
processed_at
attempt_count
next_attempt_at
last_error
status
```

## 10. `case_relations` (Fase 8)

```text
id BIGINT PRIMARY KEY AUTO_INCREMENT
source_case_id BIGINT NOT NULL (FK cases)
target_case_id BIGINT NOT NULL (FK cases)
relation_type VARCHAR(64) NOT NULL (Similar, Duplicate, Recurrence, CommonCause, Dependency, Reference)
similarity_score DOUBLE NULL
matched_factors_json JSON NULL
created_by BIGINT NULL (FK users; NULL para relações automáticas 'Similar')
created_at DATETIME NOT NULL
UNIQUE KEY uq_case_relations_pair_type (source_case_id, target_case_id, relation_type)
```

## 11. Motor de Diagnóstico Guiado (Fase 9 / M07)

```text
diagnostic_flows
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  code VARCHAR(64) NOT NULL UNIQUE
  name VARCHAR(255) NOT NULL
  description TEXT NULL
  initial_keywords_json JSON NOT NULL
  is_active BOOLEAN NOT NULL DEFAULT TRUE
  created_at DATETIME NOT NULL
  updated_at DATETIME NULL

diagnostic_flow_hypotheses
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  flow_id BIGINT NOT NULL (FK diagnostic_flows)
  title VARCHAR(255) NOT NULL
  description TEXT NULL
  component_id BIGINT NULL (FK components)
  prior_weight DECIMAL(5,2) NOT NULL DEFAULT 1.00
  sort_order INT NOT NULL DEFAULT 0

diagnostic_checks
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  flow_id BIGINT NOT NULL (FK diagnostic_flows)
  code VARCHAR(64) NOT NULL
  title VARCHAR(255) NOT NULL
  question TEXT NOT NULL
  cost_score INT NOT NULL DEFAULT 1
  risk_level VARCHAR(32) NOT NULL DEFAULT 'Low'
  discriminative_power DECIMAL(5,2) NOT NULL DEFAULT 1.00
  skip_condition_field VARCHAR(128) NULL
  sort_order INT NOT NULL DEFAULT 0
  created_at DATETIME NOT NULL
  UNIQUE KEY uq_diag_flow_check_code (flow_id, code)

diagnostic_check_options
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  check_id BIGINT NOT NULL (FK diagnostic_checks)
  option_text VARCHAR(255) NOT NULL
  sort_order INT NOT NULL DEFAULT 0

diagnostic_check_impacts
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  option_id BIGINT NOT NULL (FK diagnostic_check_options)
  target_hypothesis_id BIGINT NOT NULL (FK diagnostic_flow_hypotheses)
  impact_type VARCHAR(32) NOT NULL (Favors, Discards, Neutral)
  weight DECIMAL(5,2) NOT NULL DEFAULT 1.00
```

## 12. `searchable_content_entries` (Fase 12 / M11)

```text
id BIGINT PRIMARY KEY AUTO_INCREMENT
source_type VARCHAR(50) NOT NULL (ValidatedKnowledge, HistoricalCase, Document, AiSuggestion)
source_id BIGINT NOT NULL
source_version_id BIGINT NULL
title VARCHAR(500) NOT NULL
normalized_content LONGTEXT NOT NULL
content_hash VARCHAR(64) NOT NULL (SHA-256)
validation_status VARCHAR(50) NOT NULL (Validated, PendingValidation, NotValidated, Rejected)
quality_status VARCHAR(50) NOT NULL (Complete, Incomplete, NeedsReview, Validated, Obsolete)
visibility VARCHAR(50) NOT NULL (Public, Internal, Confidential, Restricted)
client_id BIGINT NULL (FK clients)
product_id BIGINT NULL (FK products)
component_ids_json TEXT NULL
metadata_json LONGTEXT NULL
created_at DATETIME NOT NULL
updated_at DATETIME NOT NULL
source_updated_at DATETIME NOT NULL
indexed_at DATETIME NULL (reservado)
embedding_version VARCHAR(50) NULL (reservado)
INDEX ix_searchable_source (source_type, source_id, source_version_id)
INDEX ix_searchable_hash (content_hash)
INDEX ix_searchable_quality (quality_status)
INDEX ix_searchable_validation (validation_status)
INDEX ix_searchable_updated (updated_at DESC)
```

## 13. Índices mínimos

- status + datas em `cases`;
- cliente/produto/componente por tabelas de associação;
- `FULLTEXT` em campos textuais definidos por benchmark;
- `error_code` índice normal;
- `knowledge_items(status, review_due_at)`;
- `knowledge_usages(knowledge_item_id, outcome, used_at)`;
- `audit_events(entity_type, entity_id, occurred_at)`;
- `audit_events(actor_user_id, occurred_at)`;
- `searchable_content_entries(source_type, source_id, source_version_id)`;
- `searchable_content_entries(content_hash)`;
- `searchable_content_entries(quality_status, validation_status)`;
- outbox por `status,next_attempt_at`;
- relações e FKs nos dois sentidos de consultas frequentes;
- fluxos e checagens por `code` e `flow_id`.

## 14. Retenção

A retenção exata depende de política corporativa. No modelo:
- casos: longo prazo;
- versões de conhecimento: histórico completo;
- auditoria: período definido por segurança/compliance;
- interações de busca/IA: retenção menor e anonimização/agregação quando possível;
- anexos: política por classificação.

