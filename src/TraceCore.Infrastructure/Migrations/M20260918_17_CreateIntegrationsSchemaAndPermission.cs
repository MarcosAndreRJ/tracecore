using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091817, "Create integrations schema, integration_runs log and seed integracao.gerenciar (M10 - Integrações)")]
public class M20260918_17_CreateIntegrationsSchemaAndPermission : Migration
{
    public override void Up()
    {
        // 1. integrations — registro/catálogo administrativo (NÃO é conector real).
        // ADR-P005 (sistema de chamados) e ADR-P010 (implantação) seguem em aberto:
        // sem credencial e sem sistema externo disponível, esta fase constrói apenas
        // o registro. Estado nunca muda por checagem automática — só por ação manual.
        if (!Schema.Table("integrations").Exists())
        {
            Create.Table("integrations")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("code").AsString(64).NotNullable().Unique()
                .WithColumn("name").AsString(255).NotNullable()
                .WithColumn("integration_type").AsString(64).NotNullable() // Catálogo aberto: Ticketing, Sap, Monitoring, Telemetry, Directory, Notification, Repository, Other
                .WithColumn("target_system_description").AsString(int.MaxValue).Nullable()
                .WithColumn("status").AsString(32).NotNullable().WithDefaultValue("Configured") // Configured, Active, Inactive, Error — manual apenas
                .WithColumn("owner_department_id").AsInt64().Nullable().ForeignKey("fk_intg_dept", "departments", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("contract_notes").AsString(int.MaxValue).Nullable() // Isolamento e contrato próprio por conector (doc 03 §M10)
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_intg_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("ix_integrations_type").OnTable("integrations").OnColumn("integration_type");
            Create.Index("ix_integrations_status").OnTable("integrations").OnColumn("status");
        }

        // 2. integration_runs — LOG manual registrado por um humano, não resultado
        // de execução automática (não há conector real rodando).
        if (!Schema.Table("integration_runs").Exists())
        {
            Create.Table("integration_runs")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integration_id").AsInt64().NotNullable().ForeignKey("fk_intg_run", "integrations", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("started_at").AsDateTime().NotNullable()
                .WithColumn("finished_at").AsDateTime().Nullable()
                .WithColumn("status").AsString(32).NotNullable() // Success, Failed, Partial
                .WithColumn("records_processed").AsInt64().Nullable()
                .WithColumn("error_message").AsString(int.MaxValue).Nullable()
                .WithColumn("recorded_by").AsInt64().Nullable().ForeignKey("fk_intg_run_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("recorded_at").AsDateTime().NotNullable();

            Create.Index("ix_integration_runs_integration").OnTable("integration_runs").OnColumn("integration_id");
        }

        // 3. Permissão integracao.gerenciar
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'integracao.gerenciar', 'Permite cadastrar integrações, alterar status e registrar execuções manuais no catálogo de integrações'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'integracao.gerenciar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE p.code = 'integracao.gerenciar'
              AND r.name IN ('Admin', 'Admin Funcional')
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");

        // 4. Seed (dado, não código): integrações citadas no M10 e já mencionadas no
        // catálogo técnico como is_external (SAP, sistema de chamados). Entram como
        // catálogo 'Configured' sem execução — claramente NÃO é conexão ativa.
        Execute.Sql(@"
            INSERT INTO integrations
                (code, name, integration_type, target_system_description, status, owner_department_id, contract_notes, created_by, created_at)
            SELECT
                'INT-SAP',
                'Integração SAP',
                'Sap',
                'ERP SAP — integração de dados mestre e financeiro',
                'Configured',
                (SELECT id FROM departments WHERE name = 'Integrações'),
                'Registro de catálogo (M10): conector ainda não construído. Isolamento e contrato próprio por definir após ADR-P010. Nenhuma conexão ativa.',
                NULL,
                UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM integrations WHERE code = 'INT-SAP');

            INSERT INTO integrations
                (code, name, integration_type, target_system_description, status, owner_department_id, contract_notes, created_by, created_at)
            SELECT
                'INT-TICKET',
                'Sistema de Chamados Corporativo',
                'Ticketing',
                'Plataforma corporativa de chamados/suporte',
                'Configured',
                (SELECT id FROM departments WHERE name = 'Suporte'),
                'Registro de catálogo (M10): fonte e sincronização ainda sem decisão (ADR-P005 em aberto). Conector não construído. Nenhuma conexão ativa.',
                NULL,
                UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM integrations WHERE code = 'INT-TICKET');
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DELETE FROM integration_runs;
            DELETE FROM integrations;

            DELETE rp FROM role_permissions rp
            INNER JOIN permissions p ON p.id = rp.permission_id
            WHERE p.code = 'integracao.gerenciar';

            DELETE FROM permissions WHERE code = 'integracao.gerenciar';
        ");

        if (Schema.Table("integration_runs").Exists()) Delete.Table("integration_runs");
        if (Schema.Table("integrations").Exists()) Delete.Table("integrations");
    }
}