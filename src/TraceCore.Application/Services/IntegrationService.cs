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

    public IntegrationService(
        IIntegrationRepository integrationRepository,
        IDepartmentRepository departmentRepository)
    {
        _integrationRepository = integrationRepository;
        _departmentRepository = departmentRepository;
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
            productId: command.ProductId);

        return await _integrationRepository.AddIntegrationAsync(integration, ct);
    }

    public async Task UpdateIntegrationStatusAsync(long id, string status, long? updatedBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("O status da integração é obrigatório.", nameof(status));

        var integration = await _integrationRepository.GetIntegrationByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {id} não encontrada.");

        // O status muda somente por ação manual de um usuário — nunca por uma
        // checagem automática (não há conector/health-check real).
        integration.Status = status.Trim();
        await _integrationRepository.UpdateIntegrationAsync(integration, ct);
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
            triggeredBy: command.TriggeredBy ?? "Manual");

        return await _integrationRepository.AddRunAsync(run, ct);
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
            r.TriggeredBy ?? "Manual"
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
            runDtos
        );
    }
}