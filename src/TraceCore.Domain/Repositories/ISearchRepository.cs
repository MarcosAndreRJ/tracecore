using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public record SearchFilterCriteriaDb(
    long? ProductId = null,
    long? ProductVersionId = null,
    long? ClientId = null,
    long? ClientUnitId = null,
    long? DepartmentId = null,
    long? TechnologyId = null,
    long? ComponentId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? ErrorCode = null,
    long? RootCauseId = null,
    string? Status = null,
    string? Environment = null
);

// Bloco 7.A.0: convertidos de records posicionais para records com construtor
// vazio + propriedades `init`. Records posicionais exigem que o Dapper faça
// correspondência exata de tipo por posição do construtor a partir do schema do
// DbDataReader — colunas nullable (long?) vindas de LEFT JOIN quebram esse
// casamento porque o reader não expõe nullability, só o tipo CLR de base (Int64).
// Só é detectável testando contra um banco real (nunca falha no InMemory).
public record CaseSearchRawResult
{
    public long Id { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public string? NormalizedSummary { get; init; }
    public string OriginalReport { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public long? ProductId { get; init; }
    public string? ProductName { get; init; }
    public long? ProductVersionId { get; init; }
    public string? VersionName { get; init; }
    public long? ComponentId { get; init; }
    public string? ComponentName { get; init; }
    public long? ClientId { get; init; }
    public string? ClientName { get; init; }
    public long? ClientUnitId { get; init; }
    public string? ClientUnitName { get; init; }
    public long? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public DateTime OpenedAt { get; init; }
    public double TextScore { get; init; }
}

public record SolutionSearchRawResult
{
    public long Id { get; init; }
    public string KnowledgeCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? ProblemDescription { get; init; }
    public string? ValidationMethod { get; init; }
    public string? RiskWarning { get; init; }
    public string? RollbackPlan { get; init; }
    public string? ContentMarkdown { get; init; }
    public long? OwnerDepartmentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public long? SourceCaseId { get; init; }
    public int TotalUsages { get; init; }
    public int SuccessfulUsages { get; init; }
    public double TextScore { get; init; }
    public List<string> ApplicableEnvironments { get; init; } = new();
    public List<long> ApplicableProductIds { get; init; } = new();
    public List<long> ApplicableComponentIds { get; init; } = new();
    public List<long> NegativeProductVersionIds { get; init; } = new();
}

public record ProductSearchRawResult
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double TextScore { get; init; }
}

public record ComponentSearchRawResult
{
    public long Id { get; init; }
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Technology { get; init; }
    public double TextScore { get; init; }
}

public record RootCauseSearchRawResult
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double TextScore { get; init; }
}

public interface ISearchRepository
{
    Task<long> CreateSessionAsync(SearchSession session, CancellationToken ct = default);
    Task<long> RecordQueryAsync(SearchQueryRecord queryRecord, CancellationToken ct = default);
    Task<long> RecordInteractionAsync(SearchResultInteraction interaction, CancellationToken ct = default);
    Task MarkInteractionOpenedAsync(long interactionId, DateTime openedAt, CancellationToken ct = default);
    Task RecordFeedbackAsync(long interactionId, bool useful, CancellationToken ct = default);

    Task<IReadOnlyList<CaseSearchRawResult>> SearchCasesAsync(string query, SearchFilterCriteriaDb filters, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<SolutionSearchRawResult>> SearchSolutionsAsync(string query, SearchFilterCriteriaDb filters, bool allowDrafts = false, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSearchRawResult>> SearchProductsAsync(string query, int limit = 20, CancellationToken ct = default);
    Task<IReadOnlyList<ComponentSearchRawResult>> SearchComponentsAsync(string query, long? productId = null, int limit = 20, CancellationToken ct = default);
    Task<IReadOnlyList<RootCauseSearchRawResult>> SearchRootCausesAsync(string query, int limit = 20, CancellationToken ct = default);
}
