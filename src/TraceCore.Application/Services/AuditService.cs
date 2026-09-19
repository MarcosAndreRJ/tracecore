using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly IUserRepository? _userRepository;

    public AuditService(
        IAuditEventRepository auditEventRepository,
        IUserRepository? userRepository = null)
    {
        _auditEventRepository = auditEventRepository;
        _userRepository = userRepository;
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

    public async Task<AuditSearchResultDto> SearchAsync(AuditFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await _auditEventRepository.SearchAsync(
            fromDate: filter.FromDate,
            toDate: filter.ToDate,
            actorUserId: filter.ActorUserId,
            action: filter.Action,
            entityType: filter.EntityType,
            entityId: filter.EntityId,
            searchTerm: filter.SearchTerm,
            skip: skip,
            take: pageSize,
            ct: ct);

        // Batch resolve user names if user repository is available
        var userNames = new Dictionary<long, string>();
        if (_userRepository != null)
        {
            var userIds = items
                .Where(i => i.ActorUserId.HasValue)
                .Select(i => i.ActorUserId!.Value)
                .Distinct();

            foreach (var uid in userIds)
            {
                var user = await _userRepository.GetByIdAsync(uid, ct);
                if (user != null)
                {
                    userNames[uid] = user.Name;
                }
            }
        }

        var entryDtos = items.Select(item =>
        {
            string? actorName = null;
            if (item.ActorUserId.HasValue && userNames.TryGetValue(item.ActorUserId.Value, out var name))
            {
                actorName = name;
            }

            var entityUrl = ResolveEntityUrl(item.EntityType, item.EntityId);

            return new AuditEntryDto(
                Id: item.Id,
                OccurredAt: item.OccurredAt,
                ActorUserId: item.ActorUserId,
                ActorUserName: actorName,
                ActorType: item.ActorType,
                Action: item.Action,
                EntityType: item.EntityType,
                EntityId: item.EntityId,
                CorrelationId: item.CorrelationId,
                IpAddress: item.IpAddress,
                UserAgentSummary: item.UserAgentSummary,
                BeforeJson: item.BeforeJson,
                AfterJson: item.AfterJson,
                MetadataJson: item.MetadataJson,
                EntityUrl: entityUrl
            );
        }).ToList();

        return new AuditSearchResultDto(entryDtos, totalCount, page, pageSize);
    }

    private static string? ResolveEntityUrl(string entityType, string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
            return null;

        return entityType.ToLowerInvariant() switch
        {
            "case" or "cases" => $"/Cases/Details/{entityId}",
            "knowledgeitem" or "knowledge_items" or "knowledge" => $"/Knowledge/Details/{entityId}",
            "user" or "users" => "/Users/Index",
            "department" or "departments" => "/Departments/Index",
            "client" or "clients" => "/Clients/Index",
            "component" or "components" => "/Catalog/Components/Index",
            "product" or "products" => "/Catalog/Components/Index",
            _ => null
        };
    }
}
