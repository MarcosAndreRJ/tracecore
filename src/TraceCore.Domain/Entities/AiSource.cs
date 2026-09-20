using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 13 (M12): fonte recuperada da base oficial usada na resposta (BR-084).
/// Originalmente só apontava para uma entrada indexada por embedding
/// (SearchableContentEntryId + SimilarityScore de cosseno, ambos obrigatórios).
/// Prompt 3 (Copiloto investigativo) generalizou o modelo: uma fonte agora pode vir
/// de busca estruturada (caso, conhecimento) sem nunca ter sido indexada por
/// embedding e sem ter uma similaridade de cosseno — apenas um score determinístico
/// (ex.: CaseRelationService, 0-100) ou nenhum score. SearchableContentEntryId e
/// SimilarityScore continuam preenchidos normalmente pelo RAG semântico (RagService)
/// e ficam nulos para fontes de busca estruturada, que usam SourceType/SourceRefId/
/// SourceUrl/SourceTitle/MatchScore.
/// </summary>
public class AiSource
{
    public long Id { get; set; }
    public long AiInteractionId { get; set; }

    public long? SearchableContentEntryId { get; set; }
    public double? SimilarityScore { get; set; }

    // Catálogo aberto: "SearchableContent" (RAG semântico existente), "HistoricalCase",
    // "ValidatedKnowledge", "TechnicalContext", "ExternalSource" (Prompt 4).
    public string? SourceType { get; set; }

    // Id da entidade referenciada (CaseId, KnowledgeItemId, ProductId) quando SourceType
    // não for "SearchableContent". Nulo para fonte externa (Prompt 4), que não tem
    // id interno — usa SourceUrl/SourceTitle.
    public long? SourceRefId { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceTitle { get; set; }

    // Score determinístico (ex.: 0-100 do CaseRelationService) — nunca confundir com
    // SimilarityScore (cosseno). Nulo quando a fonte não tem pontuação.
    public double? MatchScore { get; set; }

    public int Rank { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiSource() { }

    /// <summary>Fonte do RAG semântico existente (SearchableContentEntry indexado por embedding).</summary>
    public AiSource(
        long aiInteractionId,
        long searchableContentEntryId,
        int rank,
        double similarityScore)
    {
        AiInteractionId = aiInteractionId;
        SearchableContentEntryId = searchableContentEntryId;
        SimilarityScore = similarityScore;
        SourceType = "SearchableContent";
        Rank = rank;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Fonte de busca estruturada (Prompt 3): caso, conhecimento ou contexto técnico.</summary>
    public static AiSource ForStructuredSource(long aiInteractionId, string sourceType, long sourceRefId, int rank, double? matchScore = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("SourceType é obrigatório.", nameof(sourceType));

        return new AiSource
        {
            AiInteractionId = aiInteractionId,
            SourceType = sourceType.Trim(),
            SourceRefId = sourceRefId,
            MatchScore = matchScore,
            Rank = rank,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Fonte externa (Prompt 4): sem id interno, identificada por URL/título.</summary>
    public static AiSource ForExternalSource(long aiInteractionId, string sourceUrl, string sourceTitle, int rank)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            throw new ArgumentException("SourceUrl é obrigatório.", nameof(sourceUrl));

        return new AiSource
        {
            AiInteractionId = aiInteractionId,
            SourceType = "ExternalSource",
            SourceUrl = sourceUrl.Trim(),
            SourceTitle = string.IsNullOrWhiteSpace(sourceTitle) ? sourceUrl.Trim() : sourceTitle.Trim(),
            Rank = rank,
            CreatedAt = DateTime.UtcNow
        };
    }
}
