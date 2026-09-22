using System;
using System.Collections.Generic;
using TraceCore.Domain.Entities;

namespace TraceCore.Application.Services;

/// <summary>
/// Núcleo de pontuação e ranqueamento determinístico de similaridade entre casos e itens de versão (Fase 3).
/// Princípio P-006: similaridade determinística baseada em pesos técnicos, não em modelos probabilísticos ou LLM.
/// </summary>
public interface ICaseSimilarityScoringService
{
    /// <summary>
    /// Pontua e ranqueia casos candidatos em relação a um conjunto de características de origem.
    /// </summary>
    List<(Case candidate, double score, List<string> factors)> ScoreAndRankCandidates(
        IReadOnlyList<Case> candidates,
        long? sourceClientId,
        long? sourceProductId,
        long? sourceVersionId,
        string? sourceErrorCode,
        IReadOnlyCollection<long> sourceComponentIds,
        string sourceText,
        IReadOnlyCollection<string>? sourceTags = null,
        bool enableSameVersionBonus = true,
        Func<Case, (double bonus, string? factor)>? additionalScorer = null,
        int topCount = 5);

    /// <summary>
    /// Concatena o texto do relato e os sintomas em uma única string para tokenização.
    /// </summary>
    string BuildSourceText(string? reportText, IEnumerable<string> symptomTexts);

    /// <summary>
    /// Extrai palavras significativas com 3 ou mais caracteres, ignorando stopwords em português.
    /// </summary>
    HashSet<string> ExtractSignificantWords(string? text);
}
