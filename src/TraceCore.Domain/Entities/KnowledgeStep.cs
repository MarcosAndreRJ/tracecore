using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Passo procedimental estruturado vinculado à versão do conhecimento.
/// </summary>
public class KnowledgeStep
{
    public long Id { get; set; }
    public long KnowledgeVersionId { get; set; }
    public int SequenceNo { get; set; }
    public string StepType { get; set; } = "Solution"; // Diagnostic, Solution, Verification, Rollback
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Command { get; set; }
    public string? ExpectedOutput { get; set; }

    public KnowledgeStep() { }

    public KnowledgeStep(
        long knowledgeVersionId,
        int sequenceNo,
        string title,
        string description,
        string stepType = "Solution",
        string? command = null,
        string? expectedOutput = null)
    {
        if (knowledgeVersionId <= 0)
            throw new ArgumentException("KnowledgeVersionId inválido.", nameof(knowledgeVersionId));
        if (sequenceNo <= 0)
            throw new ArgumentException("SequenceNo deve ser maior que zero.", nameof(sequenceNo));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título do passo é obrigatório.", nameof(title));

        KnowledgeVersionId = knowledgeVersionId;
        SequenceNo = sequenceNo;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        StepType = string.IsNullOrWhiteSpace(stepType) ? "Solution" : stepType.Trim();
        Command = command?.Trim();
        ExpectedOutput = expectedOutput?.Trim();
    }
}
