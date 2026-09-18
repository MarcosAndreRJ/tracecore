using System;

namespace TraceCore.Domain.Entities;

public class CaseComponent
{
    public long CaseId { get; set; }
    public long ComponentId { get; set; }
    // BR-023: Na abertura, relation_type default é 'Affected'. Outros tipos (Suspected, RootCause) são de fases futuras.
    public string RelationType { get; set; } = "Affected";
    public string? ConfidenceLabel { get; set; }

    public CaseComponent() { }

    public CaseComponent(long caseId, long componentId, string relationType = "Affected", string? confidenceLabel = null)
    {
        if (componentId <= 0)
            throw new ArgumentException("ComponentId inválido.", nameof(componentId));

        CaseId = caseId;
        ComponentId = componentId;
        RelationType = string.IsNullOrWhiteSpace(relationType) ? "Affected" : relationType.Trim();
        ConfidenceLabel = string.IsNullOrWhiteSpace(confidenceLabel) ? null : confidenceLabel.Trim();
    }
}
