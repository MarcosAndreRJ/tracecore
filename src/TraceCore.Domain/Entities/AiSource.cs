using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 13 (M12): fonte recuperada da base oficial usada na resposta (BR-084).
/// Aponta para uma entrada de conteúdo pesquisável (artigo/caso/documento).
/// </summary>
public class AiSource
{
    public long Id { get; set; }
    public long AiInteractionId { get; set; }
    public long SearchableContentEntryId { get; set; }
    public int Rank { get; set; }
    public double SimilarityScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiSource() { }

    public AiSource(
        long aiInteractionId,
        long searchableContentEntryId,
        int rank,
        double similarityScore)
    {
        AiInteractionId = aiInteractionId;
        SearchableContentEntryId = searchableContentEntryId;
        Rank = rank;
        SimilarityScore = similarityScore;
        CreatedAt = DateTime.UtcNow;
    }
}