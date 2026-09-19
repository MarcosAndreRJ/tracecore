using System.Collections.Generic;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 17: catalogo de modelos suportados por provider x proposito.
/// A lista nao e eterna — pode ser atualizada sem alterar a interface.
/// Qualquer string fora da lista e aceita como modelo personalizado (campo livre de escape).
/// </summary>
public record LlmModelEntry(string ModelId, string DisplayName, bool IsDefault = false);

public interface ILlmModelCatalog
{
    /// <summary>
    /// Retorna os modelos conhecidos para o par (providerCode, purpose).
    /// Lista vazia = provedor/proposito nao suportado pelo catalogo.
    /// Campo livre sempre disponivel na UI independente do retorno.
    /// </summary>
    IReadOnlyList<LlmModelEntry> GetSupportedModels(string providerCode, string purpose);

    /// <summary>
    /// Retorna true se o providerCode eh reconhecido pelo catalogo.
    /// Nao bloqueia provedores desconhecidos — apenas informa.
    /// </summary>
    bool IsKnownProvider(string providerCode);
}
