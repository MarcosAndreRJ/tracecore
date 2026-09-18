using System;
using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091816, "Create diagnostic flows schema, checks, impacts and seed FLOW-LOGIN-ISSUES (Phase 9 - M07/BR-070-BR-076)")]
public class M20260918_16_CreateDiagnosticFlowsSchemaAndSeed : Migration
{
    public override void Up()
    {
        // 1. diagnostic_flows
        if (!Schema.Table("diagnostic_flows").Exists())
        {
            Create.Table("diagnostic_flows")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("code").AsString(64).NotNullable().Unique()
                .WithColumn("name").AsString(255).NotNullable()
                .WithColumn("description").AsString(int.MaxValue).Nullable()
                .WithColumn("entry_keywords").AsString(int.MaxValue).NotNullable()
                .WithColumn("status").AsString(32).NotNullable().WithDefaultValue("Active")
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_diag_flow_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable();
        }

        // 2. diagnostic_flow_hypotheses
        if (!Schema.Table("diagnostic_flow_hypotheses").Exists())
        {
            Create.Table("diagnostic_flow_hypotheses")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("flow_id").AsInt64().NotNullable().ForeignKey("fk_dfh_flow", "diagnostic_flows", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("title").AsString(255).NotNullable()
                .WithColumn("description").AsString(int.MaxValue).Nullable()
                .WithColumn("associated_component_id").AsInt64().Nullable().ForeignKey("fk_dfh_comp", "components", "id").OnDelete(System.Data.Rule.SetNull);
        }

        // 3. diagnostic_checks
        if (!Schema.Table("diagnostic_checks").Exists())
        {
            Create.Table("diagnostic_checks")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("flow_id").AsInt64().NotNullable().ForeignKey("fk_dc_flow", "diagnostic_flows", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("code").AsString(64).NotNullable()
                .WithColumn("title").AsString(255).NotNullable()
                .WithColumn("question_text").AsString(int.MaxValue).NotNullable()
                .WithColumn("check_type").AsString(64).NotNullable().WithDefaultValue("Question")
                .WithColumn("cost").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("risk_level").AsString(32).NotNullable().WithDefaultValue("Low")
                .WithColumn("skip_condition_field").AsString(64).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("uk_flow_check_code")
                .OnTable("diagnostic_checks")
                .OnColumn("flow_id").Ascending()
                .OnColumn("code").Ascending()
                .WithOptions().Unique();
        }

        // 4. diagnostic_check_options
        if (!Schema.Table("diagnostic_check_options").Exists())
        {
            Create.Table("diagnostic_check_options")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("check_id").AsInt64().NotNullable().ForeignKey("fk_dco_check", "diagnostic_checks", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("option_text").AsString(255).NotNullable()
                .WithColumn("order_no").AsInt32().NotNullable().WithDefaultValue(1);
        }

        // 5. diagnostic_check_impacts
        if (!Schema.Table("diagnostic_check_impacts").Exists())
        {
            Create.Table("diagnostic_check_impacts")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("check_option_id").AsInt64().NotNullable().ForeignKey("fk_dci_option", "diagnostic_check_options", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("flow_hypothesis_id").AsInt64().NotNullable().ForeignKey("fk_dci_hyp", "diagnostic_flow_hypotheses", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("impact_type").AsString(32).NotNullable()
                .WithColumn("weight").AsDecimal(5, 2).NotNullable().WithDefaultValue(1.0);
        }

        // 6. Permissão diagnostico.configurar
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'diagnostico.configurar', 'Permite criar e gerenciar fluxos de diagnóstico guiado, hipóteses candidatas e verificações'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'diagnostico.configurar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE p.code = 'diagnostico.configurar'
              AND r.name IN ('Admin', 'Admin Funcional')
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");

        // 7. Seed do fluxo 'FLOW-LOGIN-ISSUES' (doc 05 §6)
        Execute.Sql(@"
            INSERT INTO diagnostic_flows (code, name, description, entry_keywords, status, created_by, created_at)
            SELECT 'FLOW-LOGIN-ISSUES',
                   'Falha de Autenticação e Acesso ao Sistema',
                   'Fluxo adaptativo para triagem de usuários que não conseguem entrar no sistema (doc 05 §6 / BR-070)',
                   'não consigo entrar,login,autenticação,senha,acesso recusado,bloqueado,invalid credentials,credenciais',
                   'Active',
                   NULL,
                   UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM diagnostic_flows WHERE code = 'FLOW-LOGIN-ISSUES');

            -- Hipóteses do fluxo
            INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description)
            SELECT df.id, 'Bloqueio ou expiração de credencial de usuário', 'Conta expirada, senha incorreta repetida ou bloqueio no diretório/IAM.'
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_flow_hypotheses WHERE flow_id = df.id AND title = 'Bloqueio ou expiração de credencial de usuário');

            INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description)
            SELECT df.id, 'Indisponibilidade ou instabilidade do serviço de autenticação', 'Serviço de IAM, SSO ou gateway de autenticação fora do ar ou degradado.'
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_flow_hypotheses WHERE flow_id = df.id AND title = 'Indisponibilidade ou instabilidade do serviço de autenticação');

            INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description)
            SELECT df.id, 'Erro de configuração ou deploy recente no gateway de login', 'Alteração recente de certificados, CORS, redirect URIs ou segredos de cliente.'
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_flow_hypotheses WHERE flow_id = df.id AND title = 'Erro de configuração ou deploy recente no gateway de login');

            INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description)
            SELECT df.id, 'Bloqueio de conectividade ou rede local do cliente', 'Firewall, proxy corporativo do cliente ou falha de DNS impedindo handshake.'
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_flow_hypotheses WHERE flow_id = df.id AND title = 'Bloqueio de conectividade ou rede local do cliente');

            -- Pergunta 1: CHK-SCOPE
            INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, created_at)
            SELECT df.id, 'CHK-SCOPE', 'Escopo de Usuários Afetados', 'O problema afeta somente um usuário específico ou múltiplos/todos os usuários?', 'Question', 1, 'Low', UTC_TIMESTAMP()
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_checks WHERE flow_id = df.id AND code = 'CHK-SCOPE');

            -- Opções do CHK-SCOPE
            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Apenas um usuário isolado', 1
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-SCOPE'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Apenas um usuário isolado');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Múltiplos ou todos os usuários da organização', 2
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-SCOPE'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Múltiplos ou todos os usuários da organização');

            -- Impactos do CHK-SCOPE
            -- Opção 1 favorece H1 (credencial) e descarta H2 (indisponibilidade geral)
            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 1.50
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCOPE' AND dco.option_text = 'Apenas um usuário isolado'
              AND dfh.title = 'Bloqueio ou expiração de credencial de usuário'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Discards', 1.50
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCOPE' AND dco.option_text = 'Apenas um usuário isolado'
              AND dfh.title = 'Indisponibilidade ou instabilidade do serviço de autenticação'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            -- Opção 2 favorece H2 e H3 e descarta H1
            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCOPE' AND dco.option_text = 'Múltiplos ou todos os usuários da organização'
              AND dfh.title = 'Indisponibilidade ou instabilidade do serviço de autenticação'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Discards', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCOPE' AND dco.option_text = 'Múltiplos ou todos os usuários da organização'
              AND dfh.title = 'Bloqueio ou expiração de credencial de usuário'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            -- Pergunta 2: CHK-SCREEN
            INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, created_at)
            SELECT df.id, 'CHK-SCREEN', 'Carregamento da Tela de Login', 'A tela/página de login chega a carregar ou o navegador/app apresenta falha de conexão imediata?', 'Question', 1, 'Low', UTC_TIMESTAMP()
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_checks WHERE flow_id = df.id AND code = 'CHK-SCREEN');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Sim, a tela de login carrega e permite digitar', 1
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-SCREEN'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Sim, a tela de login carrega e permite digitar');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Não, tela em branco ou timeout/erro de rede do navegador', 2
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-SCREEN'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Não, tela em branco ou timeout/erro de rede do navegador');

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Discards', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCREEN' AND dco.option_text = 'Sim, a tela de login carrega e permite digitar'
              AND dfh.title = 'Bloqueio de conectividade ou rede local do cliente'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-SCREEN' AND dco.option_text = 'Não, tela em branco ou timeout/erro de rede do navegador'
              AND dfh.title = 'Bloqueio de conectividade ou rede local do cliente'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            -- Pergunta 3: CHK-ERRMSG
            INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, created_at)
            SELECT df.id, 'CHK-ERRMSG', 'Mensagem de Erro Apresentada', 'Qual mensagem de erro exata é exibida após a tentativa de login?', 'Question', 1, 'Low', UTC_TIMESTAMP()
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_checks WHERE flow_id = df.id AND code = 'CHK-ERRMSG');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Usuário/senha inválidos ou conta temporariamente bloqueada', 1
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-ERRMSG'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Usuário/senha inválidos ou conta temporariamente bloqueada');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Erro 500 / 502 / 504 / Falha interna de comunicação', 2
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-ERRMSG'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Erro 500 / 502 / 504 / Falha interna de comunicação');

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-ERRMSG' AND dco.option_text = 'Usuário/senha inválidos ou conta temporariamente bloqueada'
              AND dfh.title = 'Bloqueio ou expiração de credencial de usuário'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-ERRMSG' AND dco.option_text = 'Erro 500 / 502 / 504 / Falha interna de comunicação'
              AND dfh.title = 'Indisponibilidade ou instabilidade do serviço de autenticação'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            -- Pergunta 4: CHK-RECENT-CHANGE
            INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, created_at)
            SELECT df.id, 'CHK-RECENT-CHANGE', 'Deploy ou Mudança Recente', 'Houve publicação de versão, manutenção ou alteração de configurações no ecossistema nas últimas 24h?', 'Question', 1, 'Low', UTC_TIMESTAMP()
            FROM diagnostic_flows df WHERE df.code = 'FLOW-LOGIN-ISSUES'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_checks WHERE flow_id = df.id AND code = 'CHK-RECENT-CHANGE');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Sim, houve deploy recente ou alteração de infraestrutura', 1
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-RECENT-CHANGE'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Sim, houve deploy recente ou alteração de infraestrutura');

            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            SELECT dc.id, 'Não, ambiente totalmente estável sem alterações recentes', 2
            FROM diagnostic_checks dc
            INNER JOIN diagnostic_flows df ON df.id = dc.flow_id
            WHERE df.code = 'FLOW-LOGIN-ISSUES' AND dc.code = 'CHK-RECENT-CHANGE'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_options WHERE check_id = dc.id AND option_text = 'Não, ambiente totalmente estável sem alterações recentes');

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Favors', 2.00
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-RECENT-CHANGE' AND dco.option_text = 'Sim, houve deploy recente ou alteração de infraestrutura'
              AND dfh.title = 'Erro de configuração ou deploy recente no gateway de login'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);

            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            SELECT dco.id, dfh.id, 'Discards', 1.50
            FROM diagnostic_check_options dco
            INNER JOIN diagnostic_checks dc ON dc.id = dco.check_id
            INNER JOIN diagnostic_flow_hypotheses dfh ON dfh.flow_id = dc.flow_id
            WHERE dc.code = 'CHK-RECENT-CHANGE' AND dco.option_text = 'Não, ambiente totalmente estável sem alterações recentes'
              AND dfh.title = 'Erro de configuração ou deploy recente no gateway de login'
              AND NOT EXISTS (SELECT 1 FROM diagnostic_check_impacts WHERE check_option_id = dco.id AND flow_hypothesis_id = dfh.id);
        ");
    }

    public override void Down()
    {
        if (Schema.Table("diagnostic_check_impacts").Exists()) Delete.Table("diagnostic_check_impacts");
        if (Schema.Table("diagnostic_check_options").Exists()) Delete.Table("diagnostic_check_options");
        if (Schema.Table("diagnostic_checks").Exists()) Delete.Table("diagnostic_checks");
        if (Schema.Table("diagnostic_flow_hypotheses").Exists()) Delete.Table("diagnostic_flow_hypotheses");
        if (Schema.Table("diagnostic_flows").Exists()) Delete.Table("diagnostic_flows");
    }
}
