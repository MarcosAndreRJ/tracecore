using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// BR-040 a BR-049: Item corporativo da Base de Conhecimento e Soluções (M05).
/// Representa o cabeçalho e metadados agregados do conhecimento.
/// O conteúdo textual e estruturado é versionado em KnowledgeVersion.
/// </summary>
public class KnowledgeItem
{
    public long Id { get; set; }
    public string KnowledgeCode { get; set; } = string.Empty;
    public string KnowledgeType { get; set; } = "Solution";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Review, Published, Deprecated, Archived
    public string Confidentiality { get; set; } = "Internal";
    public long? OwnerUserId { get; set; }
    public long? OwnerDepartmentId { get; set; }
    public int CurrentVersionNo { get; set; } = 1;
    
    // Rastreabilidade e Origem (BR-046)
    public string ProvenanceType { get; set; } = "Case"; // Case, Documentation, Initiative, Ai
    public long? ProvenanceCaseId { get; set; }
    public string? ProvenanceReference { get; set; }

    // Qual iteração do caso (Case.Iterations) gerou esta solução — usado para permitir uma
    // nova solução do mesmo caso apenas quando ele foi reaberto e resolvido novamente
    // (nova iteração), distinguindo isso de uma tentativa de duplicar a solução já ativa.
    public long? SourceCaseIterationId { get; set; }
    
    // Ciclo de Vida e Revisão (BR-041, BR-042, BR-049)
    public DateTime? ReviewDueAt { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
    public long? ReplacementKnowledgeId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public KnowledgeItem() { }

    public KnowledgeItem(
        string knowledgeCode,
        string title,
        string summary,
        string knowledgeType,
        string provenanceType,
        long createdBy,
        long? provenanceCaseId = null,
        string? provenanceReference = null,
        long? ownerUserId = null,
        long? ownerDepartmentId = null,
        string confidentiality = "Internal")
    {
        if (string.IsNullOrWhiteSpace(knowledgeCode))
            throw new ArgumentException("Código do item de conhecimento é obrigatório.", nameof(knowledgeCode));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título é obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(provenanceType))
            throw new ArgumentException("Origem do conhecimento (BR-046) é obrigatória.", nameof(provenanceType));

        KnowledgeCode = knowledgeCode.Trim().ToUpperInvariant();
        Title = title.Trim();
        Summary = summary?.Trim() ?? string.Empty;
        KnowledgeType = string.IsNullOrWhiteSpace(knowledgeType) ? "Solution" : knowledgeType.Trim();
        ProvenanceType = provenanceType.Trim();
        ProvenanceCaseId = provenanceCaseId;
        ProvenanceReference = provenanceReference;
        OwnerUserId = ownerUserId;
        OwnerDepartmentId = ownerDepartmentId;
        Confidentiality = confidentiality;
        Status = "Draft"; // BR-040: todo conhecimento nasce como Draft
        CurrentVersionNo = 1;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitForReview()
    {
        if (Status != "Draft")
            throw new InvalidOperationException($"Apenas itens em rascunho (Draft) podem ser submetidos para revisão. Status atual: {Status}.");

        Status = "Review";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish(int versionNo, long approvedBy, DateTime approvedAt, DateTime? nextReviewDue)
    {
        if (Status != "Review" && Status != "Draft")
            throw new InvalidOperationException($"Item com status '{Status}' não pode ser publicado diretamente.");

        Status = "Published";
        CurrentVersionNo = versionNo;
        PublishedAt = approvedAt;
        LastReviewedAt = approvedAt;
        ReviewDueAt = nextReviewDue ?? approvedAt.AddMonths(6);
        UpdatedAt = approvedAt;
    }

    public void Deprecate(DateTime deprecatedAt, long? replacementKnowledgeId = null)
    {
        Status = "Deprecated";
        DeprecatedAt = deprecatedAt;
        ReplacementKnowledgeId = replacementKnowledgeId;
        UpdatedAt = deprecatedAt;
    }

    public void Archive(DateTime archivedAt)
    {
        Status = "Archived";
        UpdatedAt = archivedAt;
    }

    public void IncrementVersion(int newVersionNo)
    {
        CurrentVersionNo = newVersionNo;
        UpdatedAt = DateTime.UtcNow;
    }
}
