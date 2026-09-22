using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092126, "Fase 05 (Ajuste Ecossistema): run_context/case_id em integration_runs e vínculo estrutural de executions em diagnostic_steps/case_evidences")]
public class M20260921_26_AddRunContextAndIntegrationRunLinks : Migration
{
    public override void Up()
    {
        // 1. integration_runs — origem da execução (Administrative, Diagnostic,
        // SolutionsValidation, AutomatedHealthCheck) e caso relacionado (quando houver),
        // suportando o mecanismo único de execução com contexto.
        if (Schema.Table("integration_runs").Exists())
        {
            if (!Schema.Table("integration_runs").Column("run_context").Exists())
            {
                Alter.Table("integration_runs")
                    .AddColumn("run_context").AsString(50).Nullable();
            }

            if (!Schema.Table("integration_runs").Column("case_id").Exists())
            {
                Alter.Table("integration_runs")
                    .AddColumn("case_id").AsInt64().Nullable()
                    .ForeignKey("fk_intrun_case", "cases", "id")
                    .OnDelete(System.Data.Rule.SetNull);
            }

            // Backfill idempotente com base no comportamento existente:
            // executions antigas que são logs administrativos (Manual) e verificações
            // automáticas (Automated) voltam a ser classificadas corretamente.
            Execute.Sql("""
                UPDATE integration_runs
                SET run_context = CASE triggered_by
                    WHEN 'Automated' THEN 'AutomatedHealthCheck'
                    WHEN 'Manual' THEN 'Administrative'
                    ELSE run_context
                END
                WHERE run_context IS NULL AND triggered_by IN ('Automated', 'Manual');
                """);
        }

        // 2. diagnostic_steps — execução de integração que fundamenta o passo (teste real)
        if (Schema.Table("diagnostic_steps").Exists() && !Schema.Table("diagnostic_steps").Column("integration_run_id").Exists())
        {
            Alter.Table("diagnostic_steps")
                .AddColumn("integration_run_id").AsInt64().Nullable()
                .ForeignKey("fk_dstep_intrun", "integration_runs", "id")
                .OnDelete(System.Data.Rule.SetNull);
        }

        // 3. case_evidences — execução de integração cujo resultado fundamenta a evidência
        if (Schema.Table("case_evidences").Exists() && !Schema.Table("case_evidences").Column("integration_run_id").Exists())
        {
            Alter.Table("case_evidences")
                .AddColumn("integration_run_id").AsInt64().Nullable()
                .ForeignKey("fk_caseev_intrun", "integration_runs", "id")
                .OnDelete(System.Data.Rule.SetNull);
        }
    }

    public override void Down()
    {
        if (Schema.Table("case_evidences").Exists() && Schema.Table("case_evidences").Column("integration_run_id").Exists())
        {
            Delete.ForeignKey("fk_caseev_intrun").OnTable("case_evidences");
            Delete.Column("integration_run_id").FromTable("case_evidences");
        }

        if (Schema.Table("diagnostic_steps").Exists() && Schema.Table("diagnostic_steps").Column("integration_run_id").Exists())
        {
            Delete.ForeignKey("fk_dstep_intrun").OnTable("diagnostic_steps");
            Delete.Column("integration_run_id").FromTable("diagnostic_steps");
        }

        if (Schema.Table("integration_runs").Exists())
        {
            if (Schema.Table("integration_runs").Column("case_id").Exists())
            {
                Delete.ForeignKey("fk_intrun_case").OnTable("integration_runs");
                Delete.Column("case_id").FromTable("integration_runs");
            }

            if (Schema.Table("integration_runs").Column("run_context").Exists())
            {
                Delete.Column("run_context").FromTable("integration_runs");
            }
        }
    }
}