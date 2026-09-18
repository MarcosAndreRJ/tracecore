using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091810, "Client enhancements, Admin role rename and drop teams (Phase 7.A - Bloco 7.A.1)")]
public class M20260918_10_ClientEnhancementsAdminRenameAndDropTeams : Migration
{
    public override void Up()
    {
        // 1. Clientes: external_crm_id e notes
        if (!Schema.Table("clients").Column("external_crm_id").Exists())
        {
            Alter.Table("clients")
                .AddColumn("external_crm_id").AsString(100).Nullable().Unique();
        }

        if (!Schema.Table("clients").Column("notes").Exists())
        {
            Alter.Table("clients")
                .AddColumn("notes").AsString(int.MaxValue).Nullable();
        }

        // 2. Unidades de Clientes (client_units)
        if (!Schema.Table("client_units").Exists())
        {
            Create.Table("client_units")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("client_id").AsInt64().NotNullable().ForeignKey("fk_client_units_client", "clients", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("code").AsString(50).NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active")
                .WithColumn("external_crm_id").AsString(100).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("created_by").AsInt64().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable();

            Create.Index("ix_client_units_client_id")
                .OnTable("client_units")
                .OnColumn("client_id");

            Create.Index("ix_client_units_crm")
                .OnTable("client_units")
                .OnColumn("external_crm_id");
        }

        // 3. Contextos Técnicos de Clientes (client_technical_contexts)
        if (!Schema.Table("client_technical_contexts").Exists())
        {
            Create.Table("client_technical_contexts")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("client_id").AsInt64().NotNullable().ForeignKey("fk_ctc_client", "clients", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("client_unit_id").AsInt64().Nullable().ForeignKey("fk_ctc_unit", "client_units", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("product_id").AsInt64().NotNullable().ForeignKey("fk_ctc_product", "products", "id")
                .WithColumn("product_version_id").AsInt64().Nullable().ForeignKey("fk_ctc_version", "product_versions", "id")
                .WithColumn("environment_id").AsInt64().Nullable().ForeignKey("fk_ctc_env", "environments", "id")
                .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active")
                .WithColumn("effective_from").AsDateTime().NotNullable()
                .WithColumn("effective_to").AsDateTime().Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("created_by").AsInt64().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable();

            Create.Index("ix_ctc_client_id")
                .OnTable("client_technical_contexts")
                .OnColumn("client_id");
        }

        // 4. Renomear papel 'Super Admin (Dev)' -> 'Admin'
        Execute.Sql(@"
            UPDATE roles 
            SET name = 'Admin', 
                description = 'Administrador com controle total sobre configurações e permissões da plataforma'
            WHERE name = 'Super Admin (Dev)';
        ");

        // 5. Cadastrar nova permissão cliente.gerenciar e conceder a Admin e Admin Funcional
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'cliente.gerenciar', 'Permite cadastrar, editar e gerenciar clientes, unidades e seus contextos técnicos'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'cliente.gerenciar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.name IN ('Admin', 'Admin Funcional')
              AND p.code = 'cliente.gerenciar'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");

        // 6. Remover tabelas obsoletas de Team
        Execute.Sql("DROP TABLE IF EXISTS user_teams; DROP TABLE IF EXISTS teams;");
    }

    public override void Down()
    {
        Delete.Table("client_technical_contexts");
        Delete.Table("client_units");
        Delete.Column("external_crm_id").FromTable("clients");
        Delete.Column("notes").FromTable("clients");
    }
}
