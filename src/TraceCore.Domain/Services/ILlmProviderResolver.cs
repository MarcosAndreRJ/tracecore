using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 13 (M12): resolve o provedor ativo por propósito (Generation/Embedding)
/// a partir de llm_provider_configs (banco) + credencial de API Key (configuração
/// de ambiente, nunca no banco). Falha explicitamente (fail-fast) quando não há
/// provedor ativo ou credencial configurada — a UI informa o estado real.
/// </summary>
public interface ILlmProviderResolver
{
    Task<ILlmProvider> ResolveGenerationProviderAsync(CancellationToken ct = default);
    Task<IEmbeddingProvider> ResolveEmbeddingProviderAsync(CancellationToken ct = default);
    bool IsCredentialConfigured(string providerCode);
}