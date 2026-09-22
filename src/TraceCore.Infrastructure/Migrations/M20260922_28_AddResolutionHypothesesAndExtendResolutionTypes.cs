using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092228, "Fase 5 (Encerramento Estruturado): vínculo N:N entre resolução e hipóteses causadoras do caso, permitindo apontar mais de uma hipótese confirmada como causa raiz real investigada")]
public class M20260922_28_AddResolutionHypothesesAndExtendResolutionTypes : Migration
{
    public override void Up()
    {
        if (!Schema.Table("case_resolution_hypotheses").Exists())
        {
            Create.Table("case_resolution_hypotheses")
                .WithColumn("case_resolution_id").AsInt64().NotNullable().ForeignKey("fk_crh_resolution", "case_resolutions", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("case_hypothesis_id").AsInt64().NotNullable().ForeignKey("fk_crh_hypothesis", "case_hypotheses", "id").OnDelete(System.Data.Rule.Cascade);

            Create.PrimaryKey("pk_case_resolution_hypotheses")
                .OnTable("case_resolution_hypotheses")
                .Columns("case_resolution_id", "case_hypothesis_id");
        }

        // resolution_type continua string livre (sem CHECK constraint no schema atual) — a validação de
        // valores permitidos (Definitive, Workaround, NeedsFollowUp, Inconclusive, NotAnIssue,
        // ClientEnvironment) vive em CaseResolutionService, não requer alteração de schema.
    }

    public override void Down()
    {
        if (Schema.Table("case_resolution_hypotheses").Exists())
        {
            Delete.Table("case_resolution_hypotheses");
        }
    }
}
