using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091820, "Fase 15 (M10 - Integrações Automáticas): health-check HTTP/TCP para integrações, triggered_by em integration_runs e vínculo de automated check")]
public class M20260918_20_AddHealthCheckToIntegrationsAndDiagnosticCheck : Migration
{
    public override void Up()
    {
        // 1. integrations — campos para verificação real de saúde HTTP/TCP
        if (Schema.Table("integrations").Exists())
        {
            if (!Schema.Table("integrations").Column("health_check_url").Exists())
            {
                Alter.Table("integrations")
                    .AddColumn("health_check_url").AsString(1000).Nullable();
            }

            if (!Schema.Table("integrations").Column("health_check_method").Exists())
            {
                Alter.Table("integrations")
                    .AddColumn("health_check_method").AsString(32).NotNullable().WithDefaultValue("Http");
            }

            if (!Schema.Table("integrations").Column("health_check_timeout_seconds").Exists())
            {
                Alter.Table("integrations")
                    .AddColumn("health_check_timeout_seconds").AsInt32().NotNullable().WithDefaultValue(5);
            }

            if (!Schema.Table("integrations").Column("health_check_expected_status_code").Exists())
            {
                Alter.Table("integrations")
                    .AddColumn("health_check_expected_status_code").AsInt32().Nullable().WithDefaultValue(200);
            }
        }

        // 2. integration_runs — triggered_by ('Manual' ou 'Automated')
        if (Schema.Table("integration_runs").Exists() && !Schema.Table("integration_runs").Column("triggered_by").Exists())
        {
            Alter.Table("integration_runs")
                .AddColumn("triggered_by").AsString(32).NotNullable().WithDefaultValue("Manual");
        }

        // 3. diagnostic_checks — integration_id (FK para automated checks)
        if (Schema.Table("diagnostic_checks").Exists() && !Schema.Table("diagnostic_checks").Column("integration_id").Exists())
        {
            Alter.Table("diagnostic_checks")
                .AddColumn("integration_id").AsInt64().Nullable()
                .ForeignKey("fk_diag_check_integration", "integrations", "id")
                .OnDelete(System.Data.Rule.SetNull);
        }
    }

    public override void Down()
    {
        if (Schema.Table("diagnostic_checks").Exists() && Schema.Table("diagnostic_checks").Column("integration_id").Exists())
        {
            Delete.ForeignKey("fk_diag_check_integration").OnTable("diagnostic_checks");
            Delete.Column("integration_id").FromTable("diagnostic_checks");
        }

        if (Schema.Table("integration_runs").Exists() && Schema.Table("integration_runs").Column("triggered_by").Exists())
        {
            Delete.Column("triggered_by").FromTable("integration_runs");
        }

        if (Schema.Table("integrations").Exists())
        {
            if (Schema.Table("integrations").Column("health_check_expected_status_code").Exists())
                Delete.Column("health_check_expected_status_code").FromTable("integrations");
            if (Schema.Table("integrations").Column("health_check_timeout_seconds").Exists())
                Delete.Column("health_check_timeout_seconds").FromTable("integrations");
            if (Schema.Table("integrations").Column("health_check_method").Exists())
                Delete.Column("health_check_method").FromTable("integrations");
            if (Schema.Table("integrations").Column("health_check_url").Exists())
                Delete.Column("health_check_url").FromTable("integrations");
        }
    }
}
