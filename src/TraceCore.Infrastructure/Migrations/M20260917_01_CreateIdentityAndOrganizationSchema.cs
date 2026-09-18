using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

// TODO: ADR-P003 - Migração utilizando FluentMigrator como default da fase (ADR-P003 FluentMigrator vs DbUp permanece em aberto).
[Migration(2026091701, "Create initial Identity, Organization and Audit schema")]
public class M20260917_01_CreateIdentityAndOrganizationSchema : Migration
{
    public override void Up()
    {
        // 1. users
        Create.Table("users")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("email").AsString(255).NotNullable().Unique("uq_users_email")
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("last_login_at").AsDateTime().Nullable()
            // TODO: ADR-P004 - Storage de anexos e avatares pendente de decisão arquitetural
            .WithColumn("avatar_storage_key").AsString(255).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsInt64().Nullable()
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("updated_by").AsInt64().Nullable()
            .WithColumn("row_version").AsInt64().NotNullable().WithDefaultValue(1);

        // 2. user_sessions
        Create.Table("user_sessions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("fk_user_sessions_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("revoked_at").AsDateTime().Nullable()
            .WithColumn("ip_address").AsString(45).Nullable()
            .WithColumn("user_agent").AsString(500).Nullable();

        Create.Index("ix_user_sessions_user_id").OnTable("user_sessions").OnColumn("user_id");

        // 3. password_reset_tokens
        // Extensão documentada necessária para o bloco 1.1 (Recuperação de Senha) conforme BR-101
        Create.Table("password_reset_tokens")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("fk_password_reset_tokens_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("token_hash").AsString(128).NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("used_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_password_reset_tokens_hash").OnTable("password_reset_tokens").OnColumn("token_hash");

        // 4. departments
        Create.Table("departments")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(150).NotNullable().Unique("uq_departments_name")
            .WithColumn("description").AsString(500).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Active");

        // 5. teams (M02: times podem ser transversais, department_id nullable)
        Create.Table("teams")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("department_id").AsInt64().Nullable().ForeignKey("fk_teams_department", "departments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("name").AsString(150).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Active");

        // 6. user_departments (BR-002: vínculo N:N)
        Create.Table("user_departments")
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("fk_user_departments_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("department_id").AsInt64().NotNullable().ForeignKey("fk_user_departments_department", "departments", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("pk_user_departments").OnTable("user_departments").Columns("user_id", "department_id");

        // 7. user_teams
        Create.Table("user_teams")
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("fk_user_teams_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("team_id").AsInt64().NotNullable().ForeignKey("fk_user_teams_team", "teams", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("pk_user_teams").OnTable("user_teams").Columns("user_id", "team_id");

        // 8. roles
        Create.Table("roles")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique("uq_roles_name")
            .WithColumn("description").AsString(500).NotNullable();

        // 9. permissions (Convenção: português, 'dominio.acao' em lowercase)
        Create.Table("permissions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("code").AsString(100).NotNullable().Unique("uq_permissions_code")
            .WithColumn("description").AsString(500).NotNullable();

        // 10. role_permissions
        Create.Table("role_permissions")
            .WithColumn("role_id").AsInt64().NotNullable().ForeignKey("fk_role_permissions_role", "roles", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("permission_id").AsInt64().NotNullable().ForeignKey("fk_role_permissions_permission", "permissions", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("pk_role_permissions").OnTable("role_permissions").Columns("role_id", "permission_id");

        // 11. user_roles
        // TODO: BR-002 - Papéis com escopo por departamento serão modelados em fase futura. Nesta fase, user_roles é global.
        Create.Table("user_roles")
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("fk_user_roles_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("role_id").AsInt64().NotNullable().ForeignKey("fk_user_roles_role", "roles", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("scope").AsString(100).Nullable();

        Create.PrimaryKey("pk_user_roles").OnTable("user_roles").Columns("user_id", "role_id");

        // 12. audit_events
        // BR-004, BR-100: Tabela mínima de auditoria imutável
        // TODO: BR-004, BR-100 e M09 - Auditoria mínima nesta fase; outbox completo e assíncrono na fase correspondente.
        Create.Table("audit_events")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            .WithColumn("actor_user_id").AsInt64().Nullable()
            .WithColumn("actor_type").AsString(50).NotNullable().WithDefaultValue("User")
            .WithColumn("action").AsString(100).NotNullable()
            .WithColumn("entity_type").AsString(100).NotNullable()
            .WithColumn("entity_id").AsString(100).NotNullable()
            .WithColumn("correlation_id").AsString(100).Nullable()
            .WithColumn("ip_address").AsString(45).Nullable()
            .WithColumn("user_agent_summary").AsString(255).Nullable()
            .WithColumn("before_json").AsString(int.MaxValue).Nullable()
            .WithColumn("after_json").AsString(int.MaxValue).Nullable()
            .WithColumn("metadata_json").AsString(int.MaxValue).Nullable();

        Create.Index("ix_audit_events_entity").OnTable("audit_events").OnColumn("entity_type").Ascending().OnColumn("entity_id").Ascending();
        Create.Index("ix_audit_events_occurred_at").OnTable("audit_events").OnColumn("occurred_at");
        Create.Index("ix_audit_events_actor").OnTable("audit_events").OnColumn("actor_user_id");
    }

    public override void Down()
    {
        Delete.Table("audit_events");
        Delete.Table("user_roles");
        Delete.Table("role_permissions");
        Delete.Table("permissions");
        Delete.Table("roles");
        Delete.Table("user_teams");
        Delete.Table("user_departments");
        Delete.Table("teams");
        Delete.Table("departments");
        Delete.Table("password_reset_tokens");
        Delete.Table("user_sessions");
        Delete.Table("users");
    }
}
