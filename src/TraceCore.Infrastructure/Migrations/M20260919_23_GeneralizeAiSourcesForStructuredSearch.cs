using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091923, "Generalize ai_sources beyond embeddings (structured/case/knowledge sources) and add ai_interactions.metadata_json (Prompt 3 - Copiloto investigativo)")]
public class M20260919_23_GeneralizeAiSourcesForStructuredSearch : Migration
{
    public override void Up()
    {
        // 1. ai_interactions.metadata_json — registra estratégias de recuperação usadas
        // (structured search, similaridade de casos, contexto técnico, semântica, pesquisa
        // externa) e contadores de observabilidade. Uma única coluna, aditiva.
        if (!Schema.Table("ai_interactions").Column("metadata_json").Exists())
        {
            Alter.Table("ai_interactions").AddColumn("metadata_json").AsString(int.MaxValue).Nullable();
        }

        // 2. ai_sources: originalmente toda fonte precisava apontar para uma entrada
        // indexada por embedding (searchable_content_entry_id NOT NULL) e ter uma
        // similaridade de cosseno (similarity_score NOT NULL). O novo Copiloto
        // investigativo (Prompt 3) encontra fontes por busca estruturada (caso,
        // conhecimento, contexto técnico) que não passam por embedding e não têm
        // similaridade de cosseno — só um score determinístico (0-100) ou nenhum.
        // Mudança aditiva: RAG semântico existente continua preenchendo
        // searchable_content_entry_id + similarity_score exatamente como antes.
        if (Schema.Table("ai_sources").Column("searchable_content_entry_id").Exists())
        {
            Delete.ForeignKey("fk_aisrc_entry").OnTable("ai_sources");
            Alter.Table("ai_sources").AlterColumn("searchable_content_entry_id").AsInt64().Nullable();
            Create.ForeignKey("fk_aisrc_entry")
                .FromTable("ai_sources").ForeignColumn("searchable_content_entry_id")
                .ToTable("searchable_content_entries").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        if (Schema.Table("ai_sources").Column("similarity_score").Exists())
        {
            Alter.Table("ai_sources").AlterColumn("similarity_score").AsDecimal(8, 5).Nullable();
        }

        if (!Schema.Table("ai_sources").Column("source_type").Exists())
        {
            Alter.Table("ai_sources").AddColumn("source_type").AsString(50).Nullable();
        }
        if (!Schema.Table("ai_sources").Column("source_ref_id").Exists())
        {
            Alter.Table("ai_sources").AddColumn("source_ref_id").AsInt64().Nullable();
        }
        if (!Schema.Table("ai_sources").Column("source_url").Exists())
        {
            Alter.Table("ai_sources").AddColumn("source_url").AsString(1000).Nullable();
        }
        if (!Schema.Table("ai_sources").Column("source_title").Exists())
        {
            Alter.Table("ai_sources").AddColumn("source_title").AsString(500).Nullable();
        }
        if (!Schema.Table("ai_sources").Column("match_score").Exists())
        {
            Alter.Table("ai_sources").AddColumn("match_score").AsDecimal(8, 3).Nullable();
        }

        // Backfill: fontes existentes (todas do RAG semântico até aqui) recebem
        // source_type explícito para não ficarem com o campo novo vazio.
        Execute.Sql(@"
            UPDATE ai_sources
            SET source_type = 'SearchableContent'
            WHERE source_type IS NULL AND searchable_content_entry_id IS NOT NULL;
        ");

        Create.Index("ix_ai_sources_source_type").OnTable("ai_sources").OnColumn("source_type");
    }

    public override void Down()
    {
        if (Schema.Table("ai_sources").Column("match_score").Exists())
            Delete.Column("match_score").FromTable("ai_sources");
        if (Schema.Table("ai_sources").Column("source_title").Exists())
            Delete.Column("source_title").FromTable("ai_sources");
        if (Schema.Table("ai_sources").Column("source_url").Exists())
            Delete.Column("source_url").FromTable("ai_sources");
        if (Schema.Table("ai_sources").Column("source_ref_id").Exists())
            Delete.Column("source_ref_id").FromTable("ai_sources");
        if (Schema.Table("ai_sources").Column("source_type").Exists())
            Delete.Column("source_type").FromTable("ai_sources");

        if (Schema.Table("ai_interactions").Column("metadata_json").Exists())
            Delete.Column("metadata_json").FromTable("ai_interactions");
    }
}
