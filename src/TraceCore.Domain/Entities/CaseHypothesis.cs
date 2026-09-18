using System;
using TraceCore.Domain.Enums;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Entidade de hipótese de investigação vinculada a um caso (BR-023, BR-024).
/// Um caso pode ter múltiplas hipóteses simultâneas (BR-023).
/// </summary>
public class CaseHypothesis
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public long CaseIterationId { get; set; }
    public long? ComponentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = nameof(HypothesisStatus.Proposed);
    public string SourceType { get; set; } = "Human";
    public string? Justification { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Construtor para deserialização e mapeamento Dapper
    public CaseHypothesis() { }

    public CaseHypothesis(
        long caseId,
        string title,
        string? description = null,
        long? componentId = null,
        long? createdBy = null,
        DateTime? createdAt = null)
    {
        if (caseId <= 0)
            throw new ArgumentException("O ID do caso deve ser informado.", nameof(caseId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("O título da hipótese não pode ser vazio.", nameof(title));

        CaseId = caseId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ComponentId = componentId;
        Status = nameof(HypothesisStatus.Proposed);
        SourceType = "Human";
        CreatedAt = createdAt ?? DateTime.UtcNow;
        CreatedBy = createdBy;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Avalia a hipótese alterando seu status para Discarded ou Supported (BR-024).
    /// </summary>
    public void Evaluate(HypothesisStatus newStatus, string justification, DateTime? evaluatedAt = null)
    {
        if (newStatus == HypothesisStatus.Proposed)
        {
            // TODO: BR-024 — Se no futuro o produto definir regra formal para reabertura de hipótese descartada por engano,
            // implementar fluxo auditado sem sobrescrever o histórico original.
            throw new InvalidOperationException("Não é permitido regredir uma hipótese avaliada de volta para 'Proposed' apagando a decisão histórica.");
        }

        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new ArgumentException("A justificativa técnica da avaliação da hipótese é obrigatória.", nameof(justification));
        }

        Status = newStatus.ToString();
        Justification = justification.Trim();
        UpdatedAt = evaluatedAt ?? DateTime.UtcNow;
    }
}
