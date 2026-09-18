using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091808, "Create knowledge base schema: items, versions, applicability, symptoms, steps, technologies, tags and usages (Phase 6 - M05)")]
public class M20260918_08_CreateKnowledgeBaseSchema : Migration
{
    public override void Up()
    {
        // 1. knowledge_items
        Create.Table("knowledge_items")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_code").AsString(80).NotNullable().Unique("uk_knowledge_code")
            .WithColumn("knowledge_type").AsString(50).NotNullable().WithDefaultValue("Solution")
            .WithColumn("title").AsString(400).NotNullable()
            .WithColumn("summary").AsString(int.MaxValue).NotNullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Draft")
            .WithColumn("confidentiality").AsString(30).NotNullable().WithDefaultValue("Internal")
            .WithColumn("owner_user_id").AsInt64().Nullable().ForeignKey("fk_ki_owner", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("owner_department_id").AsInt64().Nullable().ForeignKey("fk_ki_dept", "departments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("current_version_no").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("provenance_type").AsString(30).NotNullable().WithDefaultValue("Case")
            .WithColumn("provenance_case_id").AsInt64().Nullable().ForeignKey("fk_ki_prov_case", "cases", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("provenance_reference").AsString(255).Nullable()
            .WithColumn("review_due_at").AsDateTime().Nullable()
            .WithColumn("last_reviewed_at").AsDateTime().Nullable()
            .WithColumn("published_at").AsDateTime().Nullable()
            .WithColumn("deprecated_at").AsDateTime().Nullable()
            .WithColumn("replacement_knowledge_id").AsInt64().Nullable().ForeignKey("fk_ki_replacement", "knowledge_items", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsInt64().NotNullable().ForeignKey("fk_ki_creator", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_knowledge_status_review")
            .OnTable("knowledge_items")
            .OnColumn("status").Ascending()
            .OnColumn("review_due_at").Ascending();

        // 2. knowledge_versions
        Create.Table("knowledge_versions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_item_id").AsInt64().NotNullable().ForeignKey("fk_kv_item", "knowledge_items", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("version_no").AsInt32().NotNullable()
            .WithColumn("content_markdown").AsString(16777215).NotNullable()
            .WithColumn("problem_description").AsString(16777215).Nullable()
            .WithColumn("root_cause_summary").AsString(16777215).Nullable()
            .WithColumn("validation_method").AsString(16777215).Nullable()
            .WithColumn("risk_warning").AsString(int.MaxValue).Nullable()
            .WithColumn("rollback_plan").AsString(int.MaxValue).Nullable()
            .WithColumn("change_summary").AsString(1000).Nullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Draft")
            .WithColumn("content_hash").AsString(64).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsInt64().NotNullable().ForeignKey("fk_kv_creator", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("approved_by").AsInt64().Nullable().ForeignKey("fk_kv_approver", "users", "id").OnDelete(System.Data.Rule.SetNull);

        Create.Index("uk_knowledge_version")
            .OnTable("knowledge_versions")
            .OnColumn("knowledge_item_id").Ascending()
            .OnColumn("version_no").Ascending()
            .WithOptions().Unique();

        // 3. knowledge_applicability (BR-043)
        Create.Table("knowledge_applicability")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_item_id").AsInt64().NotNullable().ForeignKey("fk_ka_item", "knowledge_items", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("product_id").AsInt64().Nullable().ForeignKey("fk_ka_product", "products", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("product_version_id").AsInt64().Nullable().ForeignKey("fk_ka_version", "product_versions", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("component_id").AsInt64().Nullable().ForeignKey("fk_ka_comp", "components", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("environment_id").AsInt64().Nullable().ForeignKey("fk_ka_env", "environments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("applicability_type").AsString(30).NotNullable().WithDefaultValue("Applies")
            .WithColumn("notes").AsString(1000).Nullable();

        Create.Index("ix_ka_item")
            .OnTable("knowledge_applicability")
            .OnColumn("knowledge_item_id");

        // 4. knowledge_symptoms
        Create.Table("knowledge_symptoms")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_version_id").AsInt64().NotNullable().ForeignKey("fk_ks_version", "knowledge_versions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("symptom_text").AsString(1000).NotNullable();

        Create.Index("ix_ks_version")
            .OnTable("knowledge_symptoms")
            .OnColumn("knowledge_version_id");

        // 5. knowledge_steps
        Create.Table("knowledge_steps")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_version_id").AsInt64().NotNullable().ForeignKey("fk_kstep_version", "knowledge_versions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("sequence_no").AsInt32().NotNullable()
            .WithColumn("step_type").AsString(40).NotNullable().WithDefaultValue("Solution")
            .WithColumn("title").AsString(300).NotNullable()
            .WithColumn("description").AsString(16777215).NotNullable()
            .WithColumn("command").AsString(int.MaxValue).Nullable()
            .WithColumn("expected_output").AsString(int.MaxValue).Nullable();

        Create.Index("uk_kstep_sequence")
            .OnTable("knowledge_steps")
            .OnColumn("knowledge_version_id").Ascending()
            .OnColumn("sequence_no").Ascending()
            .WithOptions().Unique();

        // 6. technologies & knowledge_technologies
        Create.Table("technologies")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique("uk_technologies_name");

        Create.Table("knowledge_technologies")
            .WithColumn("knowledge_item_id").AsInt64().NotNullable().ForeignKey("fk_kt_item", "knowledge_items", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("technology_id").AsInt64().NotNullable().ForeignKey("fk_kt_tech", "technologies", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("pk_knowledge_technologies")
            .OnTable("knowledge_technologies")
            .Columns("knowledge_item_id", "technology_id");

        // 7. tags & knowledge_tags
        Create.Table("tags")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique("uk_tags_name");

        Create.Table("knowledge_tags")
            .WithColumn("knowledge_item_id").AsInt64().NotNullable().ForeignKey("fk_ktag_item", "knowledge_items", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("tag_id").AsInt64().NotNullable().ForeignKey("fk_ktag_tag", "tags", "id").OnDelete(System.Data.Rule.Cascade);

        Create.PrimaryKey("pk_knowledge_tags")
            .OnTable("knowledge_tags")
            .Columns("knowledge_item_id", "tag_id");

        // 8. knowledge_usages (BR-047, BR-048)
        Create.Table("knowledge_usages")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("knowledge_item_id").AsInt64().NotNullable().ForeignKey("fk_ku_item", "knowledge_items", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("knowledge_version_id").AsInt64().NotNullable().ForeignKey("fk_ku_version", "knowledge_versions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_ku_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("used_by").AsInt64().NotNullable().ForeignKey("fk_ku_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("used_at").AsDateTime().NotNullable()
            .WithColumn("outcome").AsString(40).NotNullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("context_match_json").AsString(int.MaxValue).Nullable();

        Create.Index("ix_ku_item_outcome")
            .OnTable("knowledge_usages")
            .OnColumn("knowledge_item_id").Ascending()
            .OnColumn("outcome").Ascending()
            .OnColumn("used_at").Descending();

        Create.Index("ix_ku_case")
            .OnTable("knowledge_usages")
            .OnColumn("case_id");

        // Seed inicial de tecnologias e tags
        Insert.IntoTable("technologies")
            .Row(new { name = "Kubernetes" })
            .Row(new { name = "Kafka" })
            .Row(new { name = "Redis" })
            .Row(new { name = "PostgreSQL" })
            .Row(new { name = "MySQL" })
            .Row(new { name = ".NET Core" })
            .Row(new { name = "Docker" })
            .Row(new { name = "RabbitMQ" });

        Insert.IntoTable("tags")
            .Row(new { name = "deadlock" })
            .Row(new { name = "connection-pool" })
            .Row(new { name = "timeout" })
            .Row(new { name = "latency" })
            .Row(new { name = "cache" })
            .Row(new { name = "memory-leak" })
            .Row(new { name = "mtls" })
            .Row(new { name = "sre" });
    }

    public override void Down()
    {
        Delete.Table("knowledge_usages");
        Delete.Table("knowledge_tags");
        Delete.Table("tags");
        Delete.Table("knowledge_technologies");
        Delete.Table("technologies");
        Delete.Table("knowledge_steps");
        Delete.Table("knowledge_symptoms");
        Delete.Table("knowledge_applicability");
        Delete.Table("knowledge_versions");
        Delete.Table("knowledge_items");
    }
}
