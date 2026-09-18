using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091813, "Structured Evidence and Hypothesis-Evidence N:N Relations (Phase 7.A - Bloco 7.A.4)")]
public class M20260918_13_StructuredEvidenceAndHypothesisRelations : Migration
{
    public override void Up()
    {
        // 1. Adicionar diagnostic_step_id na tabela case_evidences
        if (!Schema.Table("case_evidences").Column("diagnostic_step_id").Exists())
        {
            Alter.Table("case_evidences")
                .AddColumn("diagnostic_step_id").AsInt64().Nullable();

            Create.ForeignKey("fk_ce_diagnostic_step")
                .FromTable("case_evidences").ForeignColumn("diagnostic_step_id")
                .ToTable("diagnostic_steps").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_ce_diagnostic_step")
                .OnTable("case_evidences")
                .OnColumn("diagnostic_step_id").Ascending();
        }

        // 2. Criar tabela case_hypothesis_evidence (N:N com relation_type)
        if (!Schema.Table("case_hypothesis_evidence").Exists())
        {
            Create.Table("case_hypothesis_evidence")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("evidence_id").AsInt64().NotNullable()
                    .ForeignKey("fk_che_evidence", "case_evidences", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("hypothesis_id").AsInt64().NotNullable()
                    .ForeignKey("fk_che_hypothesis", "case_hypotheses", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("relation_type").AsString(50).NotNullable()
                .WithColumn("justification").AsString(2000).Nullable()
                .WithColumn("created_by").AsInt64().NotNullable()
                    .ForeignKey("fk_che_user", "users", "id")
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.UniqueConstraint("uk_che_evidence_hypothesis")
                .OnTable("case_hypothesis_evidence")
                .Columns("evidence_id", "hypothesis_id");

            Create.Index("ix_che_evidence")
                .OnTable("case_hypothesis_evidence")
                .OnColumn("evidence_id").Ascending();

            Create.Index("ix_che_hypothesis")
                .OnTable("case_hypothesis_evidence")
                .OnColumn("hypothesis_id").Ascending();
        }
    }

    public override void Down()
    {
        if (Schema.Table("case_hypothesis_evidence").Exists())
        {
            Delete.Table("case_hypothesis_evidence");
        }

        if (Schema.Table("case_evidences").Column("diagnostic_step_id").Exists())
        {
            Delete.ForeignKey("fk_ce_diagnostic_step").OnTable("case_evidences");
            Delete.Column("diagnostic_step_id").FromTable("case_evidences");
        }
    }
}
