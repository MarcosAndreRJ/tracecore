using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091812, "CaseIteration entity, backfill, and reopening support (Phase 7.A - Bloco 7.A.3)")]
public class M20260918_12_CaseIterationAndReopenSupport : Migration
{
    public override void Up()
    {
        // 1. Criação da tabela case_iterations
        if (!Schema.Table("case_iterations").Exists())
        {
            Create.Table("case_iterations")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("case_id").AsInt64().NotNullable().ForeignKey("fk_ci_case", "cases", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("sequence_number").AsInt32().NotNullable()
                .WithColumn("opened_at").AsDateTime().NotNullable()
                .WithColumn("opened_by").AsInt64().NotNullable().ForeignKey("fk_ci_user", "users", "id")
                .WithColumn("reason").AsString(1000).Nullable()
                .WithColumn("closed_at").AsDateTime().Nullable()
                .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Open");

            Create.UniqueConstraint("uk_case_iterations_seq")
                .OnTable("case_iterations")
                .Columns("case_id", "sequence_number");

            Create.Index("ix_ci_case_status")
                .OnTable("case_iterations")
                .OnColumn("case_id").Ascending()
                .OnColumn("status").Ascending();
        }

        // 2. Adicionar case_iteration_id como nullable nas 4 tabelas
        if (!Schema.Table("case_hypotheses").Column("case_iteration_id").Exists())
        {
            Alter.Table("case_hypotheses")
                .AddColumn("case_iteration_id").AsInt64().Nullable();
        }

        if (!Schema.Table("diagnostic_sessions").Column("case_iteration_id").Exists())
        {
            Alter.Table("diagnostic_sessions")
                .AddColumn("case_iteration_id").AsInt64().Nullable();
        }

        if (!Schema.Table("case_evidences").Column("case_iteration_id").Exists())
        {
            Alter.Table("case_evidences")
                .AddColumn("case_iteration_id").AsInt64().Nullable();
        }

        if (!Schema.Table("case_resolutions").Column("case_iteration_id").Exists())
        {
            Alter.Table("case_resolutions")
                .AddColumn("case_iteration_id").AsInt64().Nullable();
        }

        // 3. Backfill dos dados existentes
        Execute.Sql(@"
            -- 3.1 Criar Iteração 1 para cada caso já existente
            INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
            SELECT 
                c.id, 
                1, 
                c.opened_at, 
                COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)), 
                'Iteração inicial (backfill)', 
                c.resolved_at, 
                IF(c.status = 'Resolved', 'Resolved', 'Open')
            FROM cases c
            WHERE NOT EXISTS (
                SELECT 1 FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1
            );

            -- 3.2 Associar hipóteses existentes à Iteração 1
            UPDATE case_hypotheses ch
            INNER JOIN case_iterations ci ON ci.case_id = ch.case_id AND ci.sequence_number = 1
            SET ch.case_iteration_id = ci.id
            WHERE ch.case_iteration_id IS NULL;

            -- 3.3 Associar sessões de diagnóstico existentes à Iteração 1
            UPDATE diagnostic_sessions ds
            INNER JOIN case_iterations ci ON ci.case_id = ds.case_id AND ci.sequence_number = 1
            SET ds.case_iteration_id = ci.id
            WHERE ds.case_iteration_id IS NULL;

            -- 3.4 Associar evidências existentes à Iteração 1
            UPDATE case_evidences ce
            INNER JOIN case_iterations ci ON ci.case_id = ce.case_id AND ci.sequence_number = 1
            SET ce.case_iteration_id = ci.id
            WHERE ce.case_iteration_id IS NULL;

            -- 3.5 Associar resoluções existentes à Iteração 1
            UPDATE case_resolutions cr
            INNER JOIN case_iterations ci ON ci.case_id = cr.case_id AND ci.sequence_number = 1
            SET cr.case_iteration_id = ci.id
            WHERE cr.case_iteration_id IS NULL;
        ");

        // 4. Tornar case_iteration_id NOT NULL e adicionar FKs
        // DDL no MySQL/MariaDB é não-transacional: se a migração falhar no meio e for
        // reexecutada (FluentMigrator não grava a versão de migrações que lançam exceção),
        // os passos já aplicados precisam ser idempotentes. Por isso os FKs só são
        // criados se ainda não existirem.
        Alter.Table("case_hypotheses")
            .AlterColumn("case_iteration_id").AsInt64().NotNullable();
        Alter.Table("diagnostic_sessions")
            .AlterColumn("case_iteration_id").AsInt64().NotNullable();
        Alter.Table("case_evidences")
            .AlterColumn("case_iteration_id").AsInt64().NotNullable();
        Alter.Table("case_resolutions")
            .AlterColumn("case_iteration_id").AsInt64().NotNullable();

        AddIterationForeignKeyIfMissing("case_hypotheses", "fk_ch_iteration");
        AddIterationForeignKeyIfMissing("diagnostic_sessions", "fk_ds_iteration");
        AddIterationForeignKeyIfMissing("case_evidences", "fk_ce_iteration");
        AddIterationForeignKeyIfMissing("case_resolutions", "fk_cres_iteration");

        // 5. case_resolutions: UNIQUE passa de case_id para case_iteration_id
        // Em M20260917_07 o case_id foi criado como UNIQUE ("uk_case_resolution_case")
        // e esse mesmo índice único é o índice de suporte da FK fk_cres_case. O
        // MySQL/MariaDB não permite derrubar um índice que está em uso por uma FK
        // ("Cannot drop index ... needed in a foreign key constraint"). A ordem
        // correta é: derrubar a FK, derrubar o índice único, criar um índice comum de
        // suporte em case_id e recriar a FK (que passa a usar o novo índice). Só então
        // o novo UNIQUE em case_iteration_id pode ser criado.
        if (Schema.Table("case_resolutions").Constraint("fk_cres_case").Exists())
        {
            Delete.ForeignKey("fk_cres_case").OnTable("case_resolutions");
        }

        if (Schema.Table("case_resolutions").Index("uk_case_resolution_case").Exists())
        {
            Delete.Index("uk_case_resolution_case").OnTable("case_resolutions");
        }

        if (!Schema.Table("case_resolutions").Index("ix_case_resolutions_case").Exists())
        {
            Create.Index("ix_case_resolutions_case")
                .OnTable("case_resolutions")
                .OnColumn("case_id");
        }

        if (!Schema.Table("case_resolutions").Constraint("fk_cres_case").Exists())
        {
            Create.ForeignKey("fk_cres_case")
                .FromTable("case_resolutions").ForeignColumn("case_id")
                .ToTable("cases").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        if (!Schema.Table("case_resolutions").Constraint("uk_case_resolution_iteration").Exists())
        {
            Create.UniqueConstraint("uk_case_resolution_iteration")
                .OnTable("case_resolutions")
                .Column("case_iteration_id");
        }
    }

    private void AddIterationForeignKeyIfMissing(string tableName, string constraintName)
    {
        if (Schema.Table(tableName).Constraint(constraintName).Exists())
        {
            return;
        }

        Create.ForeignKey(constraintName)
            .FromTable(tableName).ForeignColumn("case_iteration_id")
            .ToTable("case_iterations").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        foreach (var (table, fk) in new[]
                 {
                     ("case_hypotheses", "fk_ch_iteration"),
                     ("diagnostic_sessions", "fk_ds_iteration"),
                     ("case_evidences", "fk_ce_iteration"),
                     ("case_resolutions", "fk_cres_iteration")
                 })
        {
            if (Schema.Table(table).Constraint(fk).Exists())
            {
                Delete.ForeignKey(fk).OnTable(table);
            }
        }

        if (Schema.Table("case_iterations").Exists())
        {
            Delete.Table("case_iterations");
        }
    }
}
