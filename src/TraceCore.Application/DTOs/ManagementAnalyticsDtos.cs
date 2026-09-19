using System;
using System.Collections.Generic;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.DTOs;

public class AnalyticsFilterDto
{
    public string Period { get; set; } = "30d"; // 7d, 30d, 90d, 6m, 12m, custom, all
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public long? ClientId { get; set; }
    public long? ClientUnitId { get; set; }
    public long? ProductId { get; set; }
    public long? ProductVersionId { get; set; }
    public long? EnvironmentId { get; set; }
    public long? ComponentId { get; set; }
    public long? DepartmentId { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public string Grouping { get; set; } = "day"; // day, week, month

    public AnalyticsFilterCriteria ToCriteria(DateTime? computedFrom = null, DateTime? computedTo = null)
    {
        return new AnalyticsFilterCriteria
        {
            FromUtc = computedFrom ?? StartDate,
            ToUtc = computedTo ?? EndDate,
            ClientId = ClientId,
            ClientUnitId = ClientUnitId,
            ProductId = ProductId,
            ProductVersionId = ProductVersionId,
            EnvironmentId = EnvironmentId,
            ComponentId = ComponentId,
            DepartmentId = DepartmentId,
            Severity = Severity,
            Status = Status
        };
    }

    public string ToQueryString(string? extraParam = null)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(Period)) parameters.Add($"Period={Uri.EscapeDataString(Period)}");
        if (StartDate.HasValue) parameters.Add($"StartDate={StartDate.Value:yyyy-MM-dd}");
        if (EndDate.HasValue) parameters.Add($"EndDate={EndDate.Value:yyyy-MM-dd}");
        if (ClientId.HasValue) parameters.Add($"ClientId={ClientId.Value}");
        if (ClientUnitId.HasValue) parameters.Add($"ClientUnitId={ClientUnitId.Value}");
        if (ProductId.HasValue) parameters.Add($"ProductId={ProductId.Value}");
        if (ProductVersionId.HasValue) parameters.Add($"ProductVersionId={ProductVersionId.Value}");
        if (EnvironmentId.HasValue) parameters.Add($"EnvironmentId={EnvironmentId.Value}");
        if (ComponentId.HasValue) parameters.Add($"ComponentId={ComponentId.Value}");
        if (DepartmentId.HasValue) parameters.Add($"DepartmentId={DepartmentId.Value}");
        if (!string.IsNullOrWhiteSpace(Severity)) parameters.Add($"Severity={Uri.EscapeDataString(Severity)}");
        if (!string.IsNullOrWhiteSpace(Status)) parameters.Add($"Status={Uri.EscapeDataString(Status)}");
        if (!string.IsNullOrWhiteSpace(extraParam)) parameters.Add(extraParam);

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;
    }
}

public class MetricItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string FormattedValue { get; set; } = "0";
    public string Unit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double? PreviousPeriodValue { get; set; }
    public double? TrendPercentage { get; set; }
    public string TrendDirection { get; set; } = "neutral"; // up, down, neutral
    public bool HasSufficientData { get; set; } = false;
    public string DrillDownUrl { get; set; } = string.Empty;
}

public class TrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int OpenedCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class FrequencyItemDto
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SecondaryText { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string DrillDownUrl { get; set; } = string.Empty;
}

public class AttentionCaseDto
{
    public long Id { get; set; }
    public ulong CaseNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public int ActiveDays { get; set; }
    public int IterationCount { get; set; }
    public bool HasRootCause { get; set; }
    public bool HasKnowledge { get; set; }
    public string AttentionReason { get; set; } = string.Empty;
    public string CaseUrl => $"/Cases/Details/{Id}";
}

public class ManagementOverviewDto
{
    public AnalyticsFilterDto Filter { get; set; } = new();
    
    public MetricItemDto OpenCasesMetric { get; set; } = new();
    public MetricItemDto ResolvedCasesMetric { get; set; } = new();
    public MetricItemDto MttrMetric { get; set; } = new();
    public MetricItemDto MttrMedianMetric { get; set; } = new();
    public MetricItemDto RecurrentCasesMetric { get; set; } = new();
    public MetricItemDto UnconfirmedRootCauseMetric { get; set; } = new();
    public MetricItemDto UndocumentedKnowledgeMetric { get; set; } = new();

    public IReadOnlyList<TrendPointDto> TimeEvolution { get; set; } = [];
    public IReadOnlyList<FrequencyItemDto> TopProducts { get; set; } = [];
    public IReadOnlyList<FrequencyItemDto> TopComponents { get; set; } = [];
    public IReadOnlyList<FrequencyItemDto> TopRootCauses { get; set; } = [];
    public IReadOnlyList<FrequencyItemDto> TopRecurrences { get; set; } = [];
    public IReadOnlyList<AttentionCaseDto> AttentionCases { get; set; } = [];
}

public class DepartmentSummaryDto
{
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ActiveCasesCount { get; set; }
    public int ResolvedCasesCount { get; set; }
    public int ReopenedCasesCount { get; set; }
    public int RecurrentCasesCount { get; set; }
    public int KnowledgeCreatedCount { get; set; }
    public int DiagnosticStepsCount { get; set; }
    public double MttrMinutes { get; set; }
    public string FormattedMttr { get; set; } = "N/D";
    public double MttrMedianMinutes { get; set; }
    public string FormattedMttrMedian { get; set; } = "N/D";
    public string DrillDownUrl { get; set; } = string.Empty;
}

public class DepartmentAnalyticsDto
{
    public AnalyticsFilterDto Filter { get; set; } = new();
    public IReadOnlyList<DepartmentSummaryDto> Departments { get; set; } = [];
}

public class UserSummaryDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int OpenedCasesCount { get; set; }
    public int ResolvedCasesCount { get; set; }
    public int DiagnosticStepsCount { get; set; }
    public int HypothesesCreatedCount { get; set; }
    public int EvidencesCreatedCount { get; set; }
    public int KnowledgeAuthoredCount { get; set; }
    public int KnowledgeUsedCount { get; set; }
    public string? EmergingFocusArea { get; set; }
    public string DrillDownUrl { get; set; } = string.Empty;
}

public class UserAnalyticsDto
{
    public AnalyticsFilterDto Filter { get; set; } = new();
    public IReadOnlyList<UserSummaryDto> Users { get; set; } = [];
}

public class KnowledgeAnalyticsDto
{
    public AnalyticsFilterDto Filter { get; set; } = new();
    public MetricItemDto TotalPublishedMetric { get; set; } = new();
    public MetricItemDto NeverReviewedMetric { get; set; } = new();
    public MetricItemDto ReviewOverdueMetric { get; set; } = new();
    public MetricItemDto TotalUsagesMetric { get; set; } = new();
    public MetricItemDto SuccessOutcomesMetric { get; set; } = new();
    public MetricItemDto PartialOutcomesMetric { get; set; } = new();
    public MetricItemDto FailedOutcomesMetric { get; set; } = new();

    public IReadOnlyList<FrequencyItemDto> TopUsedKnowledge { get; set; } = [];
    public IReadOnlyList<AttentionCaseDto> RecurrentWithoutRootCauseCases { get; set; } = [];
    public IReadOnlyList<AttentionCaseDto> ResolvedWithoutKnowledgeCases { get; set; } = [];
}
