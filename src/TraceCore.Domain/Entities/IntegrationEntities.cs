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
        string status = "Configured")
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
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Log de execução registrado manualmente por um usuário (ex.: "rodei a
/// sincronização com o sistema de chamados, processou 40 registros"). Não é
/// resultado de execução automática — não há conector real em execução.
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

    public IntegrationRun() { }

    public IntegrationRun(
        long integrationId,
        string status,
        DateTime startedAt,
        long? recordsProcessed,
        string? errorMessage,
        long? recordedBy,
        DateTime? finishedAt = null)
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
    }
}