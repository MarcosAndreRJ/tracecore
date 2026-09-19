using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public interface ILlmConfigurationService
{
    // Legado (compatibilidade durante migração)
    Task<IReadOnlyList<LlmProviderConfigDto>> GetLegacyConfigsAsync(CancellationToken ct = default);
    Task ActivateLegacyAsync(long configId, long? updatedBy, CancellationToken ct = default);
    Task UpdateLegacyModelAsync(long configId, string modelName, long? updatedBy, CancellationToken ct = default);
    bool IsCredentialConfigured(string providerCode);

    /// <summary>Cria ou atualiza configuração legada. Audita com llm_provider_config.update.</summary>
    Task<LlmProviderConfigDto> UpsertLegacyConfigAsync(UpsertLlmProviderConfigCommand command, CancellationToken ct = default);

    /// <summary>
    /// Salva a API Key no ISecretStore. Audita com llm_provider_config.update
    /// incluindo CredentialChanged: true — NUNCA o valor da chave.
    /// </summary>
    Task SaveCredentialAsync(string providerCode, string apiKey, long? updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Remove a API Key do ISecretStore. Audita com llm_provider_config.credential_removed.
    /// </summary>
    Task DeleteCredentialAsync(string providerCode, long? updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Testa a conexao com o provedor ativo para o purpose informado.
    /// Nunca devolve segredo, header ou stack trace na resposta.
    /// </summary>
    Task<ConnectionTestResult> TestLegacyConnectionAsync(string providerCode, string purpose, CancellationToken ct = default);

    /// <summary>
    /// Testa a conexao com o provedor ativo para o purpose informado.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(string providerCode, string purpose, CancellationToken ct = default);

    /// <summary>Retorna os modelos do catalogo para o par (providerCode, purpose).</summary>
    IReadOnlyList<LlmModelEntry> GetLegacySupportedModels(string providerCode, string purpose);

    /// <summary>Retorna os modelos do catalogo para o par (providerCode, purpose).</summary>
    IReadOnlyList<LlmModelEntry> GetSupportedModels(string providerCode, string purpose);

    // Nova arquitetura Fase 17

    /// <summary>Lista todos os provedores administrativos cadastrados.</summary>
    Task<IReadOnlyList<LlmProviderDto>> GetProvidersAsync(CancellationToken ct = default);

    /// <summary>Obtém um provedor por ID.</summary>
    Task<LlmProviderDto?> GetProviderByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Obtém um provedor por código.</summary>
    Task<LlmProviderDto?> GetProviderByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Cria ou atualiza um provedor administrativo.</summary>
    Task<LlmProviderDto> UpsertProviderAsync(UpsertLlmProviderCommand command, CancellationToken ct = default);

    /// <summary>Lista todas as configurações de modelo por propósito.</summary>
    Task<IReadOnlyList<LlmModelConfigDto>> GetModelConfigsAsync(CancellationToken ct = default);

    /// <summary>Obtém a configuração ativa para um propósito.</summary>
    Task<LlmModelConfigDto?> GetActiveModelConfigAsync(string purpose, CancellationToken ct = default);

    /// <summary>Cria ou atualiza uma configuração de modelo por propósito.</summary>
    Task<LlmModelConfigDto> UpsertModelConfigAsync(UpsertLlmModelConfigCommand command, CancellationToken ct = default);

    /// <summary>Ativa uma configuração de modelo (desativa as outras do mesmo propósito).</summary>
    Task ActivateModelConfigAsync(long configId, long? updatedBy, CancellationToken ct = default);

    /// <summary>Testa conexão usando a nova arquitetura (provider + model config).</summary>
    Task<ConnectionTestResult> TestModelConfigConnectionAsync(long modelConfigId, CancellationToken ct = default);

    /// <summary>Protocolos suportados pelo sistema.</summary>
    IReadOnlyList<string> GetSupportedProtocols();

    /// <summary>Tipos de autenticação suportados.</summary>
    IReadOnlyList<string> GetSupportedAuthenticationTypes();
}
