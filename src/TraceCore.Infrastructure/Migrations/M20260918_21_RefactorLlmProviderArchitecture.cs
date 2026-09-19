using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091821, "Fase 17: Desacoplamento completo de provedores de IA — arquitetura Provider/Protocol/Model/Purpose/Credential. Nova tabela llm_providers, llm_model_configs, migra dados existentes sem perda.")]
public class M20260918_21_RefactorLlmProviderArchitecture : Migration
{
    public override void Up()
    {
        // 1. Nova tabela llm_providers — provedores administrativos cadastráveis
        if (!Schema.Table("llm_providers").Exists())
        {
            Create.Table("llm_providers")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("code").AsString(100).NotNullable().Unique("ux_llm_providers_code")
                .WithColumn("protocol").AsString(50).NotNullable() // OpenAICompatible, AnthropicMessages
                .WithColumn("base_url").AsString(500).NotNullable()
                .WithColumn("authentication_type").AsString(50).NotNullable().WithDefaultValue("BearerApiKey") // None, BearerApiKey, HeaderApiKey
                .WithColumn("has_generation_capability").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("has_embedding_capability").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("status").AsString(32).NotNullable().WithDefaultValue("Active") // Active, Inactive
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_llmprov_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_llmprov_upd_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("ix_llm_providers_status").OnTable("llm_providers").OnColumn("status");
        }

        // 2. Nova tabela llm_model_configs — configuração de uso por propósito
        if (!Schema.Table("llm_model_configs").Exists())
        {
            Create.Table("llm_model_configs")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("purpose").AsString(32).NotNullable() // Generation, Embedding
                .WithColumn("provider_id").AsInt64().NotNullable().ForeignKey("fk_llmmodcfg_provider", "llm_providers", "id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("model_name").AsString(200).NotNullable()
                .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("created_by").AsInt64().Nullable().ForeignKey("fk_llmmodcfg_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_by").AsInt64().Nullable().ForeignKey("fk_llmmodcfg_upd_user", "users", "id").OnDelete(System.Data.Rule.SetNull)
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("ix_llm_model_configs_purpose_active")
                .OnTable("llm_model_configs")
                .OnColumn("purpose").Ascending()
                .OnColumn("is_active").Ascending();

            Create.Index("ix_llm_model_configs_provider").OnTable("llm_model_configs").OnColumn("provider_id");
        }

        // 3. Migrar dados existentes de llm_provider_configs para as novas tabelas (se llm_provider_configs existir)
        if (Schema.Table("llm_provider_configs").Exists())
        {
            Execute.Sql(@"
                -- Inserir provedores se não existirem
                INSERT INTO llm_providers (name, code, protocol, base_url, authentication_type, has_generation_capability, has_embedding_capability, status, created_by, created_at)
                SELECT
                    CASE provider_code
                        WHEN 'Anthropic' THEN 'Anthropic'
                        WHEN 'OpenAI' THEN 'OpenAI'
                        ELSE provider_code
                    END AS name,
                    LOWER(provider_code) AS code,
                    CASE provider_code
                        WHEN 'Anthropic' THEN 'AnthropicMessages'
                        ELSE 'OpenAICompatible'
                    END AS protocol,
                    CASE provider_code
                        WHEN 'Anthropic' THEN 'https://api.anthropic.com/v1'
                        ELSE 'https://api.openai.com/v1'
                    END AS base_url,
                    CASE provider_code
                        WHEN 'Anthropic' THEN 'HeaderApiKey'
                        ELSE 'BearerApiKey'
                    END AS authentication_type,
                    TRUE AS has_generation_capability,
                    MAX(CASE WHEN purpose = 'Embedding' THEN TRUE ELSE FALSE END) AS has_embedding_capability,
                    'Active' AS status,
                    MIN(created_by) AS created_by,
                    MIN(created_at) AS created_at
                FROM llm_provider_configs
                WHERE NOT EXISTS (
                    SELECT 1 FROM llm_providers lp WHERE lp.code = LOWER(llm_provider_configs.provider_code)
                )
                GROUP BY provider_code;
            ");

            // Migrar configurações de uso para llm_model_configs
            Execute.Sql(@"
                INSERT INTO llm_model_configs (purpose, provider_id, model_name, is_active, created_by, created_at, updated_by, updated_at)
                SELECT 
                    lpc.purpose,
                    lp.id AS provider_id,
                    lpc.model_name,
                    lpc.is_active,
                    lpc.created_by,
                    lpc.created_at,
                    lpc.updated_by,
                    lpc.updated_at
                FROM llm_provider_configs lpc
                INNER JOIN llm_providers lp ON LOWER(lp.code) = LOWER(lpc.provider_code)
                WHERE NOT EXISTS (
                    SELECT 1 FROM llm_model_configs lmc 
                    WHERE lmc.purpose = lpc.purpose 
                      AND lmc.provider_id = lp.id 
                      AND lmc.model_name = lpc.model_name
                );
            ");
        }
    }

    public override void Down()
    {
        if (Schema.Table("llm_model_configs").Exists()) Delete.Table("llm_model_configs");
        if (Schema.Table("llm_providers").Exists()) Delete.Table("llm_providers");
    }
}
