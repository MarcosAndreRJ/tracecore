using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

/// <summary>
/// Fase 13 (M12): indexação idempotente de embeddings via ação administrativa explícita.
/// Só processa conteúdo elegível (Validated + Complete/Validated + Public/Internal) ainda sem
/// vetor para o modelo de embedding ativo. Nada roda em background (BR-083).
/// </summary>
public class EmbeddingIndexingService : IEmbeddingIndexingService
{
    private const int MaxEmbeddingChars = 6000;

    private readonly ILlmProviderResolver _resolver;
    private readonly ISearchableContentRepository _searchableContentRepository;

    public EmbeddingIndexingService(
        ILlmProviderResolver resolver,
        ISearchableContentRepository searchableContentRepository)
    {
        _resolver = resolver;
        _searchableContentRepository = searchableContentRepository;
    }

    public async Task<EmbeddingIndexingResultDto> IndexReadyContentAsync(int limit = 200, CancellationToken ct = default)
    {
        var embeddingProvider = await _resolver.ResolveEmbeddingProviderAsync(ct);

        var pending = await _searchableContentRepository.GetPendingEmbeddingAsync(
            embeddingProvider.ModelName, limit, ct);

        int ok = 0, fail = 0;
        foreach (var entry in pending)
        {
            try
            {
                var vector = await embeddingProvider.EmbedAsync(
                    Truncate(entry.NormalizedContent, MaxEmbeddingChars), ct);

                var json = JsonSerializer.Serialize(vector);
                await _searchableContentRepository.UpdateEmbeddingAsync(
                    entry.Id, json, embeddingProvider.ModelName, DateTime.UtcNow, ct);
                ok++;
            }
            catch
            {
                // Falha pontual não aborta o lote — o registro segue elegível na próxima rodada.
                fail++;
            }
        }

        string? message = pending.Count == 0
            ? "Nenhum conteúdo pendente para o modelo de embedding atual."
            : $"{ok} conteúdo(s) vetorizado(s) com sucesso";

        return new EmbeddingIndexingResultDto(
            ProcessedCount: ok,
            FailedCount: fail,
            ModelName: embeddingProvider.ModelName,
            ProviderConfigured: true,
            Message: message);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}