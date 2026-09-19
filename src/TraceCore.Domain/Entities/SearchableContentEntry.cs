using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// M11 / §12.4: Registro estruturado e normalizado de conteúdo preparado para busca semântica e IA.
/// Sem vetores ou chamadas a modelos generativos nesta fase.
/// </summary>
public class SearchableContentEntry
{
    public long Id { get; set; }
    
    // Origem e Rastreabilidade (§12.4 / §12.20)
    public string SourceType { get; set; } = string.Empty; // ValidatedKnowledge, HistoricalCase, Document, AiSuggestion
    public long SourceId { get; set; }
    public long? SourceVersionId { get; set; }

    // Conteúdo e Hash (§12.8 / §12.23)
    public string Title { get; set; } = string.Empty;
    public string NormalizedContent { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty; // SHA-256 (64 hex lowercase)

    // Qualidade e Prontidão Explicáveis (§12.15 / §12.18)
    public string ValidationStatus { get; set; } = "NotValidated"; // Validated, PendingValidation, NotValidated, Rejected
    public string QualityStatus { get; set; } = "Incomplete"; // Complete, Incomplete, NeedsReview, Validated, Obsolete
    public string Visibility { get; set; } = "Internal"; // Public, Internal, Confidential, Restricted

    // Relacionamentos e Metadados Estruturados (§12.11 / §12.12)
    public long? ClientId { get; set; }
    public long? ProductId { get; set; }
    public string? ComponentIdsJson { get; set; }
    public string? MetadataJson { get; set; }

    // Ciclo de Vida e Campos Reservados (§12.26)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SourceUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? IndexedAt { get; set; }
    public string? EmbeddingVersion { get; set; } // Identificador do algoritmo/versão da vetorização

    // Fase 13 (M12): vetor de embedding e metadados de geração (ADR-P006 — vetor em MySQL, similaridade por cosseno)
    public string? EmbeddingVectorJson { get; set; } // JSON: array de floats
    public string? EmbeddingModel { get; set; }
    public DateTime? EmbeddingGeneratedAt { get; set; }

    public SearchableContentEntry() { }

    public SearchableContentEntry(
        string sourceType,
        long sourceId,
        string title,
        string normalizedContent,
        string contentHash,
        string validationStatus,
        string qualityStatus,
        string visibility,
        DateTime sourceUpdatedAt,
        long? sourceVersionId = null,
        long? clientId = null,
        long? productId = null,
        string? componentIdsJson = null,
        string? metadataJson = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("SourceType é obrigatório.", nameof(sourceType));
        if (sourceId <= 0)
            throw new ArgumentException("SourceId deve ser positivo.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title é obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("ContentHash é obrigatório.", nameof(contentHash));

        SourceType = sourceType.Trim();
        SourceId = sourceId;
        SourceVersionId = sourceVersionId;
        Title = title.Trim();
        NormalizedContent = normalizedContent ?? string.Empty;
        ContentHash = contentHash.Trim().ToLowerInvariant();
        ValidationStatus = validationStatus?.Trim() ?? "NotValidated";
        QualityStatus = qualityStatus?.Trim() ?? "Incomplete";
        Visibility = visibility?.Trim() ?? "Internal";
        SourceUpdatedAt = sourceUpdatedAt;
        ClientId = clientId;
        ProductId = productId;
        ComponentIdsJson = componentIdsJson;
        MetadataJson = metadataJson;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IndexedAt = null;
        EmbeddingVersion = null;
    }
}
