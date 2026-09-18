using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091815, "Create case_relations schema and seed caso.relacionar permission (Phase 8 - M04/BR-030)")]
public class M20260918_15_CreateCaseRelationsSchemaAndPermission : Migration
{
    public override void Up()
    {
        // 1. Tabela case_relations
        if (!Schema.Table("case_relations").Exists())
        {
            Create.Table("case_relations")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("source_case_id").AsInt64().NotNullable().ForeignKey("fk_case_rel_source", "cases", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("target_case_id").AsInt64().NotNullable().ForeignKey("fk_case_rel_target", "cases", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("relation_type").AsString(40).NotNullable()
                .WithColumn("similarity_score").AsDouble().Nullable()
                .WithColumn("matched_factors_json").AsString(int.MaxValue).Nullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_case_rel_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("uk_case_relations_pair_type")
                .OnTable("case_relations")
                .OnColumn("source_case_id").Ascending()
                .OnColumn("target_case_id").Ascending()
                .OnColumn("relation_type").Ascending()
                .WithOptions().Unique();

            Create.Index("ix_case_relations_target")
                .OnTable("case_relations")
                .OnColumn("target_case_id");
        }

        // 2. Permissão caso.relacionar
        // A tabela permissions usa (code, description) — ver M20260917_01 e padrão de
        // seed de M20260918_11. Usar INSERT ... SELECT ... WHERE NOT EXISTS para idempotência.
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'caso.relacionar', 'Permite estabelecer relacionamentos determinísticos ou manuais entre casos'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'caso.relacionar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE p.code = 'caso.relacionar'
              AND r.name IN ('Admin', 'Admin Funcional', 'Especialista', 'Analista de Suporte')
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");
    }

    public override void Down()
    {
        if (Schema.Table("case_relations").Exists())
        {
            Delete.Table("case_relations");
        }

        Execute.Sql(@"
            DELETE rp FROM role_permissions rp
            INNER JOIN permissions p ON p.id = rp.permission_id
            WHERE p.code = 'caso.relacionar';

            DELETE FROM permissions WHERE code = 'caso.relacionar';
        ");
    }
}
