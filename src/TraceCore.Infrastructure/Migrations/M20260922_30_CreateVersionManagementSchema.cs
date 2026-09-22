using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092230, "Versionamento Inteligente - Fase 1: release_order em product_versions, tabelas de alteracoes por versao, vinculo alteracao<->caso e destinacao (rollout) para clientes")]
public class M20260922_30_CreateVersionManagementSchema : Migration
{
    public override void Up()
    {
        // 1. release_order em product_versions — ordem real de lançamento, única por produto.
        // NUNCA usar a label da versão para ordenar (labels arbitrárias, ex.: "5.9" > "5.10").
        if (!Schema.Table("product_versions").Column("release_order").Exists())
        {
            Alter.Table("product_versions")
                .AddColumn("release_order").AsInt32().NotNullable().WithDefaultValue(0);

            // Backfill: ordem sequencial por produto, ordenando por data de lançamento
            // (versões sem data por último) e, em empate, pelo id (estável).
            Execute.Sql(@"
                UPDATE product_versions pv
                JOIN (
                    SELECT pv2.id,
                           ROW_NUMBER() OVER (
                               PARTITION BY pv2.product_id
                               ORDER BY (pv2.released_at IS NULL) ASC, pv2.released_at ASC, pv2.id ASC
                           ) AS rn
                    FROM product_versions pv2
                ) seq ON seq.id = pv.id
                SET pv.release_order = seq.rn;");

            Create.UniqueConstraint("uk_product_versions_release_order")
                .OnTable("product_versions")
                .Columns("product_id", "release_order");

            Create.Index("ix_product_versions_release_order").OnTable("product_versions").OnColumn("release_order");
        }

        // 2. product_version_changes — itens entregues em cada versão.
        if (!Schema.Table("product_version_changes").Exists())
        {
            Create.Table("product_version_changes")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_version_id").AsInt64().NotNullable().ForeignKey("fk_pvc_version", "product_versions", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("change_type").AsString(40).NotNullable()
                .WithColumn("title").AsString(500).NotNullable()
                .WithColumn("description").AsString(int.MaxValue).Nullable()
                .WithColumn("component_id").AsInt64().Nullable().ForeignKey("fk_pvc_component", "components", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("error_code").AsString(180).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_pvc_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_pvc_updated_by", "users", "id").OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_pvc_version").OnTable("product_version_changes").OnColumn("product_version_id");
            Create.Index("ix_pvc_change_type").OnTable("product_version_changes").OnColumn("change_type");
            Create.Index("ix_pvc_component").OnTable("product_version_changes").OnColumn("component_id");
            Create.Index("ix_pvc_error_code").OnTable("product_version_changes").OnColumn("error_code");
        }

        // 3. product_version_change_cases — N:N alteração <-> caso (FixedBy/Related and beyond).
        if (!Schema.Table("product_version_change_cases").Exists())
        {
            Create.Table("product_version_change_cases")
                .WithColumn("product_version_change_id").AsInt64().NotNullable().ForeignKey("fk_pvcc_change", "product_version_changes", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_pvcc_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("relation_type").AsString(40).NotNullable()
                .WithColumn("match_score").AsDecimal(6, 4).Nullable()
                .WithColumn("matched_factors_json").AsString(int.MaxValue).Nullable()
                .WithColumn("linked_by").AsInt64().Nullable().ForeignKey("fk_pvcc_linked_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("linked_at").AsDateTime().NotNullable();

            Create.PrimaryKey("pk_pvcc").OnTable("product_version_change_cases")
                .Columns("product_version_change_id", "case_id", "relation_type");
            Create.Index("ix_pvcc_case").OnTable("product_version_change_cases").OnColumn("case_id");
        }

        // 4. product_version_assignments — destinação (rollout) de versão para cliente/unidade.
        // Uma atribuição não muda a versão corrente do cliente; somente o deploy confirmado
        // (status Deployed) atualiza client_technical_contexts.product_version_id.
        if (!Schema.Table("product_version_assignments").Exists())
        {
            Create.Table("product_version_assignments")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_version_id").AsInt64().NotNullable().ForeignKey("fk_pva_version", "product_versions", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("client_id").AsInt64().NotNullable().ForeignKey("fk_pva_client", "clients", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("client_unit_id").AsInt64().Nullable().ForeignKey("fk_pva_client_unit", "client_units", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Planned")
                .WithColumn("planned_at").AsDateTime().NotNullable()
                .WithColumn("scheduled_at").AsDateTime().Nullable()
                .WithColumn("deployed_at").AsDateTime().Nullable()
                .WithColumn("notes").AsString(int.MaxValue).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_pva_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_pva_updated_by", "users", "id").OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_pva_version").OnTable("product_version_assignments").OnColumn("product_version_id");
            Create.Index("ix_pva_client").OnTable("product_version_assignments").OnColumn("client_id");
            Create.Index("ix_pva_status").OnTable("product_version_assignments").OnColumn("status");
        }
    }

    public override void Down()
    {
        if (Schema.Table("product_version_assignments").Exists()) Delete.Table("product_version_assignments");
        if (Schema.Table("product_version_change_cases").Exists()) Delete.Table("product_version_change_cases");
        if (Schema.Table("product_version_changes").Exists()) Delete.Table("product_version_changes");

        if (Schema.Table("product_versions").Column("release_order").Exists())
        {
            if (Schema.Table("product_versions").Constraint("uk_product_versions_release_order").Exists())
                Delete.UniqueConstraint("uk_product_versions_release_order").FromTable("product_versions");
            Delete.Column("release_order").FromTable("product_versions");
        }
    }
}