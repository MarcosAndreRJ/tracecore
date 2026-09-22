# Banco de dados

O schema do TraceCore é governado exclusivamente por **migrations FluentMigrator** em `src/TraceCore.Infrastructure/Migrations` (29 migrations: `M20260917_01` a `M20260922_29`), executadas pelo `DatabaseMigrationRunner`.

Decisões já consolidadas (superam o esqueleto inicial):
1. **IDs**: `BIGINT` auto-incremento padronizado em todas as tabelas e chaves estrangeiras.
2. **DDL canônico**: migrations versionadas substituem o `schema_inicial.sql` histórico; o script original do `sql_mockup/` permanece para carga de dados de exemplo.
3. **Índices**: validados e refletidos nas migrations (status + datas em `cases`, FKs de associação, `error_code`, FULLTEXT em campos definidos, índices de auditoria, outbox por `status,next_attempt_at`).
4. **Charset/collation e backup/restore**: devem ser confirmados junto à infraestrutura antes do primeiro release (fora do escopo das migrations hoje).

Ver também: `10_MODELO_DE_DADOS.md` (mapa conceitual das tabelas) e `99_REFERENCIAS_TECNICAS.md`.
