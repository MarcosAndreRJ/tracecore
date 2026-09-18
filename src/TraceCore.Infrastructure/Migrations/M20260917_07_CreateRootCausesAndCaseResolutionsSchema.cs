using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091707, "Create root causes and case resolutions schema for structured case closure (Phase 5)")]
public class M20260917_07_CreateRootCausesAndCaseResolutionsSchema : Migration
{
    public override void Up()
    {
        // 1. root_causes
        Create.Table("root_causes")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("code").AsString(50).Nullable().Unique("uk_root_cause_code")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("category").AsString(100).Nullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // 2. case_resolutions (BR-027, BR-028, Bloco 5.1)
        Create.Table("case_resolutions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_id").AsInt64().NotNullable().Unique("uk_case_resolution_case").ForeignKey("fk_cres_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("resolution_summary").AsString(16777215).NotNullable()
            .WithColumn("validation_summary").AsString(16777215).NotNullable()
            .WithColumn("root_cause_id").AsInt64().Nullable().ForeignKey("fk_cres_root_cause", "root_causes", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("root_cause_confirmed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("responsible_department_id").AsInt64().Nullable().ForeignKey("fk_cres_dept", "departments", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("resolution_type").AsString(50).NotNullable().WithDefaultValue("Definitive")
            .WithColumn("recurrence_risk").AsString(30).NotNullable().WithDefaultValue("Low")
            .WithColumn("recurrence_notes").AsString(int.MaxValue).Nullable()
            .WithColumn("preventive_actions").AsString(16777215).Nullable()
            .WithColumn("effort_minutes").AsInt32().Nullable()
            .WithColumn("resolved_by").AsInt64().NotNullable().ForeignKey("fk_cres_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("resolved_at").AsDateTime().NotNullable();

        Create.Index("ix_case_resolutions_root_cause")
            .OnTable("case_resolutions")
            .OnColumn("root_cause_id");

        // 3. Atribuição de permissão 'caso.encerrar' aos papéis que têm capacidade de encerramento
        // Usuário Técnico, Especialista, Revisor, Gestor, Admin Funcional
        Execute.Sql(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name IN ('Usuário Técnico', 'Especialista', 'Revisor', 'Gestor', 'Admin Funcional')
              AND p.code = 'caso.encerrar'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");

        // 4. Seed de causas raízes comuns para acelerar taxonomia reutilizável inicial
        Insert.IntoTable("root_causes")
            .Row(new { code = "RC-POOL-STARV", name = "Esgotamento de Threads por Deadlock / Timeout no Pool", category = "Software / Concorrência", description = "Saturação de pool de conexões sob degradação em cascata de chamadas síncronas.", created_at = System.DateTime.UtcNow })
            .Row(new { code = "RC-KAFKA-REBAL", name = "Rebalanceamento Indesejado de Consumer Group", category = "Infraestrutura / Mensageria", description = "max.poll.interval.ms excedido durante processamento batch intensivo.", created_at = System.DateTime.UtcNow })
            .Row(new { code = "RC-CERT-EXPIRED", name = "Certificado mTLS ou CA Intermediária Expirada", category = "Segurança / Infraestrutura", description = "Rejeição de handshake TLS por cadeia de certificação expirada.", created_at = System.DateTime.UtcNow })
            .Row(new { code = "RC-MEM-LEAK", name = "Vazamento de Memória por Desalocação Incompleta de Buffers", category = "Software / Runtime", description = "Acúmulo progressivo de heap em workers assíncronos.", created_at = System.DateTime.UtcNow });
    }

    public override void Down()
    {
        Delete.Table("case_resolutions");
        Delete.Table("root_causes");
    }
}
