using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091706, "Create diagnostic sessions, case hypotheses and diagnostic steps schema with permissions")]
public class M20260917_06_CreateInvestigationAndDiagnosticSchema : Migration
{
    public override void Up()
    {
        // 1. diagnostic_sessions
        Create.Table("diagnostic_sessions")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_ds_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Open")
            .WithColumn("started_at").AsDateTime().NotNullable()
            .WithColumn("started_by").AsInt64().NotNullable().ForeignKey("fk_ds_user", "users", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ended_at").AsDateTime().Nullable();

        Create.Index("ix_diag_sessions_case")
            .OnTable("diagnostic_sessions")
            .OnColumn("case_id").Ascending()
            .OnColumn("started_at").Descending();

        // 2. case_hypotheses (BR-023, BR-024)
        Create.Table("case_hypotheses")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_hyp_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("component_id").AsInt64().Nullable().ForeignKey("fk_hyp_component", "components", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("title").AsString(300).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("status").AsString(30).NotNullable().WithDefaultValue("Proposed")
            .WithColumn("source_type").AsString(30).NotNullable().WithDefaultValue("Human")
            .WithColumn("justification").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_hyp_created_by", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_case_hypotheses_case_status")
            .OnTable("case_hypotheses")
            .OnColumn("case_id").Ascending()
            .OnColumn("status").Ascending();

        // 3. diagnostic_steps (BR-025, BR-026)
        Create.Table("diagnostic_steps")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("diagnostic_session_id").AsInt64().NotNullable().ForeignKey("fk_dstep_session", "diagnostic_sessions", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("sequence_no").AsInt32().NotNullable()
            .WithColumn("step_type").AsString(40).NotNullable().WithDefaultValue("Verification")
            .WithColumn("hypothesis_id").AsInt64().Nullable().ForeignKey("fk_dstep_hyp", "case_hypotheses", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("title").AsString(300).NotNullable()
            .WithColumn("objective").AsString(int.MaxValue).Nullable()
            .WithColumn("instruction").AsString(16777215).Nullable()
            .WithColumn("input_evidence_summary").AsString(int.MaxValue).Nullable()
            .WithColumn("result_summary").AsString(16777215).NotNullable()
            .WithColumn("outcome").AsString(40).NotNullable()
            .WithColumn("risk_level").AsString(30).NotNullable().WithDefaultValue("Low")
            .WithColumn("duration_seconds").AsInt32().Nullable()
            .WithColumn("performed_by").AsInt64().Nullable().ForeignKey("fk_dstep_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
            .WithColumn("performed_at").AsDateTime().NotNullable()
            .WithColumn("metadata_json").AsString(int.MaxValue).Nullable();

        Create.UniqueConstraint("uk_diag_step_sequence")
            .OnTable("diagnostic_steps")
            .Columns("diagnostic_session_id", "sequence_no");

        Create.Index("ix_diag_steps_hypothesis")
            .OnTable("diagnostic_steps")
            .OnColumn("hypothesis_id");

        // 4. Nova Permissão: caso.diagnosticar (25_MATRIZ_PERFIS_PERMISSOES.md)
        Insert.IntoTable("permissions")
            .Row(new
            {
                code = "caso.diagnosticar",
                description = "Diagnosticar caso, registrar hipóteses, testes e avaliações"
            });

        // Vínculo aos perfis que têm capacidade de diagnóstico:
        // Usuário Técnico (✓), Especialista (✓), Revisor (✓), Gestor (✓), Admin Funcional (✓)
        Execute.Sql(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name IN ('Usuário Técnico', 'Especialista', 'Revisor', 'Gestor', 'Admin Funcional')
              AND p.code = 'caso.diagnosticar';
        ");
    }

    public override void Down()
    {
        Execute.Sql("DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code = 'caso.diagnosticar');");
        Execute.Sql("DELETE FROM permissions WHERE code = 'caso.diagnosticar';");

        Delete.Table("diagnostic_steps");
        Delete.Table("case_hypotheses");
        Delete.Table("diagnostic_sessions");
    }
}
