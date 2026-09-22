using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TraceCore.Domain.Entities;

namespace TraceCore.Application.Services;

public class CaseSimilarityScoringService : ICaseSimilarityScoringService
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "que", "para", "com", "não", "uma", "por", "mais", "dos", "como", "mas",
        "foi", "ao", "ele", "das", "tem", "à", "seu", "sua", "ou", "ser",
        "quando", "muito", "há", "nos", "já", "está", "eu", "também", "só",
        "pelo", "pela", "até", "isso", "ela", "entre", "era", "depois", "sem",
        "mesmo", "aos", "ter", "seus", "quem", "nas", "me", "esse", "eles",
        "estão", "você", "tinha", "foram", "essa", "num", "nem", "suas", "meu",
        "às", "minha", "têm", "numa", "pelos", "elas", "havia", "seja", "qual",
        "será", "nós", "tenho", "lhe", "deles", "essas", "esses", "pelas", "este",
        "fosse", "dele", "tu", "te", "vocês", "vos", "lhes", "meus", "minhas",
        "teu", "tua", "teus", "tuas", "nosso", "nossa", "nossos", "nossas", "dela",
        "delas", "esta", "estes", "estas", "aquele", "aquela", "aqueles", "aquelas",
        "isto", "aquilo", "estou", "está", "estamos", "estão", "estive", "esteve",
        "estivemos", "estiveram", "estava", "estávamos", "estavam", "caso", "erro"
    };

    public List<(Case candidate, double score, List<string> factors)> ScoreAndRankCandidates(
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
        int topCount = 5)
    {
        var sourceCompIdSet = sourceComponentIds.ToHashSet();
        var sourceWords = ExtractSignificantWords(sourceText);
        var sourceTagSet = new HashSet<string>(sourceTags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        var scored = new List<(Case candidate, double score, List<string> factors)>();

        foreach (var candidate in candidates)
        {
            double score = 0;
            var factors = new List<string>();

            // 0. Mesmo cliente (+15)
            if (sourceClientId.HasValue && candidate.ClientId.HasValue && sourceClientId.Value == candidate.ClientId.Value)
            {
                score += 15;
                factors.Add("Mesmo cliente");
            }

            // 1. Mesmo produto (+30)
            if (sourceProductId.HasValue && candidate.ProductId.HasValue && sourceProductId.Value == candidate.ProductId.Value)
            {
                score += 30;
                factors.Add("Mesmo produto");
            }

            // 2. Mesmo componente (+25)
            var candidateCompIds = candidate.AffectedComponents.Select(c => c.ComponentId).ToHashSet();
            if (sourceCompIdSet.Count > 0 && sourceCompIdSet.Overlaps(candidateCompIds))
            {
                score += 25;
                factors.Add("Mesmo componente");
            }

            // 3. Mesma versão (+20) — pode ser desligado quando o contexto busca versões anteriores (Fase 3)
            if (enableSameVersionBonus && sourceVersionId.HasValue && candidate.ProductVersionId.HasValue && sourceVersionId.Value == candidate.ProductVersionId.Value)
            {
                score += 20;
                factors.Add("Mesma versão");
            }

            // 4. Mesmo código de erro (+35)
            if (!string.IsNullOrWhiteSpace(sourceErrorCode) && !string.IsNullOrWhiteSpace(candidate.ErrorCode) &&
                string.Equals(sourceErrorCode, candidate.ErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                score += 35;
                factors.Add($"Mesmo erro ({candidate.ErrorCode})");
            }

            // 5. Tag manual em comum (+10)
            if (sourceTagSet.Count > 0 && candidate.Tags.Count > 0 && sourceTagSet.Overlaps(candidate.Tags))
            {
                score += 10;
                factors.Add("Tag em comum");
            }

            // 6. Correspondência textual de palavras-chave no relato + sintomas (+10 a +20)
            var candidateText = BuildSourceText(candidate.NormalizedSummary ?? candidate.OriginalReport, candidate.Symptoms.Select(s => s.SymptomText));
            var candidateWords = ExtractSignificantWords(candidateText);
            int overlapWords = sourceWords.Intersect(candidateWords, StringComparer.OrdinalIgnoreCase).Count();
            if (overlapWords > 0)
            {
                score += Math.Min(overlapWords * 5.0, 20.0);
                factors.Add("Termos semelhantes no relato/sintomas");
            }

            // 7. Avaliador adicional opcional (ex.: sinal de versão anterior na Fase 3)
            if (additionalScorer != null)
            {
                var (bonus, factor) = additionalScorer(candidate);
                if (bonus > 0)
                {
                    score += bonus;
                    if (!string.IsNullOrWhiteSpace(factor))
                    {
                        factors.Add(factor);
                    }
                }
            }

            if (score <= 0) continue;

            // Princípio P-006: Similaridade é score determinístico, não probabilidade estatística.
            double normalizedScore = Math.Min(100.0, Math.Round(score, 1));
            scored.Add((candidate, normalizedScore, factors));
        }

        return scored
            .OrderByDescending(s => s.score)
            .Take(topCount)
            .ToList();
    }

    public string BuildSourceText(string? reportText, IEnumerable<string> symptomTexts)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(reportText)) parts.Add(reportText);
        parts.AddRange(symptomTexts.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.Join(" ", parts);
    }

    public HashSet<string> ExtractSignificantWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>();

        var words = Regex.Matches(text, @"\b[A-Za-z0-9_]{3,}\b")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => !Stopwords.Contains(w));

        return new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }
}
