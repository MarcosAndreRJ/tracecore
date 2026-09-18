using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091811, "Technical catalog dependencies, owners and product enhancements (Phase 7.A - Bloco 7.A.2)")]
public class M20260918_11_TechnicalCatalogDependenciesAndOwners : Migration
{
    public override void Up()
    {
        // 1. products: adicionar is_external (boolean, default false)
        if (!Schema.Table("products").Column("is_external").Exists())
        {
            Alter.Table("products")
                .AddColumn("is_external").AsBoolean().NotNullable().WithDefaultValue(false);
        }

        // 2. component_dependencies
        if (!Schema.Table("component_dependencies").Exists())
        {
            Create.Table("component_dependencies")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("source_component_id").AsInt64().NotNullable().ForeignKey("fk_cd_source", "components", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("target_component_id").AsInt64().NotNullable().ForeignKey("fk_cd_target", "components", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("dependency_type").AsString(50).NotNullable()
                .WithColumn("criticality").AsString(50).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("valid_from").AsDateTime().Nullable()
                .WithColumn("valid_to").AsDateTime().Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.UniqueConstraint("uk_component_dependencies")
                .OnTable("component_dependencies")
                .Columns("source_component_id", "target_component_id", "dependency_type");

            Create.Index("ix_cd_source").OnTable("component_dependencies").OnColumn("source_component_id");
            Create.Index("ix_cd_target").OnTable("component_dependencies").OnColumn("target_component_id");
        }

        // 3. component_owners (departamento oficial)
        if (!Schema.Table("component_owners").Exists())
        {
            Create.Table("component_owners")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("component_id").AsInt64().NotNullable().ForeignKey("fk_co_component", "components", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("department_id").AsInt64().NotNullable().ForeignKey("fk_co_department", "departments", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("ownership_role").AsString(50).NotNullable() // Primary, Secondary, Escalation
                .WithColumn("valid_from").AsDateTime().Nullable()
                .WithColumn("valid_to").AsDateTime().Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("ix_co_component").OnTable("component_owners").OnColumn("component_id");
            Create.Index("ix_co_department").OnTable("component_owners").OnColumn("department_id");
        }

        // 4. Permissão catalogo.gerenciar
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'catalogo.gerenciar', 'Permite cadastrar, editar e gerenciar produtos, componentes, dependências e ownership no catálogo técnico'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'catalogo.gerenciar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.name IN ('Admin', 'Admin Funcional')
              AND p.code = 'catalogo.gerenciar'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");
    }

    public override void Down()
    {
        Delete.Table("component_owners");
        Delete.Table("component_dependencies");
        Delete.Column("is_external").FromTable("products");
    }
}
