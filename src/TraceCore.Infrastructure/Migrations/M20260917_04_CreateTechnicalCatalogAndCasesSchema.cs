using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091704, "Create Technical Catalog, Cases, Case Symptoms, Case Components, Attachments and Case Evidences")]
public class M20260917_04_CreateTechnicalCatalogAndCasesSchema : Migration
{
    public override void Up()
    {
        // 1. clients (Bloco 3.1 / Catálogo de clientes)
        Create.Table("clients")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("code").AsString(80).Nullable().Unique("uq_clients_code")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active")
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_clients_name").OnTable("clients").OnColumn("name");

        // 2. products (Catálogo Técnico)
        Create.Table("products")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("code").AsString(80).Nullable().Unique("uq_products_code")
            .WithColumn("name").AsString(180).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active")
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_products_name").OnTable("products").OnColumn("name");

        // 3. product_versions
        Create.Table("product_versions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("product_id").AsInt64().NotNullable().ForeignKey("fk_pv_product", "products", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("version_label").AsString(80).NotNullable()
            .WithColumn("released_at").AsDateTime().Nullable()
            .WithColumn("end_of_support_at").AsDateTime().Nullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active");

        Create.UniqueConstraint("uk_product_versions").OnTable("product_versions").Columns("product_id", "version_label");

        // 4. environments
        Create.Table("environments")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique("uq_environments_name")
            .WithColumn("environment_type").AsString(30).NotNullable();

        // 5. components (Módulos / Componentes do catálogo)
        Create.Table("components")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("product_id").AsInt64().Nullable().ForeignKey("fk_components_product", "products", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("code").AsString(100).Nullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("component_type").AsString(50).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("owner_department_id").AsInt64().Nullable().ForeignKey("fk_components_department", "departments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Active")
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Index("ix_components_type").OnTable("components").OnColumn("component_type");
        Create.Index("ix_components_name").OnTable("components").OnColumn("name");

        // 6. case_number_seq (Tabela de sequência para geração atômica de case_number estável e não reutilizável)
        Create.Table("case_number_seq")
            .WithColumn("id").AsInt64().PrimaryKey().Identity();

        // 7. cases (M04 - Casos e Incidentes)
        Create.Table("cases")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_number").AsInt64().NotNullable().Unique("uq_cases_case_number")
            .WithColumn("external_reference").AsString(160).Nullable()
            .WithColumn("source_type").AsString(50).NotNullable().WithDefaultValue("Manual")
            .WithColumn("client_id").AsInt64().Nullable().ForeignKey("fk_cases_client", "clients", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("product_id").AsInt64().Nullable().ForeignKey("fk_cases_product", "products", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("product_version_id").AsInt64().Nullable().ForeignKey("fk_cases_version", "product_versions", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("environment_id").AsInt64().Nullable().ForeignKey("fk_cases_environment", "environments", "id").OnDelete(System.Data.Rule.SetNull)
            // BR-020: Relato original imutável (MEDIUMTEXT)
            .WithColumn("original_report").AsString(16777215).NotNullable()
            // BR-021: Resumo normalizado separado
            .WithColumn("normalized_summary").AsString(int.MaxValue).Nullable()
            .WithColumn("expected_behavior").AsString(int.MaxValue).Nullable()
            .WithColumn("observed_behavior").AsString(int.MaxValue).Nullable()
            .WithColumn("error_code").AsString(180).Nullable()
            .WithColumn("error_message").AsString(int.MaxValue).Nullable()
            .WithColumn("scope_type").AsString(40).Nullable()
            // Prioridade mapeada para severity
            .WithColumn("severity").AsString(20).NotNullable()
            .WithColumn("impact_level").AsString(30).Nullable()
            // TODO: BR-024 / BR-027 - Status inicia sempre como 'Open'. Transições e triagem formal são escopo de fases futuras.
            .WithColumn("status").AsString(40).NotNullable().WithDefaultValue("Open")
            .WithColumn("current_owner_user_id").AsInt64().Nullable().ForeignKey("fk_cases_owner", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("current_department_id").AsInt64().Nullable().ForeignKey("fk_cases_department", "departments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("root_cause_status").AsString(40).NotNullable().WithDefaultValue("NotEvaluated")
            .WithColumn("opened_at").AsDateTime().NotNullable()
            .WithColumn("first_response_at").AsDateTime().Nullable()
            .WithColumn("resolved_at").AsDateTime().Nullable()
            .WithColumn("closed_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_cases_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_cases_updated_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("row_version").AsInt64().NotNullable().WithDefaultValue(1);

        Create.Index("ix_cases_status_opened").OnTable("cases").OnColumn("status").Ascending().OnColumn("opened_at").Descending();
        Create.Index("ix_cases_client_opened").OnTable("cases").OnColumn("client_id").Ascending().OnColumn("opened_at").Descending();
        Create.Index("ix_cases_product_opened").OnTable("cases").OnColumn("product_id").Ascending().OnColumn("opened_at").Descending();
        Create.Index("ix_cases_error_code").OnTable("cases").OnColumn("error_code");

        // 8. case_symptoms
        Create.Table("case_symptoms")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_case_symptoms_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("symptom_code").AsString(100).Nullable()
            .WithColumn("symptom_text").AsString(1000).NotNullable()
            .WithColumn("source").AsString(30).NotNullable().WithDefaultValue("Human")
            .WithColumn("confirmed").AsBoolean().NotNullable().WithDefaultValue(true);

        Create.Index("ix_case_symptoms_case").OnTable("case_symptoms").OnColumn("case_id");

        // 9. case_components (BR-023: múltiplos componentes/módulos)
        Create.Table("case_components")
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_case_components_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("component_id").AsInt64().NotNullable().ForeignKey("fk_case_components_component", "components", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("relation_type").AsString(30).NotNullable().WithDefaultValue("Affected")
            .WithColumn("confidence_label").AsString(30).Nullable();

        Create.PrimaryKey("pk_case_components").OnTable("case_components").Columns("case_id", "component_id", "relation_type");

        // 10. attachments (Auditoria/Plataforma polimórfico - ADR-P004)
        Create.Table("attachments")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("entity_type").AsString(120).NotNullable()
            .WithColumn("entity_id").AsInt64().NotNullable()
            .WithColumn("file_name").AsString(500).NotNullable()
            .WithColumn("mime_type").AsString(200).NotNullable()
            .WithColumn("size_bytes").AsInt64().NotNullable()
            .WithColumn("sha256").AsFixedLengthString(64).NotNullable()
            .WithColumn("storage_key").AsString(1000).NotNullable()
            .WithColumn("confidentiality").AsString(30).NotNullable().WithDefaultValue("Internal")
            .WithColumn("uploaded_by").AsInt64().NotNullable().ForeignKey("fk_attachments_uploaded_by", "users", "id")
            .WithColumn("uploaded_at").AsDateTime().NotNullable();

        Create.Index("ix_attachments_entity").OnTable("attachments").OnColumn("entity_type").Ascending().OnColumn("entity_id").Ascending();

        // 11. case_evidences (Extensão ao esqueleto para evidências de apoio)
        Create.Table("case_evidences")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_case_evidences_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("evidence_type").AsString(50).NotNullable().WithDefaultValue("Other")
            .WithColumn("description").AsString(int.MaxValue).NotNullable()
            .WithColumn("attachment_id").AsInt64().Nullable().ForeignKey("fk_case_evidences_attachment", "attachments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_case_evidences_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_case_evidences_case").OnTable("case_evidences").OnColumn("case_id");
    }

    public override void Down()
    {
        Delete.Table("case_evidences");
        Delete.Table("attachments");
        Delete.Table("case_components");
        Delete.Table("case_symptoms");
        Delete.Table("cases");
        Delete.Table("case_number_seq");
        Delete.Table("components");
        Delete.Table("environments");
        Delete.Table("product_versions");
        Delete.Table("products");
        Delete.Table("clients");
    }
}
