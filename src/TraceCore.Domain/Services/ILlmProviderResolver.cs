using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 13/17: resolve o provedor ativo por propósito (Generation/Embedding)
/// a partir de llm_model_configs + llm_providers (banco) + credencial de API Key
/// (configuração de ambiente, nunca no banco). Falha explicitamente (fail-fast)
/// quando não há provedor ativo ou credencial configurada — a UI informa o estado real.
/// </summary>
public interface ILlmProviderResolver
{
    Task<ILlmProvider> ResolveGenerationProviderAsync(CancellationToken ct = default);
    Task<IEmbeddingProvider> ResolveEmbeddingProviderAsync(CancellationToken ct = default);
    bool IsCredentialConfigured(string providerCode);

    /// <summary>
    /// Testa conexão para uma configuração de modelo específica (llm_model_configs).
    /// Retorna resultado sem expor segredos ou stack traces.
    /// </summary>
    Task<ConnectionTestResult> TestModelConfigConnectionAsync(long modelConfigId, CancellationToken ct = default);

    /// <summary>
    /// Testa conexão legado (pelo providerCode e purpose) — compatibilidade.
    /// </summary>
    Task<ConnectionTestResult> TestLegacyConnectionAsync(string providerCode, string purpose, CancellationToken ct = default);
}

/// <summary>
/// Resultado de teste de conexão.
/// </summary>
public record ConnectionTestResult(bool Success, string? Message);