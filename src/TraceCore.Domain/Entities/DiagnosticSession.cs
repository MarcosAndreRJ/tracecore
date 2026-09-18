using System;
using System.Collections.Generic;
using TraceCore.Domain.Enums;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Representa uma sessão de diagnóstico de um caso (M04).
/// Uma sessão agrupa sequencialmente os passos, tentativas e verificações executados.
/// </summary>
public class DiagnosticSession
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public long CaseIterationId { get; set; }
    public string Status { get; set; } = nameof(DiagnosticSessionStatus.Open);
    public DateTime StartedAt { get; set; }
    public long StartedBy { get; set; }
    public DateTime? EndedAt { get; set; }

    public List<DiagnosticStep> Steps { get; set; } = new();

    // Construtor para deserialização e mapeamento Dapper
    public DiagnosticSession() { }

    public DiagnosticSession(long caseId, long startedBy, DateTime? startedAt = null)
    {
        if (caseId <= 0)
            throw new ArgumentException("O ID do caso deve ser válido.", nameof(caseId));
        if (startedBy <= 0)
            throw new ArgumentException("O usuário iniciador da sessão deve ser válido.", nameof(startedBy));

        CaseId = caseId;
        StartedBy = startedBy;
        StartedAt = startedAt ?? DateTime.UtcNow;
        Status = nameof(DiagnosticSessionStatus.Open);
    }

    public void Close(DateTime? endedAt = null)
    {
        Status = nameof(DiagnosticSessionStatus.Closed);
        EndedAt = endedAt ?? DateTime.UtcNow;
    }
}
