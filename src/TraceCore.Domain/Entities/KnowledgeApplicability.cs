using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// BR-043: Declaração de aplicabilidade de um item de conhecimento.
/// Uma solução DEVE declarar ao menos um elemento de aplicabilidade para publicação.
/// </summary>
public class KnowledgeApplicability
{
    public long Id { get; set; }
    public long KnowledgeItemId { get; set; }
    public long? ProductId { get; set; }
    public long? ProductVersionId { get; set; }
    public long? ComponentId { get; set; }
    public long? EnvironmentId { get; set; }
    public string ApplicabilityType { get; set; } = "Applies"; // Applies, Excludes, Recommended
    public string? Notes { get; set; }

    public KnowledgeApplicability() { }

    public KnowledgeApplicability(
        long knowledgeItemId,
        long? productId = null,
        long? productVersionId = null,
        long? componentId = null,
        long? environmentId = null,
        string applicabilityType = "Applies",
        string? notes = null)
    {
        if (knowledgeItemId <= 0)
            throw new ArgumentException("KnowledgeItemId inválido.", nameof(knowledgeItemId));

        KnowledgeItemId = knowledgeItemId;
        ProductId = productId;
        ProductVersionId = productVersionId;
        ComponentId = componentId;
        EnvironmentId = environmentId;
        ApplicabilityType = string.IsNullOrWhiteSpace(applicabilityType) ? "Applies" : applicabilityType.Trim();
        Notes = notes?.Trim();
    }
}
