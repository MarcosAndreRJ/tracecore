using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditEventRepository _auditEventRepository;

    public AuditService(IAuditEventRepository auditEventRepository)
    {
        _auditEventRepository = auditEventRepository;
    }

    public async Task RecordAsync(
        string action,
        string entityType,
        string entityId,
        long? actorUserId = null,
        string actorType = "User",
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? before = null,
        object? after = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var beforeJson = SanitizeAndSerialize(before);
        var afterJson = SanitizeAndSerialize(after);
        var metadataJson = SanitizeAndSerialize(metadata);

        var auditEvent = new AuditEvent(
            action: action,
            entityType: entityType,
            entityId: entityId,
            actorUserId: actorUserId,
            actorType: actorType,
            correlationId: correlationId,
            ipAddress: ipAddress,
            userAgentSummary: Truncate(userAgent, 255),
            beforeJson: beforeJson,
            afterJson: afterJson,
            metadataJson: metadataJson
        );

        await _auditEventRepository.AddAsync(auditEvent, ct);
    }

    // BR-101: Logs de auditoria não devem armazenar senha, token, segredo ou dado sensível em texto puro.
    private static string? SanitizeAndSerialize(object? obj)
    {
        if (obj == null) return null;

        try
        {
            var jsonNode = JsonSerializer.SerializeToNode(obj);
            if (jsonNode != null)
            {
                MaskSensitiveFields(jsonNode);
                return jsonNode.ToJsonString();
            }
        }
        catch
        {
            // fallback safe representation
            return "{\"sanitized\": true}";
        }

        return null;
    }

    private static void MaskSensitiveFields(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                var key = property.Key.ToLowerInvariant();
                if (key.Contains("password") || key.Contains("token") || key.Contains("secret") || key.Contains("hash"))
                {
                    obj[property.Key] = "***REDACTED***";
                }
                else if (property.Value != null)
                {
                    MaskSensitiveFields(property.Value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item != null)
                {
                    MaskSensitiveFields(item);
                }
            }
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
