using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Item de release registrado em uma ProductVersion (Fase 1 — Versionamento Inteligente).
/// Representa o que foi entregue/alterado na versão: novas funcionalidades (NewFeature),
/// melhorias (Improvement) ou correções (Fix). Não é criado por caso — um caso pode ser
/// vinculado a um ou mais itens de correção via <see cref="ProductVersionChangeCase"/>.
/// </summary>
public class ProductVersionChange
{
    public long Id { get; set; }
    public long ProductVersionId { get; set; }
    public string ChangeType { get; set; } = "Improvement"; // NewFeature, Improvement, Fix
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ComponentId { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }

    public static readonly string[] ValidChangeTypes = { "NewFeature", "Improvement", "Fix" };

    public static string Label(string? changeType)
    {
        return changeType switch
        {
            "NewFeature" => "Novidade",
            "Improvement" => "Melhoria",
            "Fix" => "Correção",
            _ => changeType ?? string.Empty
        };
    }

    public ProductVersionChange() { }

    public ProductVersionChange(long productVersionId, string changeType, string title, string? description = null, long? componentId = null, string? errorCode = null)
    {
        if (productVersionId <= 0)
            throw new ArgumentException("ProductVersionId inválido.", nameof(productVersionId));
        if (string.IsNullOrWhiteSpace(changeType))
            throw new ArgumentException("Tipo de alteração é obrigatório.", nameof(changeType));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título da alteração é obrigatório.", nameof(title));

        ProductVersionId = productVersionId;
        ChangeType = changeType.Trim();
        Title = title.Trim();
        Description = description?.Trim();
        ComponentId = componentId;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Associação N:N entre um item de release e um caso (ProductVersionChangeCase).
/// RelationType indica o papel: "FixedBy" (caso corrigido por esta alteração) ou
/// "Related". Extensível no futuro para "IntroducedBy"/"RegressionOf" — não implementado
/// nesta fase. A associação NUNCA altera Case.ProductVersionId (a versão do caso continua
/// sendo a versão em que a ocorrência aconteceu).
/// </summary>
public class ProductVersionChangeCase
{
    public long ProductVersionChangeId { get; set; }
    public long CaseId { get; set; }
    public string RelationType { get; set; } = "FixedBy"; // FixedBy, Related
    public decimal? MatchScore { get; set; }
    public string? MatchedFactorsJson { get; set; }
    public long? LinkedBy { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public static readonly string[] ValidRelationTypes = { "FixedBy", "Related" };
    public static readonly string[] ReservedRelationTypes = { "IntroducedBy", "RegressionOf" };

    public static string Label(string? relationType)
    {
        return relationType switch
        {
            "FixedBy" => "Corrigido por",
            "Related" => "Relacionado",
            _ => relationType ?? string.Empty
        };
    }

    public ProductVersionChangeCase() { }

    public ProductVersionChangeCase(long productVersionChangeId, long caseId, string relationType, decimal? matchScore = null, string? matchedFactorsJson = null, long? linkedBy = null)
    {
        if (productVersionChangeId <= 0)
            throw new ArgumentException("ProductVersionChangeId inválido.", nameof(productVersionChangeId));
        if (caseId <= 0)
            throw new ArgumentException("CaseId inválido.", nameof(caseId));
        if (string.IsNullOrWhiteSpace(relationType))
            throw new ArgumentException("RelationType é obrigatório.", nameof(relationType));

        ProductVersionChangeId = productVersionChangeId;
        CaseId = caseId;
        RelationType = relationType.Trim();
        MatchScore = matchScore;
        MatchedFactorsJson = matchedFactorsJson;
        LinkedBy = linkedBy;
        LinkedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Destinação (rollout) de uma ProductVersion para um cliente (opcionalmente limitado a
/// uma unidade). Criar o assignment não altera a versão corrente do cliente — somente a
/// confirmação de deploy (status "Deployed") atualiza ClientTechnicalContext.ProductVersionId.
/// </summary>
public class ProductVersionAssignment
{
    public long Id { get; set; }
    public long ProductVersionId { get; set; }
    public long ClientId { get; set; }
    public long? ClientUnitId { get; set; }
    public string Status { get; set; } = "Planned"; // Planned, Scheduled, Deployed, Skipped, Failed
    public DateTime PlannedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? DeployedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }

    public static readonly string[] ValidStatuses = { "Planned", "Scheduled", "Deployed", "Skipped", "Failed" };

    public static string Label(string? status)
    {
        return status switch
        {
            "Planned" => "Planejado",
            "Scheduled" => "Agendado",
            "Deployed" => "Implantado",
            "Skipped" => "Ignorado",
            "Failed" => "Falhou",
            _ => status ?? string.Empty
        };
    }

    public ProductVersionAssignment() { }

    public ProductVersionAssignment(long productVersionId, long clientId, long? clientUnitId = null, string? notes = null, long? createdBy = null)
    {
        if (productVersionId <= 0)
            throw new ArgumentException("ProductVersionId inválido.", nameof(productVersionId));
        if (clientId <= 0)
            throw new ArgumentException("ClientId inválido.", nameof(clientId));

        ProductVersionId = productVersionId;
        ClientId = clientId;
        ClientUnitId = clientUnitId;
        Notes = notes?.Trim();
        Status = "Planned";
        PlannedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Projeção de banco para detalhes de caso vinculado a alteração de versão (Fase 5).
/// </summary>
public record VersionLinkedCaseDetailDb
{
    public long ChangeId { get; init; }
    public string ChangeTitle { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
    public long CaseId { get; init; }
    public ulong CaseNumber { get; init; }
    public string CaseTitle { get; init; } = string.Empty;
    public string CaseStatus { get; init; } = string.Empty;
    public long? ClientId { get; init; }
    public string? ClientName { get; init; }
    public long? OccurredInVersionId { get; init; }
    public string? OccurredInVersionLabel { get; init; }
    public string RelationType { get; init; } = string.Empty;
    public DateTime LinkedAt { get; init; }
    public DateTime CaseOpenedAt { get; init; }

    public VersionLinkedCaseDetailDb() { }

    public VersionLinkedCaseDetailDb(
        long ChangeId,
        string ChangeTitle,
        string ChangeType,
        long CaseId,
        ulong CaseNumber,
        string CaseTitle,
        string CaseStatus,
        long? ClientId,
        string? ClientName,
        long? OccurredInVersionId,
        string? OccurredInVersionLabel,
        string RelationType,
        DateTime LinkedAt,
        DateTime CaseOpenedAt)
    {
        this.ChangeId = ChangeId;
        this.ChangeTitle = ChangeTitle;
        this.ChangeType = ChangeType;
        this.CaseId = CaseId;
        this.CaseNumber = CaseNumber;
        this.CaseTitle = CaseTitle;
        this.CaseStatus = CaseStatus;
        this.ClientId = ClientId;
        this.ClientName = ClientName;
        this.OccurredInVersionId = OccurredInVersionId;
        this.OccurredInVersionLabel = OccurredInVersionLabel;
        this.RelationType = RelationType;
        this.LinkedAt = LinkedAt;
        this.CaseOpenedAt = CaseOpenedAt;
    }
}

/// <summary>
/// Projeção de banco para caso ocorrido durante período de vigência de versão do cliente (Fase 5).
/// </summary>
public record ClientVersionCaseDb
{
    public long CaseId { get; init; }
    public ulong CaseNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public DateTime OpenedAt { get; init; }

    public ClientVersionCaseDb() { }

    public ClientVersionCaseDb(
        long CaseId,
        ulong CaseNumber,
        string Title,
        string Status,
        string? ErrorCode,
        DateTime OpenedAt)
    {
        this.CaseId = CaseId;
        this.CaseNumber = CaseNumber;
        this.Title = Title;
        this.Status = Status;
        this.ErrorCode = ErrorCode;
        this.OpenedAt = OpenedAt;
    }
}