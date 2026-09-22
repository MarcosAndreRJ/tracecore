using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092127, "Fase 3 (Abertura de Caso em Etapas): tags manuais em casos, reaproveitando a taxonomia 'tags' da Base de Conhecimento, como sinal adicional (peso menor) no motor de casos semelhantes")]
public class M20260921_27_AddCaseTagsSchema : Migration
{
    public override void Up()
    {
        if (!Schema.Table("case_tags").Exists())
        {
            Create.Table("case_tags")
                .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_casetag_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("tag_id").AsInt64().NotNullable().ForeignKey("fk_casetag_tag", "tags", "id").OnDelete(System.Data.Rule.Cascade);

            Create.PrimaryKey("pk_case_tags")
                .OnTable("case_tags")
                .Columns("case_id", "tag_id");
        }
    }

    public override void Down()
    {
        if (Schema.Table("case_tags").Exists())
        {
            Delete.Table("case_tags");
        }
    }
}
