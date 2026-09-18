using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// BR-047, BR-048: Registro de utilização real de uma solução em um caso/incidente.
/// É a base factual exclusiva para o cálculo de taxa de sucesso e eficácia da solução.
/// </summary>
public class KnowledgeUsage
{
    public long Id { get; set; }
    public long KnowledgeItemId { get; set; }
    public long KnowledgeVersionId { get; set; }
    public long CaseId { get; set; }
    public long UsedBy { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public string Outcome { get; set; } = "Worked"; // Worked, PartiallyWorked, DidNotWork, Inconclusive
    public string? Notes { get; set; }
    public string? ContextMatchJson { get; set; }

    public KnowledgeUsage() { }

    public KnowledgeUsage(
        long knowledgeItemId,
        long knowledgeVersionId,
        long caseId,
        long usedBy,
        string outcome,
        string? notes = null,
        string? contextMatchJson = null)
    {
        if (knowledgeItemId <= 0)
            throw new ArgumentException("KnowledgeItemId inválido.", nameof(knowledgeItemId));
        if (knowledgeVersionId <= 0)
            throw new ArgumentException("KnowledgeVersionId inválido.", nameof(knowledgeVersionId));
        if (caseId <= 0)
            throw new ArgumentException("CaseId inválido.", nameof(caseId));
        if (usedBy <= 0)
            throw new ArgumentException("UsedBy inválido.", nameof(usedBy));
        if (string.IsNullOrWhiteSpace(outcome))
            throw new ArgumentException("Outcome da utilização é obrigatório.", nameof(outcome));

        KnowledgeItemId = knowledgeItemId;
        KnowledgeVersionId = knowledgeVersionId;
        CaseId = caseId;
        UsedBy = usedBy;
        Outcome = outcome.Trim();
        Notes = notes?.Trim();
        ContextMatchJson = contextMatchJson;
        UsedAt = DateTime.UtcNow;
    }
}
