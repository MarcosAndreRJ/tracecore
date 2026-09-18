using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091809, "Add Super Admin (Dev) role and create search schema: sessions, queries and interactions (Phase 7 - M06)")]
public class M20260918_09_AddSuperAdminRoleAndSearchSchema : Migration
{
    public override void Up()
    {
        // =========================================================================
        // PARTE 1 (Seção 0.1): Papel Super Admin (Dev) com todas as permissões
        // =========================================================================
        Execute.Sql(@"
            -- 1. Cria o papel Super Admin (Dev) se não existir
            -- (Bloco 7.A.0: roles só tem id/name/description — created_at/updated_at
            -- não existem nessa tabela; bug só detectável validando contra MySQL/MariaDB real,
            -- nunca contra o InMemory, que não impõe schema.)
            INSERT INTO roles (name, description)
            SELECT 'Super Admin (Dev)', 'Papel excepcional e completo de desenvolvimento com acesso irrestrito a todos os módulos e permissões da plataforma.'
            WHERE NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Super Admin (Dev)');

            -- 2. Concede DINAMICAMENTE todas as permissões cadastradas para o papel
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.name = 'Super Admin (Dev)'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );

            -- 3. Vincula admin@tracecore.local ao papel Super Admin (Dev) além dos já existentes
            INSERT INTO user_roles (user_id, role_id)
            SELECT u.id, r.id
            FROM users u
            CROSS JOIN roles r
            WHERE u.email = 'admin@tracecore.local'
              AND r.name = 'Super Admin (Dev)'
              AND NOT EXISTS (
                  SELECT 1 FROM user_roles ur
                  WHERE ur.user_id = u.id AND ur.role_id = r.id
              );
        ");

        // =========================================================================
        // PARTE 2 (M06): Tabelas de Pesquisa e Similaridade
        // =========================================================================

        // 1. search_sessions
        Create.Table("search_sessions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt64().Nullable().ForeignKey("fk_search_session_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("started_at").AsDateTime().NotNullable()
            .WithColumn("context_json").AsString(int.MaxValue).Nullable();

        Create.Index("ix_search_sessions_user_date")
            .OnTable("search_sessions")
            .OnColumn("user_id").Ascending()
            .OnColumn("started_at").Descending();

        // 2. search_queries
        Create.Table("search_queries")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("search_session_id").AsInt64().NotNullable().ForeignKey("fk_search_query_session", "search_sessions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("query_text").AsString(500).NotNullable()
            .WithColumn("filters_json").AsString(int.MaxValue).Nullable()
            .WithColumn("result_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("duration_ms").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("executed_at").AsDateTime().NotNullable();

        Create.Index("ix_search_queries_executed_at")
            .OnTable("search_queries")
            .OnColumn("executed_at").Descending();

        Create.Index("ix_search_queries_session")
            .OnTable("search_queries")
            .OnColumn("search_session_id").Ascending();

        // 3. search_result_interactions (BR-064)
        Create.Table("search_result_interactions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("search_query_id").AsInt64().NotNullable().ForeignKey("fk_search_interaction_query", "search_queries", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("result_type").AsString(50).NotNullable()
            .WithColumn("result_id").AsInt64().NotNullable()
            .WithColumn("position").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("opened_at").AsDateTime().Nullable()
            .WithColumn("feedback_useful").AsBoolean().Nullable();

        Create.Index("ix_search_result_interactions_query_id")
            .OnTable("search_result_interactions")
            .OnColumn("search_query_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("search_result_interactions");
        Delete.Table("search_queries");
        Delete.Table("search_sessions");

        Execute.Sql(@"
            DELETE ur FROM user_roles ur
            INNER JOIN roles r ON r.id = ur.role_id
            WHERE r.name = 'Super Admin (Dev)';

            DELETE rp FROM role_permissions rp
            INNER JOIN roles r ON r.id = rp.role_id
            WHERE r.name = 'Super Admin (Dev)';

            DELETE FROM roles WHERE name = 'Super Admin (Dev)';
        ");
    }
}
