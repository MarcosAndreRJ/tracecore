using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091818, "Create searchable_content_entries schema (Phase 12 - M11 / AI Structural Readiness)")]
public class M20260918_18_CreateSearchableContentEntriesSchema : Migration
{
    public override void Up()
    {
        if (!Schema.Table("searchable_content_entries").Exists())
        {
            Create.Table("searchable_content_entries")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("source_type").AsString(50).NotNullable()
                .WithColumn("source_id").AsInt64().NotNullable()
                .WithColumn("source_version_id").AsInt64().Nullable()
                .WithColumn("title").AsString(500).NotNullable()
                .WithColumn("normalized_content").AsString(int.MaxValue).NotNullable()
                .WithColumn("content_hash").AsString(64).NotNullable()
                .WithColumn("validation_status").AsString(50).NotNullable()
                .WithColumn("quality_status").AsString(50).NotNullable()
                .WithColumn("visibility").AsString(50).NotNullable()
                .WithColumn("client_id").AsInt64().Nullable().ForeignKey("fk_searchable_client", "clients", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("product_id").AsInt64().Nullable().ForeignKey("fk_searchable_product", "products", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("component_ids_json").AsString(4000).Nullable()
                .WithColumn("metadata_json").AsString(int.MaxValue).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().NotNullable()
                .WithColumn("source_updated_at").AsDateTime().NotNullable()
                .WithColumn("indexed_at").AsDateTime().Nullable()
                .WithColumn("embedding_version").AsString(50).Nullable();

            Create.Index("ix_searchable_source")
                .OnTable("searchable_content_entries")
                .OnColumn("source_type").Ascending()
                .OnColumn("source_id").Ascending()
                .OnColumn("source_version_id").Ascending();

            Create.Index("ix_searchable_hash")
                .OnTable("searchable_content_entries")
                .OnColumn("content_hash").Ascending();

            Create.Index("ix_searchable_quality")
                .OnTable("searchable_content_entries")
                .OnColumn("quality_status").Ascending();

            Create.Index("ix_searchable_validation")
                .OnTable("searchable_content_entries")
                .OnColumn("validation_status").Ascending();

            Create.Index("ix_searchable_updated")
                .OnTable("searchable_content_entries")
                .OnColumn("updated_at").Descending();
        }
    }

    public override void Down()
    {
        if (Schema.Table("searchable_content_entries").Exists())
        {
            Delete.Table("searchable_content_entries");
        }
    }
}
