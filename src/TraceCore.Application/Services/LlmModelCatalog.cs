using System.Collections.Generic;
using System.Linq;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

/// <summary>
/// Fase 17: catalogo de modelos suportados pelos provedores implementados (Anthropic, OpenAI).
/// A lista e declarativa — nao e um contrato eterno. Modelos fora da lista sao aceitos
/// pela UI como campo livre ("Outro / identificador personalizado").
/// Validacao no LlmConfigurationService avisa quando o modelo nao esta catalogado,
/// mas nao impede o salvamento.
/// </summary>
public sealed class LlmModelCatalog : ILlmModelCatalog
{
    private static readonly Dictionary<(string Provider, string Purpose), LlmModelEntry[]> _catalog = new()
    {
        // Anthropic — Geracao
        [("Anthropic", "Generation")] =
        [
            new("claude-opus-4-5", "Claude Opus 4.5"),
            new("claude-sonnet-4-5-20250929", "Claude Sonnet 4.5 (set/25)", IsDefault: true),
            new("claude-haiku-3-5-20241022", "Claude Haiku 3.5 (out/24)"),
        ],
        // Anthropic — Embedding
        [("Anthropic", "Embedding")] =
        [
            new("voyage-3", "Voyage 3", IsDefault: true),
            new("voyage-3-lite", "Voyage 3 Lite"),
        ],
        // OpenAI — Geracao
        [("OpenAI", "Generation")] =
        [
            new("gpt-4o", "GPT-4o"),
            new("gpt-4o-mini", "GPT-4o Mini", IsDefault: true),
            new("gpt-4-turbo", "GPT-4 Turbo"),
            new("o1-mini", "o1 Mini"),
        ],
        // OpenAI — Embedding
        [("OpenAI", "Embedding")] =
        [
            new("text-embedding-3-large", "Text Embedding 3 Large"),
            new("text-embedding-3-small", "Text Embedding 3 Small", IsDefault: true),
        ],
    };

    private static readonly HashSet<string> _knownProviders = ["Anthropic", "OpenAI"];

    public IReadOnlyList<LlmModelEntry> GetSupportedModels(string providerCode, string purpose)
    {
        var key = (providerCode?.Trim() ?? string.Empty, purpose?.Trim() ?? string.Empty);
        return _catalog.TryGetValue(key, out var models) ? models : [];
    }

    public bool IsKnownProvider(string providerCode) =>
        _knownProviders.Contains(providerCode?.Trim() ?? string.Empty);
}
