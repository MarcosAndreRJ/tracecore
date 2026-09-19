using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 13 (M12): provedor de embedding (vetorização de textos) usado na busca
/// assistida por IA. Implementações concretas (Anthropic, OpenAI) vivem em
/// Infrastructure via HttpClient puro (DEV-AI-003), nunca SDK de terceiros.
/// </summary>
public interface IEmbeddingProvider
{
    string ProviderCode { get; }
    string ModelName { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}