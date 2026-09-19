using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091819, "Fase 13 (M12 - RAG e IA): provedores de LLM/embedding configuráveis, interações IA, fontes e feedback; colunas de embedding no conteúdo pesquisável")]
public class M20260918_19_CreateAiAndRagSchema : Migration
{
    public override void Up()
    {
        // 1. llm_provider_configs — provedor/modelo configurável por propósito (Generation/Embedding).
        // ADR-P006/P007 fechados nesta fase: embeddings em MySQL + similaridade por cosseno sobre
        // conjunto pré-filtrado (busca híbrida). A API key NUNCA fica no banco — mora em configuração
        // de ambiente (User Secrets em Development; env/secret store em Staging/Produção).
        if (!Schema.Table("llm_provider_configs").Exists())
        {
            Create.Table("llm_provider_configs")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("purpose").AsString(32).NotNullable() // Generation, Embedding
                .WithColumn("provider_code").AsString(100).NotNullable() // Catálogo aberto: Anthropic, OpenAI
                .WithColumn("model_name").AsString(200).NotNullable()
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_llmcfg_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_llmcfg_upd_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("ix_llm_configs_purpose_active")
                .OnTable("llm_provider_configs")
                .OnColumn("purpose").Ascending()
                .OnColumn("is_active").Ascending();
        }

        // 2. searchable_content_entries — colunas de embedding (ADR-P006: vetor JSON em MySQL, sem engine vetorial dedicado)
        if (Schema.Table("searchable_content_entries").Exists() && !Schema.Table("searchable_content_entries").Column("embedding_vector").Exists())
        {
            Alter.Table("searchable_content_entries")
                .AddColumn("embedding_vector").AsString(int.MaxValue).Nullable() // JSON: array de floats
                .AddColumn("embedding_model").AsString(200).Nullable()
                .AddColumn("embedding_generated_at").AsDateTime().Nullable();
        }

        // 3. ai_interactions — pergunta/resposta oficiais (BR-084 base)
        if (!Schema.Table("ai_interactions").Exists())
        {
            Create.Table("ai_interactions")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("user_id").AsInt64().Nullable().ForeignKey("fk_aiint_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("query_text").AsString(int.MaxValue).NotNullable()
                .WithColumn("response_text").AsString(int.MaxValue).NotNullable()
                .WithColumn("provider_code").AsString(100).Nullable() // Qual provedor respondeu (auditoria)
                .WithColumn("model_name").AsString(200).Nullable()
                .WithColumn("tokens_used").AsInt64().Nullable()
                .WithColumn("latency_ms").AsInt64().Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("ix_ai_interactions_user_created")
                .OnTable("ai_interactions")
                .OnColumn("user_id").Ascending()
                .OnColumn("created_at").Descending();
        }

        // 4. ai_sources — fontes recuperadas da base oficial (BR-084: citar e permitir abrir a origem)
        if (!Schema.Table("ai_sources").Exists())
        {
            Create.Table("ai_sources")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ai_interaction_id").AsInt64().NotNullable().ForeignKey("fk_aisrc_int", "ai_interactions", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("searchable_content_entry_id").AsInt64().NotNullable().ForeignKey("fk_aisrc_entry", "searchable_content_entries", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("rank").AsInt32().NotNullable()
                .WithColumn("similarity_score").AsDecimal(8, 5).NotNullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("ix_ai_sources_interaction").OnTable("ai_sources").OnColumn("ai_interaction_id");
        }

        // 5. ai_interaction_feedback — avaliação armazenada que NUNCA altera conhecimento/regras (BR-086)
        if (!Schema.Table("ai_interaction_feedback").Exists())
        {
            Create.Table("ai_interaction_feedback")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ai_interaction_id").AsInt64().NotNullable().ForeignKey("fk_aifb_int", "ai_interactions", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("user_id").AsInt64().Nullable().ForeignKey("fk_aifb_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("useful").AsBoolean().NotNullable()
                .WithColumn("comment").AsString(int.MaxValue).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable();

            Create.Index("ix_ai_feedback_interaction").OnTable("ai_interaction_feedback").OnColumn("ai_interaction_id");
        }

        // 6. Permissões novas do módulo
        Execute.Sql(@"
            INSERT INTO permissions (code, description)
            SELECT 'ia.usar', 'Permite usar o copiloto de IA (perguntas assistidas sobre a base de conhecimento)'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'ia.usar');

            INSERT INTO permissions (code, description)
            SELECT 'configuracao.gerenciar', 'Permite gerenciar configurações da plataforma, incluindo provedores de IA e modelos'
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'configuracao.gerenciar');

            -- ia.usar: papéis operacionais que consultam a base de conhecimento. Admin Segurança fica fora
            -- (escopo restrito a gestão de acessos/segurança).
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE p.code = 'ia.usar'
              AND r.name IN ('Admin', 'Admin Funcional', 'Gestor', 'Revisor', 'Especialista', 'Usuário Técnico')
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );

            -- configuracao.gerenciar: exclusivo do papel Admin (controle total da plataforma)
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE p.code = 'configuracao.gerenciar'
              AND r.name = 'Admin'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
        ");

        // 7. Seed (dado, não código): configurações iniciais — Anthropic ativo para Generation e Embedding,
        // OpenAI disponível porém inativo (troca via tela de configuração). Sem chave de API no banco.
        Execute.Sql(@"
            INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at)
            SELECT 'Generation', 'Anthropic', 'claude-sonnet-4-5-20250929', TRUE, NULL, UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM llm_provider_configs WHERE purpose = 'Generation' AND is_active = TRUE);

            INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at)
            SELECT 'Embedding', 'Anthropic', 'voyage-3', TRUE, NULL, UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM llm_provider_configs WHERE purpose = 'Embedding' AND is_active = TRUE);

            INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at)
            SELECT 'Generation', 'OpenAI', 'gpt-4o-mini', FALSE, NULL, UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM llm_provider_configs WHERE purpose = 'Generation' AND provider_code = 'OpenAI');

            INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at)
            SELECT 'Embedding', 'OpenAI', 'text-embedding-3-small', FALSE, NULL, UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM llm_provider_configs WHERE purpose = 'Embedding' AND provider_code = 'OpenAI');
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DELETE FROM ai_interaction_feedback;
            DELETE FROM ai_sources;
            DELETE FROM ai_interactions;

            DELETE rp FROM role_permissions rp
            INNER JOIN permissions p ON p.id = rp.permission_id
            WHERE p.code IN ('ia.usar', 'configuracao.gerenciar');

            DELETE FROM permissions WHERE code IN ('ia.usar', 'configuracao.gerenciar');

            DELETE FROM llm_provider_configs;
        ");

        if (Schema.Table("ai_interaction_feedback").Exists()) Delete.Table("ai_interaction_feedback");
        if (Schema.Table("ai_sources").Exists()) Delete.Table("ai_sources");
        if (Schema.Table("ai_interactions").Exists()) Delete.Table("ai_interactions");

        if (Schema.Table("searchable_content_entries").Exists())
        {
            if (Schema.Table("searchable_content_entries").Column("embedding_vector").Exists())
                Delete.Column("embedding_vector").FromTable("searchable_content_entries");
            if (Schema.Table("searchable_content_entries").Column("embedding_model").Exists())
                Delete.Column("embedding_model").FromTable("searchable_content_entries");
            if (Schema.Table("searchable_content_entries").Column("embedding_generated_at").Exists())
                Delete.Column("embedding_generated_at").FromTable("searchable_content_entries");
        }

        if (Schema.Table("llm_provider_configs").Exists()) Delete.Table("llm_provider_configs");
    }
}