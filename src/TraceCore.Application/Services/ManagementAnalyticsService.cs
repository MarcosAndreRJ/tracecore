using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class ManagementAnalyticsService : IManagementAnalyticsService
{
    private readonly IManagementAnalyticsRepository _analyticsRepository;

    public ManagementAnalyticsService(IManagementAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<ManagementOverviewDto> GetOverviewAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default)
    {
        var (currentFrom, currentTo, prevFrom, prevTo) = ResolvePeriodDates(filter);
        var currentCriteria = filter.ToCriteria(currentFrom, currentTo);
        var prevCriteria = prevFrom.HasValue ? filter.ToCriteria(prevFrom, prevTo) : null;

        var rawCurrent = await _analyticsRepository.GetOverviewMetricsAsync(currentCriteria, ct);
        var prevCurrent = prevCriteria != null ? await _analyticsRepository.GetOverviewMetricsAsync(prevCriteria, ct) : null;

        // Durações de iteração para MTTR e Mediana
        var currentDurations = await _analyticsRepository.GetResolvedIterationDurationsMinutesAsync(currentCriteria, ct);
        var prevDurations = prevCriteria != null ? await _analyticsRepository.GetResolvedIterationDurationsMinutesAsync(prevCriteria, ct) : null;

        double currentMttr = currentDurations.Count > 0 ? currentDurations.Average() : 0;
        double currentMedian = CalculateMedian(currentDurations);

        double? prevMttr = prevDurations != null && prevDurations.Count > 0 ? prevDurations.Average() : null;
        double? prevMedian = prevDurations != null ? CalculateMedian(prevDurations) : null;

        var result = new ManagementOverviewDto
        {
            Filter = filter,
            OpenCasesMetric = BuildMetric(
                key: "open_cases",
                label: "Casos Abertos",
                value: rawCurrent.OpenCases,
                unit: "casos",
                description: "Casos atualmente nos status Open ou Reopened.",
                prevValue: prevCurrent?.OpenCases,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("Status=Open")}"
            ),
            ResolvedCasesMetric = BuildMetric(
                key: "resolved_cases",
                label: "Casos Resolvidos",
                value: rawCurrent.ResolvedCases,
                unit: "casos",
                description: "Casos com ciclo ativo resolvido no período.",
                prevValue: prevCurrent?.ResolvedCases,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("Status=Resolved")}"
            ),
            MttrMetric = BuildTimeMetric(
                key: "mttr",
                label: "Tempo Médio de Resolução (MTTR)",
                minutes: currentMttr,
                description: "Média de duração das iterações resolvidas (ClosedAt - OpenedAt).",
                prevMinutes: prevMttr,
                sampleCount: currentDurations.Count,
                prevSampleCount: prevDurations?.Count ?? 0,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("Status=Resolved")}"
            ),
            MttrMedianMetric = BuildTimeMetric(
                key: "mttr_median",
                label: "Mediana de Resolução",
                minutes: currentMedian,
                description: "Valor central das durações das iterações resolvidas.",
                prevMinutes: prevMedian,
                sampleCount: currentDurations.Count,
                prevSampleCount: prevDurations?.Count ?? 0,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("Status=Resolved")}"
            ),
            RecurrentCasesMetric = BuildMetric(
                key: "recurrent_cases",
                label: "Recorrências",
                value: rawCurrent.RecurrentCases,
                unit: "casos",
                description: "Casos vinculados por relações 'Recurrence' ou 'CommonCause'.",
                prevValue: prevCurrent?.RecurrentCases,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("RecurrentOnly=true")}"
            ),
            UnconfirmedRootCauseMetric = BuildMetric(
                key: "unconfirmed_root_cause",
                label: "Sem Causa Raiz Definida",
                value: rawCurrent.UnconfirmedRootCauseCases,
                unit: "casos",
                description: "Casos resolvidos sem causa raiz confirmada registrada.",
                prevValue: prevCurrent?.UnconfirmedRootCauseCases,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("WithoutRootCause=true")}"
            ),
            UndocumentedKnowledgeMetric = BuildMetric(
                key: "undocumented_knowledge",
                label: "Sem Solução Documentada",
                value: rawCurrent.UndocumentedKnowledgeCases,
                unit: "casos",
                description: "Casos resolvidos que não originaram artigo na Base de Conhecimento.",
                prevValue: prevCurrent?.UndocumentedKnowledgeCases,
                drillDownUrl: $"/Cases/Index{filter.ToQueryString("WithoutKnowledge=true")}"
            )
        };

        // Séries temporais
        var rawTimeline = await _analyticsRepository.GetTimeEvolutionAsync(currentCriteria, filter.Grouping, ct);
        result.TimeEvolution = rawTimeline.Select(t => new TrendPointDto
        {
            Date = t.DateBucket,
            Label = filter.Grouping == "month" ? t.DateBucket.ToString("MMM/yy") : t.DateBucket.ToString("dd/MM"),
            OpenedCount = t.OpenedCount,
            ResolvedCount = t.ResolvedCount
        }).ToList();

        // Distribuições
        var rawProducts = await _analyticsRepository.GetTopProductsAsync(currentCriteria, 5, ct);
        int totalProd = rawProducts.Sum(p => p.Count);
        result.TopProducts = rawProducts.Select(p => new FrequencyItemDto
        {
            Id = p.Id,
            Name = p.Label,
            Count = p.Count,
            Percentage = totalProd > 0 ? Math.Round((double)p.Count / totalProd * 100, 1) : 0,
            DrillDownUrl = p.Id.HasValue ? $"/Cases/Index{filter.ToQueryString($"ProductId={p.Id.Value}")}" : $"/Cases/Index{filter.ToQueryString()}"
        }).ToList();

        var rawComponents = await _analyticsRepository.GetTopComponentsAsync(currentCriteria, 5, ct);
        int totalComp = rawComponents.Sum(c => c.Count);
        result.TopComponents = rawComponents.Select(c => new FrequencyItemDto
        {
            Id = c.Id,
            Name = c.Label,
            SecondaryText = c.SecondaryLabel,
            Count = c.Count,
            Percentage = totalComp > 0 ? Math.Round((double)c.Count / totalComp * 100, 1) : 0,
            DrillDownUrl = c.Id.HasValue ? $"/Cases/Index{filter.ToQueryString($"ComponentId={c.Id.Value}")}" : $"/Cases/Index{filter.ToQueryString()}"
        }).ToList();

        var rawCauses = await _analyticsRepository.GetTopRootCausesAsync(currentCriteria, 5, ct);
        int totalCauses = rawCauses.Sum(c => c.Count);
        result.TopRootCauses = rawCauses.Select(c => new FrequencyItemDto
        {
            Id = c.Id,
            Name = c.Label,
            Count = c.Count,
            Percentage = totalCauses > 0 ? Math.Round((double)c.Count / totalCauses * 100, 1) : 0,
            DrillDownUrl = c.Id.HasValue ? $"/Cases/Index{filter.ToQueryString($"RootCauseId={c.Id.Value}")}" : $"/Cases/Index{filter.ToQueryString("WithoutRootCause=true")}"
        }).ToList();

        var rawRecurrences = await _analyticsRepository.GetTopRecurrencesAsync(currentCriteria, 5, ct);
        int totalRec = rawRecurrences.Sum(r => r.Count);
        result.TopRecurrences = rawRecurrences.Select(r => new FrequencyItemDto
        {
            Id = r.Id,
            Name = r.Label,
            Count = r.Count,
            Percentage = totalRec > 0 ? Math.Round((double)r.Count / totalRec * 100, 1) : 0,
            DrillDownUrl = $"/Cases/Index{filter.ToQueryString("RecurrentOnly=true")}"
        }).ToList();

        var rawAttention = await _analyticsRepository.GetAttentionCasesAsync(currentCriteria, 10, ct);
        result.AttentionCases = rawAttention.Select(a => new AttentionCaseDto
        {
            Id = a.Id,
            CaseNumber = a.CaseNumber,
            Title = a.Title,
            Status = a.Status,
            Severity = a.Severity,
            OpenedAt = a.OpenedAt,
            ActiveDays = a.ActiveDays,
            IterationCount = a.IterationCount,
            HasRootCause = a.HasRootCause,
            HasKnowledge = a.HasKnowledge,
            AttentionReason = a.AttentionReason
        }).ToList();

        return result;
    }

    public async Task<DepartmentAnalyticsDto> GetDepartmentAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default)
    {
        var (currentFrom, currentTo, _, _) = ResolvePeriodDates(filter);
        var criteria = filter.ToCriteria(currentFrom, currentTo);

        var rawDeps = await _analyticsRepository.GetDepartmentMetricsAsync(criteria, ct);
        var resultList = new List<DepartmentSummaryDto>();

        foreach (var dep in rawDeps)
        {
            var durations = await _analyticsRepository.GetDepartmentIterationDurationsMinutesAsync(dep.DepartmentId, criteria, ct);
            double mttr = durations.Count > 0 ? durations.Average() : 0;
            double median = CalculateMedian(durations);

            resultList.Add(new DepartmentSummaryDto
            {
                DepartmentId = dep.DepartmentId,
                DepartmentName = dep.DepartmentName,
                ActiveCasesCount = dep.ActiveCasesCount,
                ResolvedCasesCount = dep.ResolvedCasesCount,
                ReopenedCasesCount = dep.ReopenedCasesCount,
                RecurrentCasesCount = dep.RecurrentCasesCount,
                KnowledgeCreatedCount = dep.KnowledgeCreatedCount,
                DiagnosticStepsCount = dep.DiagnosticStepsCount,
                MttrMinutes = mttr,
                FormattedMttr = FormatMinutes(mttr, durations.Count),
                MttrMedianMinutes = median,
                FormattedMttrMedian = FormatMinutes(median, durations.Count),
                DrillDownUrl = $"/Cases/Index{filter.ToQueryString($"DepartmentId={dep.DepartmentId}")}"
            });
        }

        return new DepartmentAnalyticsDto
        {
            Filter = filter,
            Departments = resultList
        };
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default)
    {
        var (currentFrom, currentTo, _, _) = ResolvePeriodDates(filter);
        var criteria = filter.ToCriteria(currentFrom, currentTo);

        var rawUsers = await _analyticsRepository.GetUserMetricsAsync(criteria, ct);

        var summaries = rawUsers.Select(u =>
        {
            string? focus = null;
            if (!string.IsNullOrEmpty(u.TopProduct) && !string.IsNullOrEmpty(u.TopComponent))
                focus = $"{u.TopProduct} / {u.TopComponent}";
            else if (!string.IsNullOrEmpty(u.TopProduct))
                focus = u.TopProduct;
            else if (!string.IsNullOrEmpty(u.TopComponent))
                focus = u.TopComponent;

            return new UserSummaryDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                UserEmail = u.UserEmail,
                OpenedCasesCount = u.OpenedCasesCount,
                ResolvedCasesCount = u.ResolvedCasesCount,
                DiagnosticStepsCount = u.DiagnosticStepsCount,
                HypothesesCreatedCount = u.HypothesesCreatedCount,
                EvidencesCreatedCount = u.EvidencesCreatedCount,
                KnowledgeAuthoredCount = u.KnowledgeAuthoredCount,
                KnowledgeUsedCount = u.KnowledgeUsedCount,
                EmergingFocusArea = focus,
                DrillDownUrl = $"/Cases/Index{filter.ToQueryString($"OwnerUserId={u.UserId}")}"
            };
        }).ToList();

        return new UserAnalyticsDto
        {
            Filter = filter,
            Users = summaries
        };
    }

    public async Task<KnowledgeAnalyticsDto> GetKnowledgeAnalyticsAsync(AnalyticsFilterDto filter, CancellationToken ct = default)
    {
        var (currentFrom, currentTo, _, _) = ResolvePeriodDates(filter);
        var criteria = filter.ToCriteria(currentFrom, currentTo);

        var rawK = await _analyticsRepository.GetKnowledgeMetricsAsync(criteria, ct);
        var topUsed = await _analyticsRepository.GetTopUsedKnowledgeAsync(criteria, 5, ct);

        var allAttention = await _analyticsRepository.GetAttentionCasesAsync(criteria, 50, ct);

        var recurrentWithoutCause = allAttention
            .Where(a => !a.HasRootCause && a.AttentionReason.Contains("recorrente", StringComparison.OrdinalIgnoreCase))
            .Select(a => new AttentionCaseDto
            {
                Id = a.Id,
                CaseNumber = a.CaseNumber,
                Title = a.Title,
                Status = a.Status,
                Severity = a.Severity,
                OpenedAt = a.OpenedAt,
                ActiveDays = a.ActiveDays,
                IterationCount = a.IterationCount,
                HasRootCause = a.HasRootCause,
                HasKnowledge = a.HasKnowledge,
                AttentionReason = a.AttentionReason
            }).ToList();

        var resolvedWithoutKnowledge = allAttention
            .Where(a => a.Status == "Resolved" && !a.HasKnowledge)
            .Select(a => new AttentionCaseDto
            {
                Id = a.Id,
                CaseNumber = a.CaseNumber,
                Title = a.Title,
                Status = a.Status,
                Severity = a.Severity,
                OpenedAt = a.OpenedAt,
                ActiveDays = a.ActiveDays,
                IterationCount = a.IterationCount,
                HasRootCause = a.HasRootCause,
                HasKnowledge = a.HasKnowledge,
                AttentionReason = a.AttentionReason
            }).ToList();

        return new KnowledgeAnalyticsDto
        {
            Filter = filter,
            TotalPublishedMetric = new MetricItemDto
            {
                Key = "published_solutions",
                Label = "Soluções Publicadas",
                Value = rawK.TotalPublished,
                FormattedValue = rawK.TotalPublished.ToString(),
                Unit = "itens",
                Description = "Total de artigos com status 'Published'."
            },
            NeverReviewedMetric = new MetricItemDto
            {
                Key = "never_reviewed",
                Label = "Nunca Revisadas",
                Value = rawK.NeverReviewedCount,
                FormattedValue = rawK.NeverReviewedCount.ToString(),
                Unit = "itens",
                Description = "Artigos sem data de última revisão registrada (LastReviewedAt IS NULL)."
            },
            ReviewOverdueMetric = new MetricItemDto
            {
                Key = "review_overdue",
                Label = "Revisão Vencida",
                Value = rawK.ReviewOverdueCount,
                FormattedValue = rawK.ReviewOverdueCount.ToString(),
                Unit = "itens",
                Description = "Artigos cuja data de revisão agendada já expirou."
            },
            TotalUsagesMetric = new MetricItemDto
            {
                Key = "total_usages",
                Label = "Reutilizações Registradas",
                Value = rawK.TotalUsagesCount,
                FormattedValue = rawK.TotalUsagesCount.ToString(),
                Unit = "usos",
                Description = "Aplicações de soluções registradas em casos via KnowledgeUsage."
            },
            SuccessOutcomesMetric = new MetricItemDto
            {
                Key = "worked_usages",
                Label = "Desfecho: Sucesso (Worked)",
                Value = rawK.WorkedUsagesCount,
                FormattedValue = rawK.WorkedUsagesCount.ToString(),
                Unit = "vezes",
                Description = "Reutilizações onde a solução resolveu a ocorrência."
            },
            PartialOutcomesMetric = new MetricItemDto
            {
                Key = "partial_usages",
                Label = "Desfecho: Parcial",
                Value = rawK.PartiallyWorkedUsagesCount,
                FormattedValue = rawK.PartiallyWorkedUsagesCount.ToString(),
                Unit = "vezes",
                Description = "Reutilizações com alívio temporário ou mitigação parcial."
            },
            FailedOutcomesMetric = new MetricItemDto
            {
                Key = "failed_usages",
                Label = "Desfecho: Não Funcionou",
                Value = rawK.DidNotWorkUsagesCount,
                FormattedValue = rawK.DidNotWorkUsagesCount.ToString(),
                Unit = "vezes",
                Description = "Reutilizações ineficazes registradas na linha do tempo."
            },
            TopUsedKnowledge = topUsed.Select(k => new FrequencyItemDto
            {
                Id = k.Id,
                Name = k.Label,
                SecondaryText = k.SecondaryLabel,
                Count = k.Count,
                DrillDownUrl = k.Id.HasValue ? $"/Knowledge/Details/{k.Id.Value}" : "/Knowledge/Index"
            }).ToList(),
            RecurrentWithoutRootCauseCases = recurrentWithoutCause,
            ResolvedWithoutKnowledgeCases = resolvedWithoutKnowledge
        };
    }

    public static double CalculateMedian(IReadOnlyList<double> sortedValues)
    {
        if (sortedValues == null || sortedValues.Count == 0) return 0.0;
        int count = sortedValues.Count;
        if (count % 2 == 1)
        {
            return sortedValues[count / 2];
        }
        else
        {
            return (sortedValues[(count / 2) - 1] + sortedValues[count / 2]) / 2.0;
        }
    }

    private static (DateTime? currentFrom, DateTime? currentTo, DateTime? prevFrom, DateTime? prevTo) ResolvePeriodDates(AnalyticsFilterDto filter)
    {
        var now = DateTime.UtcNow;
        if (filter.Period == "all")
        {
            return (null, null, null, null);
        }

        if (filter.Period == "custom" && filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            var start = filter.StartDate.Value;
            var end = filter.EndDate.Value;
            var span = end - start;
            var prevStart = start - span;
            return (start, end, prevStart, start);
        }

        int days = filter.Period switch
        {
            "7d" => 7,
            "90d" => 90,
            "6m" => 180,
            "12m" => 365,
            _ => 30 // padrão 30d
        };

        var curFrom = now.AddDays(-days);
        var curTo = now;
        var pFrom = curFrom.AddDays(-days);
        var pTo = curFrom;

        return (curFrom, curTo, pFrom, pTo);
    }

    private static MetricItemDto BuildMetric(string key, string label, double value, string unit, string description, double? prevValue, string drillDownUrl)
    {
        bool hasSufficient = prevValue.HasValue && prevValue.Value >= 5;
        double? trend = null;
        string direction = "neutral";

        if (hasSufficient && prevValue.HasValue && prevValue.Value > 0)
        {
            trend = Math.Round(((value - prevValue.Value) / prevValue.Value) * 100.0, 1);
            direction = trend > 0 ? "up" : trend < 0 ? "down" : "neutral";
        }

        return new MetricItemDto
        {
            Key = key,
            Label = label,
            Value = value,
            FormattedValue = value.ToString("N0"),
            Unit = unit,
            Description = description,
            PreviousPeriodValue = prevValue,
            TrendPercentage = trend,
            TrendDirection = direction,
            HasSufficientData = hasSufficient,
            DrillDownUrl = drillDownUrl
        };
    }

    private static MetricItemDto BuildTimeMetric(string key, string label, double minutes, string description, double? prevMinutes, int sampleCount, int prevSampleCount, string drillDownUrl)
    {
        bool hasSufficient = sampleCount >= 5 && prevSampleCount >= 5 && prevMinutes.HasValue && prevMinutes.Value > 0;
        double? trend = null;
        string direction = "neutral";

        if (hasSufficient && prevMinutes.HasValue && prevMinutes.Value > 0)
        {
            trend = Math.Round(((minutes - prevMinutes.Value) / prevMinutes.Value) * 100.0, 1);
            direction = trend > 0 ? "up" : trend < 0 ? "down" : "neutral";
        }

        return new MetricItemDto
        {
            Key = key,
            Label = label,
            Value = minutes,
            FormattedValue = FormatMinutes(minutes, sampleCount),
            Unit = minutes >= 1440 ? "dias" : minutes >= 60 ? "horas" : "min",
            Description = description,
            PreviousPeriodValue = prevMinutes,
            TrendPercentage = trend,
            TrendDirection = direction,
            HasSufficientData = hasSufficient,
            DrillDownUrl = drillDownUrl
        };
    }

    private static string FormatMinutes(double minutes, int count)
    {
        if (count == 0 || minutes <= 0) return "N/D";
        if (minutes < 60) return $"{Math.Round(minutes)} min";
        if (minutes < 1440) return $"{Math.Round(minutes / 60.0, 1)} h";
        return $"{Math.Round(minutes / 1440.0, 1)} d";
    }

    public async Task<TrendAfterVersionDto> GetTrendAfterVersionAsync(
        long productVersionId,
        long? rootCauseId = null,
        string? errorCode = null,
        long? componentId = null,
        int intervalDays = 90,
        CancellationToken ct = default)
    {
        if (intervalDays <= 0) intervalDays = 90;

        var raw = await _analyticsRepository.GetTrendAfterVersionRawAsync(
            productVersionId, rootCauseId, errorCode, componentId, intervalDays, ct);

        if (string.IsNullOrEmpty(raw.VersionLabel) || !raw.ReleasedAt.HasValue)
        {
            return new TrendAfterVersionDto(
                ProductVersionId: productVersionId,
                VersionLabel: string.IsNullOrEmpty(raw.VersionLabel) ? $"Versão #{productVersionId}" : raw.VersionLabel,
                ReleasedAt: raw.ReleasedAt,
                BeforeCount: 0,
                AfterCount: 0,
                PercentageChange: null,
                TotalSample: 0,
                IntervalDays: intervalDays,
                HasSufficientData: false,
                Observation: "Versão do produto sem data de lançamento registrada ou não encontrada na base."
            );
        }

        int totalSample = raw.BeforeCount + raw.AfterCount;
        bool hasSufficient = totalSample >= 5;
        double? percentageChange = null;
        string observation;

        if (raw.BeforeCount == 0)
        {
            if (raw.AfterCount == 0)
            {
                percentageChange = 0.0;
                observation = $"Nenhuma incidência registrada na janela de ±{intervalDays} dias da versão {raw.VersionLabel}.";
            }
            else
            {
                percentageChange = 100.0;
                observation = $"Incidência nova: 0 casos antes vs. {raw.AfterCount} casos após o lançamento da versão {raw.VersionLabel} (+100%, amostra de {totalSample} casos).";
            }
        }
        else
        {
            percentageChange = Math.Round(((double)(raw.AfterCount - raw.BeforeCount) / raw.BeforeCount) * 100.0, 1);
            string direction = percentageChange > 0 ? "aumentou" : (percentageChange < 0 ? "reduziu" : "permaneceu estável");
            string absPct = Math.Abs(percentageChange.Value).ToString("F1");
            observation = $"Incidência {direction} {absPct}% após o lançamento da versão {raw.VersionLabel} ({raw.BeforeCount} antes vs. {raw.AfterCount} após, amostra de {totalSample} casos em janela de {intervalDays} dias).";
        }

        return new TrendAfterVersionDto(
            ProductVersionId: productVersionId,
            VersionLabel: raw.VersionLabel,
            ReleasedAt: raw.ReleasedAt,
            BeforeCount: raw.BeforeCount,
            AfterCount: raw.AfterCount,
            PercentageChange: percentageChange,
            TotalSample: totalSample,
            IntervalDays: intervalDays,
            HasSufficientData: hasSufficient,
            Observation: observation
        );
    }

    public async Task<ComponentAssociationDto> GetComponentAssociationPercentageAsync(
        AnalyticsFilterDto filter,
        long? rootCauseId = null,
        CancellationToken ct = default)
    {
        var criteria = filter.ToCriteria();
        var (totalCases, rawCounts) = await _analyticsRepository.GetComponentAssociationRawAsync(criteria, rootCauseId, ct);

        if (totalCases == 0)
        {
            return new ComponentAssociationDto(
                TotalCases: 0,
                Components: Array.Empty<ComponentAssociationItemDto>(),
                HasSufficientData: false,
                Observation: "Nenhum caso encontrado para o filtro e causa raiz informados."
            );
        }

        var items = rawCounts.Select(r => new ComponentAssociationItemDto(
            ComponentId: r.ComponentId,
            ComponentName: r.ComponentName,
            CaseCount: r.Count,
            Percentage: Math.Round(((double)r.Count / totalCases) * 100.0, 1)
        )).ToList();

        bool hasSufficient = totalCases >= 5;
        string topComp = items.Count > 0 ? $"{items[0].ComponentName} ({items[0].Percentage}%)" : "nenhum";
        string observation = $"Distribuição calculada sobre amostra de {totalCases} casos. Maior associação observada em {topComp}.";

        return new ComponentAssociationDto(
            TotalCases: totalCases,
            Components: items,
            HasSufficientData: hasSufficient,
            Observation: observation
        );
    }

    public async Task<SolutionEffectivenessDto> GetSolutionEffectivenessComparisonAsync(
        long knowledgeItemId,
        AnalyticsFilterDto filter,
        CancellationToken ct = default)
    {
        var criteria = filter.ToCriteria();
        var (solutionTitle, scopeLabel, durationsWith, durationsWithout) = await _analyticsRepository.GetSolutionEffectivenessRawAsync(knowledgeItemId, criteria, ct);

        int sampleWith = durationsWith.Count;
        int sampleWithout = durationsWithout.Count;

        if (sampleWith < 3 || sampleWithout < 3)
        {
            return new SolutionEffectivenessDto(
                KnowledgeItemId: knowledgeItemId,
                SolutionTitle: solutionTitle,
                MedianMttrWithMinutes: sampleWith > 0 ? Math.Round(CalculateMedian(durationsWith), 1) : null,
                SampleWithCount: sampleWith,
                MedianMttrWithoutMinutes: sampleWithout > 0 ? Math.Round(CalculateMedian(durationsWithout), 1) : null,
                SampleWithoutCount: sampleWithout,
                MttrReductionPercentage: null,
                HasSufficientData: false,
                TechnicalScope: scopeLabel,
                Observation: $"Dados insuficientes para comparação confiável de efetividade (mínimo de 3 casos exigido por grupo; obtido: {sampleWith} com uso vs. {sampleWithout} sem uso no escopo '{scopeLabel}')."
            );
        }

        double medianWith = CalculateMedian(durationsWith);
        double medianWithout = CalculateMedian(durationsWithout);
        double? reductionPct = medianWithout > 0
            ? Math.Round(((medianWithout - medianWith) / medianWithout) * 100.0, 1)
            : 0.0;

        string obs;
        if (reductionPct.HasValue && reductionPct.Value > 0)
        {
            obs = $"Solução '{solutionTitle}' reduziu a mediana de MTTR em {reductionPct.Value}% no escopo '{scopeLabel}' (mediana de {FormatMinutes(medianWith, sampleWith)} em {sampleWith} casos vs. {FormatMinutes(medianWithout, sampleWithout)} em {sampleWithout} casos comparáveis sem a solução).";
        }
        else if (reductionPct.HasValue && reductionPct.Value < 0)
        {
            obs = $"Casos que usaram '{solutionTitle}' apresentaram mediana de MTTR superior em {Math.Abs(reductionPct.Value)}% ({FormatMinutes(medianWith, sampleWith)} em {sampleWith} casos vs. {FormatMinutes(medianWithout, sampleWithout)} em {sampleWithout} casos comparáveis).";
        }
        else
        {
            obs = $"Mediana de MTTR similar entre casos com uso ({FormatMinutes(medianWith, sampleWith)}) e sem uso ({FormatMinutes(medianWithout, sampleWithout)}) no escopo '{scopeLabel}'.";
        }

        return new SolutionEffectivenessDto(
            KnowledgeItemId: knowledgeItemId,
            SolutionTitle: solutionTitle,
            MedianMttrWithMinutes: Math.Round(medianWith, 1),
            SampleWithCount: sampleWith,
            MedianMttrWithoutMinutes: Math.Round(medianWithout, 1),
            SampleWithoutCount: sampleWithout,
            MttrReductionPercentage: reductionPct,
            HasSufficientData: true,
            TechnicalScope: scopeLabel,
            Observation: obs
        );
    }
}
