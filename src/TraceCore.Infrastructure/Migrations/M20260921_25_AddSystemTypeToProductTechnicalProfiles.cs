using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092125, "Add system_type to product_technical_profiles (Fase 04 - Ajuste Ecossistema)")]
public class M20260921_25_AddSystemTypeToProductTechnicalProfiles : Migration
{
    public override void Up()
    {
        // Tipo do sistema (Desktop, Web, Mobile, Api, Servico, SaaS, Outro).
        // String aberta controlada em Application (ProductTechnicalProfile.ValidSystemTypes):
        // mesmo padrão de integrations.responsibility/hosting_location/direction.
        if (!Schema.Table("product_technical_profiles").Column("system_type").Exists())
            Alter.Table("product_technical_profiles").AddColumn("system_type").AsString(50).Nullable();
    }

    public override void Down()
    {
        if (Schema.Table("product_technical_profiles").Column("system_type").Exists())
            Delete.Column("system_type").FromTable("product_technical_profiles");
    }
}