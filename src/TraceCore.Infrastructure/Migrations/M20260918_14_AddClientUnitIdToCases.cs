using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091814, "Add client_unit_id to cases (Phase 7.A - Bloco 7.A.6)")]
public class M20260918_14_AddClientUnitIdToCases : Migration
{
    public override void Up()
    {
        if (Schema.Table("cases").Exists() && !Schema.Table("cases").Column("client_unit_id").Exists())
        {
            Alter.Table("cases")
                .AddColumn("client_unit_id").AsInt64().Nullable().ForeignKey("fk_cases_client_unit", "client_units", "id").OnDelete(System.Data.Rule.SetNull);

            Create.Index("ix_cases_client_unit")
                .OnTable("cases")
                .OnColumn("client_unit_id");
        }
    }

    public override void Down()
    {
        if (Schema.Table("cases").Exists() && Schema.Table("cases").Column("client_unit_id").Exists())
        {
            Delete.ForeignKey("fk_cases_client_unit").OnTable("cases");
            Delete.Index("ix_cases_client_unit").OnTable("cases");
            Delete.Column("client_unit_id").FromTable("cases");
        }
    }
}
