using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Sintoma associado à versão do conhecimento para indexação e busca contextual.
/// </summary>
public class KnowledgeSymptom
{
    public long Id { get; set; }
    public long KnowledgeVersionId { get; set; }
    public string SymptomText { get; set; } = string.Empty;

    public KnowledgeSymptom() { }

    public KnowledgeSymptom(long knowledgeVersionId, string symptomText)
    {
        if (knowledgeVersionId <= 0)
            throw new ArgumentException("KnowledgeVersionId inválido.", nameof(knowledgeVersionId));
        if (string.IsNullOrWhiteSpace(symptomText))
            throw new ArgumentException("Texto do sintoma é obrigatório.", nameof(symptomText));

        KnowledgeVersionId = knowledgeVersionId;
        SymptomText = symptomText.Trim();
    }
}
