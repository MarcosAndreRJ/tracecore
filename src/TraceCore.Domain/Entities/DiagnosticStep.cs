using System;
using TraceCore.Domain.Enums;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Registra uma tentativa, verificação ou observação de diagnóstico executada (BR-025, BR-026).
/// </summary>
public class DiagnosticStep
{
    public long Id { get; set; }
    public long DiagnosticSessionId { get; set; }
    public int SequenceNo { get; set; }
    public string StepType { get; set; } = DiagnosticStepTypes.Verification;
    public long? HypothesisId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Instruction { get; set; }
    public string? InputEvidenceSummary { get; set; }
    public string ResultSummary { get; set; } = string.Empty;
    public string Outcome { get; set; } = nameof(DiagnosticStepOutcome.Inconclusive);
    public string RiskLevel { get; set; } = "Low";
    public int? DurationSeconds { get; set; }
    public long? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? MetadataJson { get; set; }

    // Fase 05 — Execução de integração que gerou/validou este passo (teste real),
    // substituindo a dependência de um texto solto em InputEvidenceSummary por uma
    // associação estrutural real com a tabela integration_runs.
    public long? IntegrationRunId { get; set; }

    // Construtor para deserialização e mapeamento Dapper
    public DiagnosticStep() { }

    public DiagnosticStep(
        long diagnosticSessionId,
        int sequenceNo,
        string stepType,
        string title,
        string objective,
        string inputEvidenceSummary,
        string resultSummary,
        DiagnosticStepOutcome outcome,
        long performedBy,
        long? hypothesisId = null,
        string? instruction = null,
        string riskLevel = "Low",
        int? durationSeconds = null,
        DateTime? performedAt = null,
        string? metadataJson = null)
    {
        if (diagnosticSessionId <= 0)
            throw new ArgumentException("O ID da sessão de diagnóstico é obrigatório.", nameof(diagnosticSessionId));
        if (sequenceNo <= 0)
            throw new ArgumentException("O número sequencial do passo deve ser positivo.", nameof(sequenceNo));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A ação/título executada é obrigatória (BR-025).", nameof(title));
        if (string.IsNullOrWhiteSpace(objective))
            throw new ArgumentException("O objetivo do teste/tentativa é obrigatório (BR-025).", nameof(objective));
        if (string.IsNullOrWhiteSpace(inputEvidenceSummary))
            throw new ArgumentException("A evidência anterior/de entrada é obrigatória (BR-025).", nameof(inputEvidenceSummary));
        if (string.IsNullOrWhiteSpace(resultSummary))
            throw new ArgumentException("O resultado observado é obrigatório (BR-025).", nameof(resultSummary));
        if (performedBy <= 0)
            throw new ArgumentException("O autor da ação é obrigatório (BR-025).", nameof(performedBy));

        DiagnosticSessionId = diagnosticSessionId;
        SequenceNo = sequenceNo;
        StepType = string.IsNullOrWhiteSpace(stepType) ? DiagnosticStepTypes.Verification : stepType.Trim();
        HypothesisId = hypothesisId;
        Title = title.Trim();
        Objective = objective.Trim();
        Instruction = string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim();
        InputEvidenceSummary = inputEvidenceSummary.Trim();
        ResultSummary = resultSummary.Trim();
        Outcome = outcome.ToString();
        RiskLevel = string.IsNullOrWhiteSpace(riskLevel) ? "Low" : riskLevel.Trim();
        DurationSeconds = durationSeconds;
        PerformedBy = performedBy;
        PerformedAt = performedAt ?? DateTime.UtcNow;
        MetadataJson = metadataJson;
    }
}
