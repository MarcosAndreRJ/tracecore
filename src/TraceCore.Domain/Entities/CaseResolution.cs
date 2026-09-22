using System;
using System.Collections.Generic;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Registro estruturado de resolução e lição aprendida de um caso (M04 / Bloco 5.1).
/// </summary>
public class CaseResolution
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public long CaseIterationId { get; set; }

    // BR-027: Ação de resolução e validação são estritamente obrigatórias
    public string ResolutionSummary { get; set; } = string.Empty;
    public string ValidationSummary { get; set; } = string.Empty;

    // BR-028: Causa raiz opcional, mas se ausente ou não confirmada, status do caso vira NotConfirmed
    public long? RootCauseId { get; set; }
    public bool RootCauseConfirmed { get; set; } = false;

    // Departamento responsável pela falha (pré-sugerido a partir do owner do componente RootCause, mas editável)
    public long? ResponsibleDepartmentId { get; set; }

    // Requisitos de produto Bloco 5.1:
    // resolution_type: Definitive | Workaround
    public string ResolutionType { get; set; } = "Definitive";

    // recurrence_risk: Low | Medium | High
    public string RecurrenceRisk { get; set; } = "Low";
    public string? RecurrenceNotes { get; set; }

    // TODO: BR-046 / M05 — preventive_actions é o gancho estruturado que alimentará
    // a publicação de soluções reutilizáveis na Base de Conhecimento em fases futuras.
    public string? PreventiveActions { get; set; }

    // Esforço manual complementar fora do sistema (opcional)
    public int? EffortMinutes { get; set; }

    public long ResolvedBy { get; set; }
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    // Hipóteses do caso apontadas como causa raiz real investigada (pode ser mais de uma,
    // ex.: causa composta) — distinto de RootCauseId, que é a taxonomia corporativa genérica.
    public List<long> RootCauseHypothesisIds { get; set; } = new();

    public CaseResolution() { }

    public CaseResolution(
        long caseId,
        string resolutionSummary,
        string validationSummary,
        long resolvedBy,
        long? rootCauseId = null,
        bool rootCauseConfirmed = false,
        long? responsibleDepartmentId = null,
        string resolutionType = "Definitive",
        string recurrenceRisk = "Low",
        string? recurrenceNotes = null,
        string? preventiveActions = null,
        int? effortMinutes = null,
        DateTime? resolvedAt = null)
    {
        if (caseId <= 0)
            throw new ArgumentException("CaseId inválido.", nameof(caseId));

        if (string.IsNullOrWhiteSpace(resolutionSummary))
            throw new ArgumentException("A ação de resolução é obrigatória (BR-027).", nameof(resolutionSummary));

        if (string.IsNullOrWhiteSpace(validationSummary))
            throw new ArgumentException("A forma de validação é obrigatória (BR-027).", nameof(validationSummary));

        if (resolvedBy <= 0)
            throw new ArgumentException("ResolvedBy inválido.", nameof(resolvedBy));

        CaseId = caseId;
        ResolutionSummary = resolutionSummary.Trim();
        ValidationSummary = validationSummary.Trim();
        ResolvedBy = resolvedBy;
        RootCauseId = rootCauseId;
        RootCauseConfirmed = rootCauseConfirmed;
        ResponsibleDepartmentId = responsibleDepartmentId;
        ResolutionType = string.IsNullOrWhiteSpace(resolutionType) ? "Definitive" : resolutionType.Trim();
        RecurrenceRisk = string.IsNullOrWhiteSpace(recurrenceRisk) ? "Low" : recurrenceRisk.Trim();
        RecurrenceNotes = string.IsNullOrWhiteSpace(recurrenceNotes) ? null : recurrenceNotes.Trim();
        PreventiveActions = string.IsNullOrWhiteSpace(preventiveActions) ? null : preventiveActions.Trim();
        EffortMinutes = effortMinutes;
        ResolvedAt = resolvedAt ?? DateTime.UtcNow;
    }
}
