using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Registro/catálogo administrativo de integrações (M10). Não representa um
/// conector real em execução — ADR-P005/P010 seguem abertas e nenhum sistema
/// externo está conectado. Estados mudam somente por ação manual de um usuário.
/// </summary>
public class Integration
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Catálogo aberto (string, não enum fechado): Ticketing, Sap, Monitoring,
    // Telemetry, Directory, Notification, Repository, Other.
    public string IntegrationType { get; set; } = "Other";

    public string? TargetSystemDescription { get; set; }

    // Configured, Active, Inactive ou Error — somente via ação manual.
    public string Status { get; set; } = "Configured";

    public long? OwnerDepartmentId { get; set; }

    // Documenta o isolamento e contrato próprio exigido pelo M10 para cada
    // conector, mesmo que o conector ainda não exista de fato.
    public string? ContractNotes { get; set; }

    // Campos de health-check configuráveis (Fase 15 / M10)
    public string? HealthCheckUrl { get; set; }
    public string HealthCheckMethod { get; set; } = "Http"; // Http, Tcp
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
    public int? HealthCheckExpectedStatusCode { get; set; } = 200;

    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Propriedades auxiliares para exibição (não mapeadas ou preenchidas por query)
    public string? OwnerDepartmentName { get; set; }

    public Integration() { }

    public Integration(
        string code,
        string name,
        string integrationType,
        string? targetSystemDescription,
        long? ownerDepartmentId,
        string? contractNotes,
        long? createdBy,
        string status = "Configured",
        string? healthCheckUrl = null,
        string healthCheckMethod = "Http",
        int healthCheckTimeoutSeconds = 5,
        int? healthCheckExpectedStatusCode = 200)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código da integração é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da integração é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(integrationType))
            throw new ArgumentException("Tipo da integração é obrigatório.", nameof(integrationType));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IntegrationType = integrationType.Trim();
        TargetSystemDescription = targetSystemDescription?.Trim();
        OwnerDepartmentId = ownerDepartmentId;
        ContractNotes = contractNotes?.Trim();
        CreatedBy = createdBy;
        Status = string.IsNullOrWhiteSpace(status) ? "Configured" : status.Trim();
        HealthCheckUrl = healthCheckUrl?.Trim();
        HealthCheckMethod = string.IsNullOrWhiteSpace(healthCheckMethod) ? "Http" : healthCheckMethod.Trim();
        HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds > 0 ? healthCheckTimeoutSeconds : 5;
        HealthCheckExpectedStatusCode = healthCheckExpectedStatusCode;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Log de execução registrado manualmente por um usuário ou automaticamente pelo
/// serviço de verificação (Fase 15 / M10).
/// </summary>
public class IntegrationRun
{
    public long Id { get; set; }
    public long IntegrationId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Success, Failed ou Partial
    public string Status { get; set; } = "Success";

    public long? RecordsProcessed { get; set; }
    public string? ErrorMessage { get; set; }

    public long? RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Distingue log manual de execução automática de verificação (Fase 15)
    public string TriggeredBy { get; set; } = "Manual"; // "Manual", "Automated"

    public IntegrationRun() { }

    public IntegrationRun(
        long integrationId,
        string status,
        DateTime startedAt,
        long? recordsProcessed,
        string? errorMessage,
        long? recordedBy,
        DateTime? finishedAt = null,
        string triggeredBy = "Manual")
    {
        if (integrationId <= 0)
            throw new ArgumentException("IntegrationId inválido.", nameof(integrationId));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status da execução é obrigatório.", nameof(status));

        IntegrationId = integrationId;
        Status = status.Trim();
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        RecordsProcessed = recordsProcessed;
        ErrorMessage = errorMessage?.Trim();
        RecordedBy = recordedBy;
        RecordedAt = DateTime.UtcNow;
        TriggeredBy = string.IsNullOrWhiteSpace(triggeredBy) ? "Manual" : triggeredBy.Trim();
    }
}