using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// BR-004 / BR-100: Registro de auditoria imutável em nível de aplicação.
/// TODO: BR-004, BR-100 e M09 - Infraestrutura completa de Outbox e processamento assíncrono de eventos de auditoria será expandida nas fases subsequentes.
/// </summary>
public class AuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public long? ActorUserId { get; set; }
    public string ActorType { get; set; } = "User"; // "User", "System"
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgentSummary { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? MetadataJson { get; set; }

    public AuditEvent() { }

    public AuditEvent(
        string action,
        string entityType,
        string entityId,
        long? actorUserId = null,
        string actorType = "User",
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgentSummary = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? metadataJson = null)
    {
        OccurredAt = DateTime.UtcNow;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        ActorUserId = actorUserId;
        ActorType = actorType;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        UserAgentSummary = userAgentSummary;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        MetadataJson = metadataJson;
    }
}
