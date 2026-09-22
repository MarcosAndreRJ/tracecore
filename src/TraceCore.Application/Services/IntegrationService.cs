using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class IntegrationService : IIntegrationService
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditService _auditService;

    public IntegrationService(
        IIntegrationRepository integrationRepository,
        IDepartmentRepository departmentRepository,
        IAuditService auditService)
    {
        _integrationRepository = integrationRepository;
        _departmentRepository = departmentRepository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<IntegrationDto>> GetIntegrationsAsync(CancellationToken ct = default)
    {
        var integrations = await _integrationRepository.GetAllIntegrationsAsync(ct);
        var result = new List<IntegrationDto>();

        foreach (var integration in integrations)
        {
            var runs = await _integrationRepository.GetRunsByIntegrationIdAsync(integration.Id, ct);
            result.Add(ToDto(integration, runs));
        }

        return result;
    }

    public async Task<IntegrationDto?> GetIntegrationByIdAsync(long id, CancellationToken ct = default)
    {
        var integration = await _integrationRepository.GetIntegrationByIdAsync(id, ct);
        if (integration == null)
            return null;

        var runs = await _integrationRepository.GetRunsByIntegrationIdAsync(id, ct);
        return ToDto(integration, runs);
    }

    public async Task<long> CreateIntegrationAsync(CreateIntegrationCommand command, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados da integração são obrigatórios.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new ArgumentException("O código da integração é obrigatório.", nameof(command.Code));
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("O nome da integração é obrigatório.", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(command.IntegrationType))
            throw new ArgumentException("O tipo de integração é obrigatório.", nameof(command.IntegrationType));

        ValidateStructuralFields(command.Responsibility, command.HostingLocation, command.Direction);

        if (command.OwnerDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(command.OwnerDepartmentId.Value, ct);
            if (dept == null)
                throw new ArgumentException($"Departamento com ID {command.OwnerDepartmentId.Value} não existe.", nameof(command.OwnerDepartmentId));
        }

        // Toda integração entra como catálogo 'Configured' — nunca como conexão ativa.
        var integration = new Integration(
            command.Code,
            command.Name,
            command.IntegrationType,
            command.TargetSystemDescription,
            command.OwnerDepartmentId,
            command.ContractNotes,
            command.CreatedBy,
            status: "Configured",
            healthCheckUrl: command.HealthCheckUrl,
            healthCheckMethod: command.HealthCheckMethod,
            healthCheckTimeoutSeconds: command.HealthCheckTimeoutSeconds,
            healthCheckExpectedStatusCode: command.HealthCheckExpectedStatusCode,
            productId: command.ProductId,
            responsibility: command.Responsibility,
            hostingLocation: command.HostingLocation,
            direction: command.Direction);

        var id = await _integrationRepository.AddIntegrationAsync(integration, ct);
        integration.Id = id;

        await _auditService.RecordAsync(
            action: "integration.create",
            entityType: "integrations",
            entityId: id.ToString(),
            actorUserId: command.CreatedBy,
            after: new
            {
                integration.Code,
                integration.Name,
                integration.IntegrationType,
                integration.Responsibility,
                integration.HostingLocation,
                integration.Direction,
                integration.Status,
            },
            ct: ct
        );

        return id;
    }

    // Edição completa da integração (Fase 02 — Ajuste do Ecossistema). Reaproveita o
    // repositório já existente (Snapshot/UPDATE de todos os campos). Não toca em
    // Status nem Health-Check — essas têm operações próprias (status manual e
    // configure health-check).
    public async Task UpdateIntegrationAsync(UpdateIntegrationCommand command, long? currentUserId = null, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados da integração são obrigatórios.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new ArgumentException("O código da integração é obrigatório.", nameof(command.Code));
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("O nome da integração é obrigatório.", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(command.IntegrationType))
            throw new ArgumentException("O tipo de integração é obrigatório.", nameof(command.IntegrationType));

        ValidateStructuralFields(command.Responsibility, command.HostingLocation, command.Direction);

        if (command.OwnerDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(command.OwnerDepartmentId.Value, ct);
            if (dept == null)
                throw new ArgumentException($"Departamento com ID {command.OwnerDepartmentId.Value} não existe.", nameof(command.OwnerDepartmentId));
        }

        var integration = await _integrationRepository.GetIntegrationByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {command.Id} não encontrada.");

        var before = new
        {
            integration.Id,
            integration.Code,
            integration.Name,
            integration.IntegrationType,
            integration.ProductId,
            integration.TargetSystemDescription,
            integration.OwnerDepartmentId,
            integration.ContractNotes,
            integration.Responsibility,
            integration.HostingLocation,
            integration.Direction,
        };

        integration.Code = command.Code.Trim().ToUpperInvariant();
        integration.Name = command.Name.Trim();
        integration.IntegrationType = command.IntegrationType.Trim();
        integration.ProductId = command.ProductId;
        integration.TargetSystemDescription = string.IsNullOrWhiteSpace(command.TargetSystemDescription) ? null : command.TargetSystemDescription.Trim();
        integration.OwnerDepartmentId = command.OwnerDepartmentId;
        integration.ContractNotes = string.IsNullOrWhiteSpace(command.ContractNotes) ? null : command.ContractNotes.Trim();
        integration.Responsibility = string.IsNullOrWhiteSpace(command.Responsibility) ? null : command.Responsibility.Trim();
        integration.HostingLocation = string.IsNullOrWhiteSpace(command.HostingLocation) ? null : command.HostingLocation.Trim();
        integration.Direction = string.IsNullOrWhiteSpace(command.Direction) ? null : command.Direction.Trim();

        await _integrationRepository.UpdateIntegrationAsync(integration, ct);

        await _auditService.RecordAsync(
            action: "integration.update",
            entityType: "integrations",
            entityId: command.Id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new
            {
                integration.Id,
                integration.Code,
                integration.Name,
                integration.IntegrationType,
                integration.ProductId,
                integration.TargetSystemDescription,
                integration.OwnerDepartmentId,
                integration.ContractNotes,
                integration.Responsibility,
                integration.HostingLocation,
                integration.Direction,
            },
            ct: ct
        );
    }

    // Desvincula a integração do sistema (ProductId = NULL) preservando a integração
    // e todo o seu histórico de execuções/verificações (Fase 02).
    public async Task UnlinkIntegrationFromProductAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var integration = await _integrationRepository.GetIntegrationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {id} não encontrada.");

        if (!integration.ProductId.HasValue)
            return; // já desvinculada — nada a fazer (sem ruído de auditoria).

        var beforeProductId = integration.ProductId;
        integration.ProductId = null;
        await _integrationRepository.UpdateIntegrationAsync(integration, ct);

        await _auditService.RecordAsync(
            action: "integration.unlink_from_product",
            entityType: "integrations",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: new { productId = beforeProductId },
            after: new { productId = (long?)null },
            ct: ct
        );
    }

    // Vincula uma integração existente a um sistema (ProductId) preservando a
    // integração e todo o seu histórico de execuções/verificações (Fase 04).
    // Simétrico ao Unlink IntegrationFromProductAsync e idempotente.
    public async Task LinkIntegrationToProductAsync(long id, long productId, long? currentUserId = null, CancellationToken ct = default)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));

        var integration = await _integrationRepository.GetIntegrationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {id} não encontrada.");

        if (integration.ProductId == productId)
            return; // já vinculada ao mesmo sistema — nada a fazer.

        var beforeProductId = integration.ProductId;
        integration.ProductId = productId;
        await _integrationRepository.UpdateIntegrationAsync(integration, ct);

        await _auditService.RecordAsync(
            action: "integration.link_to_product",
            entityType: "integrations",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: new { productId = beforeProductId },
            after: new { productId = (long?)productId },
            ct: ct
        );
    }

    private static void ValidateStructuralFields(string? responsibility, string? hostingLocation, string? direction)
    {
        if (responsibility != null && !Integration.ValidResponsibilities.Contains(responsibility, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Responsabilidade inválida: '{responsibility}'. Valores aceitos: {string.Join(", ", Integration.ValidResponsibilities)}.", nameof(responsibility));
        if (hostingLocation != null && !Integration.ValidHostingLocations.Contains(hostingLocation, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Hospedagem/Local de execução inválido: '{hostingLocation}'. Valores aceitos: {string.Join(", ", Integration.ValidHostingLocations)}.", nameof(hostingLocation));
        if (direction != null && !Integration.ValidDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Direção inválida: '{direction}'. Valores aceitos: {string.Join(", ", Integration.ValidDirections)}.", nameof(direction));
    }

    public async Task UpdateIntegrationStatusAsync(long id, string status, long? updatedBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("O status da integração é obrigatório.", nameof(status));

        var integration = await _integrationRepository.GetIntegrationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {id} não encontrada.");

        // O status muda somente por ação manual de um usuário — nunca por uma
        // checagem automática (não há conector/health-check real).
        var beforeStatus = integration.Status;
        integration.Status = status.Trim();
        await _integrationRepository.UpdateIntegrationAsync(integration, ct);

        await _auditService.RecordAsync(
            action: "integration.status_update",
            entityType: "integrations",
            entityId: id.ToString(),
            actorUserId: updatedBy,
            before: new { status = beforeStatus },
            after: new { status = integration.Status },
            ct: ct
        );
    }

    public async Task ConfigureHealthCheckAsync(ConfigureIntegrationHealthCheckCommand command, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados de configuração são obrigatórios.", nameof(command));

        var integration = await _integrationRepository.GetIntegrationByIdAsync(command.IntegrationId, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {command.IntegrationId} não encontrada.");

        integration.HealthCheckUrl = string.IsNullOrWhiteSpace(command.HealthCheckUrl) ? null : command.HealthCheckUrl.Trim();
        integration.HealthCheckMethod = string.IsNullOrWhiteSpace(command.HealthCheckMethod) ? "Http" : command.HealthCheckMethod.Trim();
        integration.HealthCheckTimeoutSeconds = command.HealthCheckTimeoutSeconds > 0 ? command.HealthCheckTimeoutSeconds : 5;
        integration.HealthCheckExpectedStatusCode = command.HealthCheckExpectedStatusCode;

        await _integrationRepository.UpdateIntegrationAsync(integration, ct);

        await _auditService.RecordAsync(
            action: "integration.healthcheck_configure",
            entityType: "integrations",
            entityId: command.IntegrationId.ToString(),
            actorUserId: command.UpdatedBy,
            after: new
            {
                integration.HealthCheckUrl,
                integration.HealthCheckMethod,
                integration.HealthCheckTimeoutSeconds,
                integration.HealthCheckExpectedStatusCode,
            },
            ct: ct
        );
    }

    public async Task<long> RegisterRunAsync(RegisterIntegrationRunCommand command, CancellationToken ct = default)
    {
        if (command == null)
            throw new ArgumentException("Dados da execução são obrigatórios.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Status))
            throw new ArgumentException("O status da execução é obrigatório.", nameof(command.Status));

        var integration = await _integrationRepository.GetIntegrationByIdAsync(command.IntegrationId, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {command.IntegrationId} não encontrada.");

        var startedAt = command.StartedAt ?? DateTime.UtcNow;
        var run = new IntegrationRun(
            integration.Id,
            command.Status,
            startedAt,
            command.RecordsProcessed,
            command.ErrorMessage,
            command.RecordedBy,
            finishedAt: DateTime.UtcNow,
            triggeredBy: command.TriggeredBy ?? "Manual")
        {
            // Fase 05 — execução registrada manualmente (Registrar Execução) é administrativa
            RunContext = "Administrative"
        };

        var runId = await _integrationRepository.AddRunAsync(run, ct);

        await _auditService.RecordAsync(
            action: "integration.run_register",
            entityType: "integration_runs",
            entityId: runId.ToString(),
            actorUserId: command.RecordedBy,
            after: new
            {
                integrationId = integration.Id,
                statusRun = run.Status,
                run.RecordsProcessed,
                run.TriggeredBy,
            },
            ct: ct
        );

        return runId;
    }

    private static IntegrationDto ToDto(Integration integration, IReadOnlyList<IntegrationRun> runs)
    {
        var runDtos = runs.Select(r => new IntegrationRunDto(
            r.Id,
            r.IntegrationId,
            r.StartedAt,
            r.FinishedAt,
            r.Status,
            r.RecordsProcessed,
            r.ErrorMessage,
            r.RecordedBy,
            r.RecordedAt,
            r.TriggeredBy ?? "Manual",
            r.RunContext,
            r.CaseId
        )).ToList();

        return new IntegrationDto(
            integration.Id,
            integration.Code,
            integration.Name,
            integration.IntegrationType,
            integration.TargetSystemDescription,
            integration.Status,
            integration.OwnerDepartmentId,
            integration.OwnerDepartmentName,
            integration.ProductId,
            integration.ContractNotes,
            integration.HealthCheckUrl,
            integration.HealthCheckMethod,
            integration.HealthCheckTimeoutSeconds,
            integration.HealthCheckExpectedStatusCode,
            integration.CreatedBy,
            integration.CreatedAt,
            integration.UpdatedAt,
            runDtos,
            integration.Responsibility,
            integration.HostingLocation,
            integration.Direction
        );
    }

    public async Task<IReadOnlyList<IntegrationType>> GetIntegrationTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        return await _integrationRepository.GetIntegrationTypesAsync(includeInactive, ct);
    }

    public async Task<long> CreateIntegrationTypeAsync(string code, string name, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("O código do tipo de integração é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do tipo de integração é obrigatório.", nameof(name));

        var type = new IntegrationType(code, name);
        var id = await _integrationRepository.AddIntegrationTypeAsync(type, ct);
        type.Id = id;

        await _auditService.RecordAsync(
            action: "integration_type.create",
            entityType: "integration_types",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { type.Id, type.Code, type.Name, type.IsActive },
            ct: ct
        );

        return id;
    }

    public async Task DeactivateIntegrationTypeAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var type = await _integrationRepository.GetIntegrationTypeByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Tipo de integração com ID {id} não encontrado.");

        if (!type.IsActive)
            return;

        if (await IsIntegrationTypeInUseAsync(type, ct))
        {
            throw new InvalidOperationException(
                $"O tipo de integração '{type.Name}' não pode ser inativado porque já está em uso por integrações cadastradas.");
        }

        var before = new { type.Id, type.Code, type.Name, type.IsActive };
        type.IsActive = false;
        await _integrationRepository.UpdateIntegrationTypeAsync(type, ct);

        await _auditService.RecordAsync(
            action: "integration_type.deactivate",
            entityType: "integration_types",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { type.Id, type.Code, type.Name, type.IsActive },
            ct: ct
        );
    }

    private async Task<bool> IsIntegrationTypeInUseAsync(IntegrationType type, CancellationToken ct)
    {
        var integrations = await _integrationRepository.GetAllIntegrationsAsync(ct);
        return integrations.Any(i => string.Equals(i.IntegrationType, type.Code, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(i.IntegrationType, type.Name, StringComparison.OrdinalIgnoreCase));
    }
}