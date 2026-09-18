using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAuditService _auditService;

    public ClientService(
        IClientRepository clientRepository,
        ICatalogRepository catalogRepository,
        IAuditService auditService)
    {
        _clientRepository = clientRepository;
        _catalogRepository = catalogRepository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<ClientDto>> GetAllClientsAsync(CancellationToken ct = default)
    {
        var clients = await _clientRepository.GetAllAsync(ct);
        var result = new List<ClientDto>();

        foreach (var c in clients)
        {
            var units = await _clientRepository.GetUnitsByClientIdAsync(c.Id, ct);
            var contexts = await _clientRepository.GetTechnicalContextsByClientIdAsync(c.Id, ct);

            result.Add(new ClientDto(
                c.Id,
                c.Code,
                c.Name,
                c.Status,
                c.ExternalCrmId,
                c.Notes,
                c.CreatedAt,
                units.Count,
                contexts.Count
            ));
        }

        return result;
    }

    public async Task<ClientDetailsDto?> GetClientDetailsAsync(long clientId, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        if (client == null) return null;

        var units = await _clientRepository.GetUnitsByClientIdAsync(clientId, ct);
        var contexts = await _clientRepository.GetTechnicalContextsByClientIdAsync(clientId, ct);

        var products = (await _catalogRepository.GetAllProductsAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        var environments = (await _catalogRepository.GetAllEnvironmentsAsync(ct)).ToDictionary(e => e.Id, e => e.Name);

        var unitDtos = units.Select(u => new ClientUnitDto(
            u.Id,
            u.ClientId,
            u.Code,
            u.Name,
            u.Status,
            u.ExternalCrmId,
            u.CreatedAt
        )).ToList();

        var contextDtos = new List<ClientTechnicalContextDto>();
        foreach (var c in contexts)
        {
            string? unitName = c.ClientUnitId.HasValue ? units.FirstOrDefault(u => u.Id == c.ClientUnitId.Value)?.Name : null;
            string productName = products.TryGetValue(c.ProductId, out var pName) ? pName : $"Produto #{c.ProductId}";
            string? versionLabel = null;
            if (c.ProductVersionId.HasValue)
            {
                var v = await _catalogRepository.GetProductVersionByIdAsync(c.ProductVersionId.Value, ct);
                versionLabel = v?.VersionLabel;
            }
            string? envName = c.EnvironmentId.HasValue && environments.TryGetValue(c.EnvironmentId.Value, out var eName) ? eName : null;

            contextDtos.Add(new ClientTechnicalContextDto(
                c.Id,
                c.ClientId,
                c.ClientUnitId,
                unitName,
                c.ProductId,
                productName,
                c.ProductVersionId,
                versionLabel,
                c.EnvironmentId,
                envName,
                c.Status,
                c.EffectiveFrom,
                c.EffectiveTo,
                c.CreatedAt
            ));
        }

        return new ClientDetailsDto(
            client.Id,
            client.Code,
            client.Name,
            client.Status,
            client.ExternalCrmId,
            client.Notes,
            client.CreatedAt,
            unitDtos,
            contextDtos
        );
    }

    public async Task<long> CreateClientAsync(CreateClientRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleValidationException("BR-CLIENT-001", "O nome do cliente é obrigatório.");

        if (!string.IsNullOrWhiteSpace(request.ExternalCrmId))
        {
            var existing = await _clientRepository.GetByExternalCrmIdAsync(request.ExternalCrmId.Trim(), ct);
            if (existing != null)
            {
                throw new ConflictException($"Já existe um cliente cadastrado com o CRM ID '{request.ExternalCrmId.Trim()}'.");
            }
        }

        var client = new Client(request.Name, request.Code, "Active", request.ExternalCrmId, request.Notes);
        var clientId = await _clientRepository.AddAsync(client, ct);

        await _auditService.RecordAsync(
            action: "client.create",
            entityType: "clients",
            entityId: clientId.ToString(),
            actorUserId: currentUserId,
            after: new { client.Id, client.Name, client.Code, client.ExternalCrmId },
            ct: ct
        );

        return clientId;
    }

    public async Task UpdateClientAsync(long clientId, UpdateClientRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        if (client == null)
            throw new EntityNotFoundException("Cliente", clientId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleValidationException("BR-CLIENT-001", "O nome do cliente é obrigatório.");

        if (!string.IsNullOrWhiteSpace(request.ExternalCrmId))
        {
            var existing = await _clientRepository.GetByExternalCrmIdAsync(request.ExternalCrmId.Trim(), ct);
            if (existing != null && existing.Id != clientId)
            {
                throw new ConflictException($"Já existe outro cliente cadastrado com o CRM ID '{request.ExternalCrmId.Trim()}'.");
            }
        }

        var before = new { client.Name, client.Code, client.Status, client.ExternalCrmId, client.Notes };

        client.Name = request.Name.Trim();
        client.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        client.Status = request.Status;
        client.ExternalCrmId = string.IsNullOrWhiteSpace(request.ExternalCrmId) ? null : request.ExternalCrmId.Trim();
        client.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _clientRepository.UpdateAsync(client, ct);

        await _auditService.RecordAsync(
            action: "client.update",
            entityType: "clients",
            entityId: clientId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { client.Name, client.Code, client.Status, client.ExternalCrmId, client.Notes },
            ct: ct
        );
    }

    public async Task<long> AddUnitAsync(long clientId, CreateClientUnitRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        if (client == null)
            throw new EntityNotFoundException("Cliente", clientId);

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BusinessRuleValidationException("BR-CLIENT-003", "O código da unidade é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleValidationException("BR-CLIENT-003", "O nome da unidade é obrigatório.");

        var unit = new ClientUnit(clientId, request.Code, request.Name, "Active", request.ExternalCrmId, currentUserId);
        var unitId = await _clientRepository.AddUnitAsync(unit, ct);

        await _auditService.RecordAsync(
            action: "client.unit_create",
            entityType: "client_units",
            entityId: unitId.ToString(),
            actorUserId: currentUserId,
            after: new { unit.Id, unit.ClientId, unit.Code, unit.Name, unit.ExternalCrmId },
            ct: ct
        );

        return unitId;
    }

    public async Task<long> AddTechnicalContextAsync(long clientId, CreateTechnicalContextRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        if (client == null)
            throw new EntityNotFoundException("Cliente", clientId);

        if (request.ProductId <= 0)
            throw new BusinessRuleValidationException("BR-CLIENT-004", "O produto técnico é obrigatório.");

        var product = await _catalogRepository.GetProductByIdAsync(request.ProductId, ct);
        if (product == null)
            throw new EntityNotFoundException("Produto", request.ProductId);

        if (request.ProductVersionId.HasValue)
        {
            var version = await _catalogRepository.GetProductVersionByIdAsync(request.ProductVersionId.Value, ct);
            if (version == null || version.ProductId != request.ProductId)
            {
                throw new BusinessRuleValidationException("BR-CLIENT-001", "A versão de produto informada não pertence ao produto selecionado.");
            }
        }

        if (request.ClientUnitId.HasValue)
        {
            var unit = await _clientRepository.GetUnitByIdAsync(request.ClientUnitId.Value, ct);
            if (unit == null || unit.ClientId != clientId)
            {
                throw new BusinessRuleValidationException("BR-CLIENT-002", "A unidade selecionada não pertence ao cliente informado.");
            }
        }

        var context = new ClientTechnicalContext(
            clientId: clientId,
            productId: request.ProductId,
            clientUnitId: request.ClientUnitId,
            productVersionId: request.ProductVersionId,
            environmentId: request.EnvironmentId,
            status: request.Status,
            effectiveFrom: request.EffectiveFrom ?? DateTime.UtcNow,
            effectiveTo: request.EffectiveTo,
            createdBy: currentUserId
        );

        var contextId = await _clientRepository.AddTechnicalContextAsync(context, ct);

        await _auditService.RecordAsync(
            action: "client.technical_context_create",
            entityType: "client_technical_contexts",
            entityId: contextId.ToString(),
            actorUserId: currentUserId,
            after: new { context.Id, context.ClientId, context.ProductId, context.ProductVersionId, context.EnvironmentId },
            ct: ct
        );

        return contextId;
    }
}
