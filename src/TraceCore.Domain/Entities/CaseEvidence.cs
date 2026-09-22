using System;
using System.Collections.Generic;
using TraceCore.Domain.Enums;

namespace TraceCore.Domain.Entities;

public class CaseEvidence
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public long CaseIterationId { get; set; }
    public long? DiagnosticStepId { get; set; }
    // evidence_type: Log, Screenshot, ErrorText, Link, Other (catálogo aberto de strings)
    public string EvidenceType { get; set; } = "Other";
    public string Description { get; set; } = string.Empty;
    public long? AttachmentId { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Fase 05 — Execução de integração cujo resultado fundamentou esta evidência
    // (teste durante investigação ou validação de solução).
    public long? IntegrationRunId { get; set; }

    public List<CaseHypothesisEvidence> HypothesisRelations { get; set; } = [];

    public CaseEvidence() { }

    public CaseEvidence(
        long caseId,
        string evidenceType,
        string description,
        long? attachmentId = null,
        long? createdBy = null,
        long caseIterationId = 0,
        long? diagnosticStepId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Descrição da evidência é obrigatória.", nameof(description));

        CaseId = caseId;
        CaseIterationId = caseIterationId;
        DiagnosticStepId = diagnosticStepId;
        EvidenceType = string.IsNullOrWhiteSpace(evidenceType) ? "Other" : evidenceType.Trim();
        Description = description.Trim();
        AttachmentId = attachmentId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}

public class CaseHypothesisEvidence
{
    public long Id { get; set; }
    public long EvidenceId { get; set; }
    public long HypothesisId { get; set; }
    public EvidenceRelationType RelationType { get; set; }
    public string? Justification { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CaseHypothesisEvidence() { }

    public CaseHypothesisEvidence(
        long evidenceId,
        long hypothesisId,
        EvidenceRelationType relationType,
        string? justification = null,
        long createdBy = 1)
    {
        if (evidenceId < 0)
            throw new ArgumentException("EvidenceId inválido.", nameof(evidenceId));
        if (hypothesisId <= 0)
            throw new ArgumentException("HypothesisId inválido.", nameof(hypothesisId));
        if (createdBy <= 0)
            throw new ArgumentException("CreatedBy inválido.", nameof(createdBy));

        EvidenceId = evidenceId;
        HypothesisId = hypothesisId;
        RelationType = relationType;
        Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim();
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}
