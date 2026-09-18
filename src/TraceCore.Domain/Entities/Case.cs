using System;
using System.Collections.Generic;

namespace TraceCore.Domain.Entities;

public class Case
{
    public long Id { get; set; }

    // Número visível ao usuário, estável e não reaproveitável
    public ulong CaseNumber { get; set; }

    public string? ExternalReference { get; set; }
    public string SourceType { get; set; } = "Manual";

    // Campos de contexto - Todos opcionais na abertura conforme BR-022 e FR-042
    public long? ClientId { get; set; }
    public long? ClientUnitId { get; set; }
    public long? ProductId { get; set; }
    public long? ProductVersionId { get; set; }
    public long? EnvironmentId { get; set; }

    // BR-020: Relato original é gravado uma única vez na criação e NUNCA sobrescrito.
    // Setter privado impede mutação após instanciação. Não existe método de alteração.
    public string OriginalReport { get; private set; } = string.Empty;

    // BR-021: Resumo normalizado separado do relato original, editável posteriormente
    public string? NormalizedSummary { get; private set; }

    public string? ExpectedBehavior { get; set; }
    public string? ObservedBehavior { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ScopeType { get; set; }

    // "Prioridade" (Bloco 3.1) mapeia diretamente para Severity
    public string Severity { get; set; } = "Medium";
    public string? ImpactLevel { get; set; }

    // TODO: BR-024 / BR-027 - Status inicia sempre como 'Open'. Transições e triagem formal são escopo de fases futuras.
    public string Status { get; set; } = "Open";

    // BR-022: Responsabilidade organizacional opcional na abertura
    public long? CurrentOwnerUserId { get; set; }
    public long? CurrentDepartmentId { get; set; }

    public string RootCauseStatus { get; set; } = "NotEvaluated";

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public long RowVersion { get; set; } = 1;

    // Coleções associadas (BR-023: múltiplos componentes/módulos, Bloco 7.A.3: Iterações)
    public List<CaseSymptom> Symptoms { get; set; } = new();
    public List<CaseComponent> AffectedComponents { get; set; } = new();
    public List<CaseEvidence> Evidences { get; set; } = new();
    public List<CaseIteration> Iterations { get; set; } = new();

    // Construtor sem parâmetros para Dapper/deserialização
    public Case() { }

    // Construtor principal para abertura de caso
    public Case(
        string originalReport,
        ulong caseNumber = 0,
        string severity = "Medium",
        string? impactLevel = null,
        long? clientId = null,
        long? productId = null,
        long? productVersionId = null,
        long? environmentId = null,
        string? errorCode = null,
        string? errorMessage = null,
        string? scopeType = null,
        string? externalReference = null,
        string sourceType = "Manual",
        long? currentDepartmentId = null,
        long? currentOwnerUserId = null,
        long? createdBy = null,
        DateTime? openedAt = null)
    {
        if (string.IsNullOrWhiteSpace(originalReport))
            throw new ArgumentException("Relato original da ocorrência é obrigatório.", nameof(originalReport));

        OriginalReport = originalReport.Trim();
        CaseNumber = caseNumber;
        Severity = string.IsNullOrWhiteSpace(severity) ? "Medium" : severity.Trim();
        ImpactLevel = string.IsNullOrWhiteSpace(impactLevel) ? null : impactLevel.Trim();
        ClientId = clientId;
        ProductId = productId;
        ProductVersionId = productVersionId;
        EnvironmentId = environmentId;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        ScopeType = string.IsNullOrWhiteSpace(scopeType) ? null : scopeType.Trim();
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        SourceType = string.IsNullOrWhiteSpace(sourceType) ? "Manual" : sourceType.Trim();
        CurrentDepartmentId = currentDepartmentId;
        CurrentOwnerUserId = currentOwnerUserId;
        CreatedBy = createdBy;
        OpenedAt = openedAt ?? DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = "Open";
        RootCauseStatus = "NotEvaluated";
        RowVersion = 1;

        // Bloco 7.A.3: Toda abertura de caso inicia formalmente com a Iteração 1
        Iterations.Add(new CaseIteration(Id, 1, createdBy ?? 1, "Abertura inicial do caso", OpenedAt, "Open"));
    }

    // BR-021: Permite atualizar o resumo normalizado sem jamais tocar no relato original
    public void UpdateNormalizedSummary(string? normalizedSummary, long? updatedBy = null)
    {
        NormalizedSummary = string.IsNullOrWhiteSpace(normalizedSummary) ? null : normalizedSummary.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        RowVersion++;
    }

    public void AddComponent(long componentId, string relationType = "Affected", string? confidenceLabel = null)
    {
        if (componentId <= 0)
            throw new ArgumentException("ComponentId inválido.", nameof(componentId));

        if (!AffectedComponents.Exists(c => c.ComponentId == componentId && c.RelationType == relationType))
        {
            AffectedComponents.Add(new CaseComponent(Id, componentId, relationType, confidenceLabel));
        }
    }

    public void AddSymptom(string symptomText, string? symptomCode = null, string source = "Human")
    {
        if (!string.IsNullOrWhiteSpace(symptomText))
        {
            Symptoms.Add(new CaseSymptom(Id, symptomText, symptomCode, source));
        }
    }

    public void AddEvidence(string evidenceType, string description, long? attachmentId = null, long? createdBy = null)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            var iterId = GetCurrentIteration()?.Id ?? 1;
            Evidences.Add(new CaseEvidence(Id, evidenceType, description, attachmentId, createdBy) { CaseIterationId = iterId });
        }
    }

    /// <summary>
    /// Retorna a iteração atual (aberta ou mais recente)
    /// </summary>
    public CaseIteration? GetCurrentIteration()
    {
        return Iterations.OrderByDescending(i => i.SequenceNumber).FirstOrDefault(i => i.Status == "Open")
               ?? Iterations.OrderByDescending(i => i.SequenceNumber).FirstOrDefault();
    }

    /// <summary>
    /// Reabertura formal do caso (Bloco 7.A.3 / BR-029).
    /// Só permitido a partir de 'Resolved'. Cria nova iteração (seq + 1) e muda status para 'Reopened'.
    /// Nunca toca na iteração anterior.
    /// </summary>
    public CaseIteration Reopen(string reason, long reopenedBy)
    {
        if (!string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Apenas casos com status 'Resolved' podem ser reabertos.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("O motivo da reabertura é obrigatório.", nameof(reason));

        if (reopenedBy <= 0)
            throw new ArgumentException("ReopenedBy inválido.", nameof(reopenedBy));

        int nextSeq = Iterations.Count > 0 ? Iterations.Max(i => i.SequenceNumber) + 1 : 2;
        var newIteration = new CaseIteration(Id, nextSeq, reopenedBy, reason.Trim(), DateTime.UtcNow, "Open");
        Iterations.Add(newIteration);

        Status = "Reopened";
        ResolvedAt = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = reopenedBy;
        RowVersion++;

        return newIteration;
    }

    /// <summary>
    /// Encerramento estruturado do caso (Fase 5 / Bloco 5.1 / Bloco 7.A.3).
    /// Encerra a iteração atual aberta sem alterar iterações anteriores.
    /// </summary>
    public void Resolve(string rootCauseStatus, DateTime resolvedAt, long resolvedBy)
    {
        if (string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Este caso já se encontra com status 'Resolved'. Reabra o caso antes de registrar novo encerramento.");

        var currentIteration = Iterations.OrderByDescending(i => i.SequenceNumber).FirstOrDefault(i => i.Status == "Open");
        if (currentIteration != null)
        {
            currentIteration.Status = "Resolved";
            currentIteration.ClosedAt = resolvedAt;
        }

        Status = "Resolved";
        RootCauseStatus = string.IsNullOrWhiteSpace(rootCauseStatus) ? "NotConfirmed" : rootCauseStatus;
        ResolvedAt = resolvedAt;
        UpdatedAt = resolvedAt;
        UpdatedBy = resolvedBy;
        RowVersion++;
    }

    /// <summary>
    /// Marca ou atualiza o componente responsável na tabela 'case_components' como 'RootCause' (Bloco 5.1).
    /// </summary>
    public void MarkComponentAsRootCause(long componentId)
    {
        if (componentId <= 0) return;

        var existing = AffectedComponents.Find(c => c.ComponentId == componentId);
        if (existing != null)
        {
            existing.RelationType = "RootCause";
        }
        else
        {
            AffectedComponents.Add(new CaseComponent(Id, componentId, "RootCause", "ConfirmedRootCause"));
        }
    }
}
