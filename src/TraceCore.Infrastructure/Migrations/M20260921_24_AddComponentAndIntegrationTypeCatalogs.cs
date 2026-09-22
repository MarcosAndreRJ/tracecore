using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092124, "Create component_types and integration_types catalogs, add responsibility/hosting_location/direction to integrations and seed (Fase 01 - Ajuste Ecossistema)")]
public class M20260921_24_AddComponentAndIntegrationTypeCatalogs : Migration
{
    public override void Up()
    {
        // 1. component_types — catálogo administrável de tipos de componente.
        // ComponentEntity.ComponentType continua sendo string aberta (sem FK): este
        // catálogo alimenta a UI (Fase 03) sem quebrar valores já gravados.
        if (!Schema.Table("component_types").Exists())
        {
            Create.Table("component_types")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("code").AsString(64).NotNullable().Unique("uk_component_types_code")
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("created_at").AsDateTime().NotNullable();
        }

        // 2. integration_types — catálogo administrável de tipos de integração.
        // Integration.IntegrationType continua sendo string aberta (sem FK) pela
        // mesma razão: compatibilidade com registros existentes.
        if (!Schema.Table("integration_types").Exists())
        {
            Create.Table("integration_types")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("code").AsString(64).NotNullable().Unique("uk_integration_types_code")
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("created_at").AsDateTime().NotNullable();
        }

        // 3. integrations — campos estruturais do mundo real (strings controladas,
        // opcionais, sem tabela própria — poucos valores estáveis).
        if (!Schema.Table("integrations").Column("responsibility").Exists())
            Alter.Table("integrations").AddColumn("responsibility").AsString(50).Nullable();
        if (!Schema.Table("integrations").Column("hosting_location").Exists())
            Alter.Table("integrations").AddColumn("hosting_location").AsString(50).Nullable();
        if (!Schema.Table("integrations").Column("direction").Exists())
            Alter.Table("integrations").AddColumn("direction").AsString(50).Nullable();

        SeedComponentTypes();
        SeedIntegrationTypes();
    }

    private void SeedComponentTypes()
    {
        // Idempotente (INSERT ... SELECT ... WHERE NOT EXISTS): valores já usados na
        // UI (Service, Frontend, ...) + 'Module' (já em uso na base) + exemplos do
        // mundo real. Code é o token gravado em components.component_type.
        Execute.Sql(@"
            INSERT INTO component_types (code, name, is_active, created_at)
            SELECT v.code, v.name, 1, UTC_TIMESTAMP()
            FROM (
                SELECT 'Service' AS code, 'Serviço' AS name
                UNION ALL SELECT 'Frontend', 'Frontend / Interface'
                UNION ALL SELECT 'Database', 'Banco de Dados'
                UNION ALL SELECT 'Worker', 'Worker / Processamento'
                UNION ALL SELECT 'Gateway', 'Gateway'
                UNION ALL SELECT 'Integration', 'Integração / Conector'
                UNION ALL SELECT 'Module', 'Módulo'
                UNION ALL SELECT 'DesktopModule', 'Módulo Desktop'
                UNION ALL SELECT 'Api', 'API'
                UNION ALL SELECT 'WindowsService', 'Serviço Windows'
                UNION ALL SELECT 'MobileApp', 'Aplicativo Mobile'
                UNION ALL SELECT 'IntegrationAdapter', 'Adaptador de Integração'
                UNION ALL SELECT 'Infrastructure', 'Infraestrutura'
                UNION ALL SELECT 'Other', 'Outro'
            ) v
            WHERE NOT EXISTS (SELECT 1 FROM component_types ct WHERE ct.code = v.code);
        ");
    }

    private void SeedIntegrationTypes()
    {
        // Idempotente: valores já usados na UI/base (Sap, Ticketing, ...) + exemplos
        // do mundo real (REST API, SAP IDoc, EDI, ...). Code é o token gravado em
        // integrations.integration_type.
        Execute.Sql(@"
            INSERT INTO integration_types (code, name, is_active, created_at)
            SELECT v.code, v.name, 1, UTC_TIMESTAMP()
            FROM (
                SELECT 'Sap' AS code, 'SAP / ERP' AS name
                UNION ALL SELECT 'Ticketing', 'Sistema de Chamados'
                UNION ALL SELECT 'Monitoring', 'Monitoração'
                UNION ALL SELECT 'Telemetry', 'Telemetria'
                UNION ALL SELECT 'Directory', 'Diretório / IAM'
                UNION ALL SELECT 'Notification', 'Notificação'
                UNION ALL SELECT 'Repository', 'Repositório / CI-CD'
                UNION ALL SELECT 'Other', 'Outro'
                UNION ALL SELECT 'RestApi', 'REST API'
                UNION ALL SELECT 'Soap', 'SOAP'
                UNION ALL SELECT 'Webhook', 'Webhook'
                UNION ALL SELECT 'Sftp', 'SFTP'
                UNION ALL SELECT 'File', 'Arquivo'
                UNION ALL SELECT 'Database', 'Banco de Dados'
                UNION ALL SELECT 'MessageQueue', 'Fila / Mensageria'
                UNION ALL SELECT 'SapRfc', 'SAP RFC'
                UNION ALL SELECT 'SapIdoc', 'SAP IDoc'
                UNION ALL SELECT 'Edi', 'EDI'
            ) v
            WHERE NOT EXISTS (SELECT 1 FROM integration_types it WHERE it.code = v.code);
        ");
    }

    public override void Down()
    {
        Delete.Column("direction").FromTable("integrations");
        Delete.Column("hosting_location").FromTable("integrations");
        Delete.Column("responsibility").FromTable("integrations");
        if (Schema.Table("integration_types").Exists()) Delete.Table("integration_types");
        if (Schema.Table("component_types").Exists()) Delete.Table("component_types");
    }
}