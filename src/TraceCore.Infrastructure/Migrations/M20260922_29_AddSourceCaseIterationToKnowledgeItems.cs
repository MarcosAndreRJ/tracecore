using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092229, "Add source_case_iteration_id to knowledge_items (M05 - bloquear multiplas solucoes ativas por caso)")]
public class M20260922_29_AddSourceCaseIterationToKnowledgeItems : Migration
{
    public override void Up()
    {
        // Rastreia qual CaseIteration (Case.Iterations) gerou este item de conhecimento,
        // permitindo diferenciar uma nova solução legítima (caso reaberto e resolvido
        // novamente em outra iteração) de uma tentativa de duplicar a solução já ativa.
        if (!Schema.Table("knowledge_items").Column("source_case_iteration_id").Exists())
            Alter.Table("knowledge_items").AddColumn("source_case_iteration_id").AsInt64().Nullable();
    }

    public override void Down()
    {
        if (Schema.Table("knowledge_items").Column("source_case_iteration_id").Exists())
            Delete.Column("source_case_iteration_id").FromTable("knowledge_items");
    }
}
