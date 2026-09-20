using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091922, "Product technical context for investigation: profile, technologies, sources, external research allowlist, integration-product link (Prompt 2/4 - Copiloto)")]
public class M20260919_22_CreateProductTechnicalContextSchema : Migration
{
    public override void Up()
    {
        // 1. product_technical_profiles — relação 1:0..1 com products. Produto sem
        // perfil continua funcionando normalmente em todo o restante do sistema.
        if (!Schema.Table("product_technical_profiles").Exists())
        {
            Create.Table("product_technical_profiles")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_id").AsInt64().NotNullable().Unique().ForeignKey("fk_ptp_product", "products", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("business_purpose").AsString(int.MaxValue).Nullable()
                .WithColumn("architecture_summary").AsString(int.MaxValue).Nullable()
                .WithColumn("frontend_stack").AsString(255).Nullable()
                .WithColumn("backend_stack").AsString(255).Nullable()
                .WithColumn("primary_database").AsString(255).Nullable()
                .WithColumn("runtime_platform").AsString(255).Nullable()
                .WithColumn("hosting_model").AsString(255).Nullable()
                .WithColumn("authentication_model").AsString(255).Nullable()
                .WithColumn("observability_stack").AsString(255).Nullable()
                .WithColumn("deployment_model").AsString(255).Nullable()
                .WithColumn("vendor").AsString(255).Nullable()
                .WithColumn("support_notes").AsString(int.MaxValue).Nullable()
                .WithColumn("known_constraints").AsString(int.MaxValue).Nullable()
                .WithColumn("investigation_notes").AsString(int.MaxValue).Nullable()
                .WithColumn("external_research_policy").AsString(50).NotNullable().WithDefaultValue("Disabled")
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_ptp_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_ptp_updated_by", "users", "id").OnDelete(System.Data.Rule.SetNull);
        }

        // 2. product_technologies — reaproveita a tabela technologies já existente
        // (Fase 6 / Knowledge Base), só adiciona a associação produto <-> tecnologia.
        // NÃO cria uma segunda tabela de tecnologias.
        if (!Schema.Table("product_technologies").Exists())
        {
            Create.Table("product_technologies")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_id").AsInt64().NotNullable().ForeignKey("fk_pt_product", "products", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("technology_id").AsInt64().NotNullable().ForeignKey("fk_pt_technology", "technologies", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.UniqueConstraint("uk_product_technologies").OnTable("product_technologies").Columns("product_id", "technology_id");
            Create.Index("ix_pt_product").OnTable("product_technologies").OnColumn("product_id");
        }

        // 3. product_technical_sources
        if (!Schema.Table("product_technical_sources").Exists())
        {
            Create.Table("product_technical_sources")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_id").AsInt64().NotNullable().ForeignKey("fk_pts_product", "products", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("name").AsString(255).NotNullable()
                .WithColumn("source_type").AsString(50).NotNullable()
                .WithColumn("url").AsString(500).Nullable()
                .WithColumn("description").AsString(int.MaxValue).Nullable()
                .WithColumn("trust_level").AsString(50).NotNullable().WithDefaultValue("Reference")
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_pts_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_pts_updated_by", "users", "id").OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_pts_product").OnTable("product_technical_sources").OnColumn("product_id");
            Create.Index("ix_pts_is_active").OnTable("product_technical_sources").OnColumn("is_active");
            Create.Index("ix_pts_source_type").OnTable("product_technical_sources").OnColumn("source_type");
        }

        // 4. product_external_research_domains
        if (!Schema.Table("product_external_research_domains").Exists())
        {
            Create.Table("product_external_research_domains")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("product_id").AsInt64().NotNullable().ForeignKey("fk_perd_product", "products", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("domain").AsString(255).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_perd_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull);

            Create.UniqueConstraint("uk_perd_product_domain").OnTable("product_external_research_domains").Columns("product_id", "domain");
            Create.Index("ix_perd_product").OnTable("product_external_research_domains").OnColumn("product_id");
        }

        // 5. Vínculo opcional Integration -> Product (aditivo; integrações sem produto
        // continuam existindo e funcionando exatamente como hoje).
        if (!Schema.Table("integrations").Column("product_id").Exists())
        {
            Alter.Table("integrations")
                .AddColumn("product_id").AsInt64().Nullable().ForeignKey("fk_integrations_product", "products", "id").OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_integrations_product").OnTable("integrations").OnColumn("product_id");
        }
    }

    public override void Down()
    {
        if (Schema.Table("integrations").Column("product_id").Exists())
        {
            Delete.ForeignKey("fk_integrations_product").OnTable("integrations");
            Delete.Column("product_id").FromTable("integrations");
        }

        if (Schema.Table("product_external_research_domains").Exists()) Delete.Table("product_external_research_domains");
        if (Schema.Table("product_technical_sources").Exists()) Delete.Table("product_technical_sources");
        if (Schema.Table("product_technologies").Exists()) Delete.Table("product_technologies");
        if (Schema.Table("product_technical_profiles").Exists()) Delete.Table("product_technical_profiles");
    }
}
