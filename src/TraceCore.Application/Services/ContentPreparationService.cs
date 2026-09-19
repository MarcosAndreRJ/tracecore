using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class ContentPreparationService : IContentPreparationService
{
    private readonly ISearchableContentRepository _contentRepo;
    private readonly IKnowledgeRepository _knowledgeRepo;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseResolutionRepository _caseResolutionRepository;
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAuditService _auditService;

    public ContentPreparationService(
        ISearchableContentRepository contentRepo,
        IKnowledgeRepository knowledgeRepo,
        ICaseRepository caseRepository,
        ICaseResolutionRepository caseResolutionRepository,
        IDiagnosticRepository diagnosticRepository,
        ICatalogRepository catalogRepository,
        IAuditService auditService)
    {
        _contentRepo = contentRepo;
        _knowledgeRepo = knowledgeRepo;
        _caseRepository = caseRepository;
        _caseResolutionRepository = caseResolutionRepository;
        _diagnosticRepository = diagnosticRepository;
        _catalogRepository = catalogRepository;
        _auditService = auditService;
    }

    public async Task<SearchableContentEntryDto> PrepareKnowledgeItemAsync(long knowledgeItemId, long? currentUserId = null, CancellationToken ct = default)
    {
        var item = await _knowledgeRepo.GetByIdAsync(knowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", knowledgeItemId);

        var versions = await _knowledgeRepo.GetVersionsByItemIdAsync(knowledgeItemId, ct);
        var version = versions.FirstOrDefault(v => v.VersionNo == item.CurrentVersionNo) ?? versions.OrderByDescending(v => v.VersionNo).FirstOrDefault();

        var sb = new StringBuilder();
        sb.AppendLine($"[CONHECIMENTO] {item.KnowledgeCode}: {item.Title}");
        sb.AppendLine($"[TIPO] {item.KnowledgeType} | [STATUS] {item.Status} | [ORIGEM] {item.ProvenanceType}");
        if (!string.IsNullOrWhiteSpace(item.Summary))
        {
            sb.AppendLine($"[RESUMO] {item.Summary}");
        }

        if (version != null)
        {
            if (!string.IsNullOrWhiteSpace(version.ProblemDescription))
                sb.AppendLine($"[PROBLEMA] {version.ProblemDescription}");
            if (!string.IsNullOrWhiteSpace(version.RootCauseSummary))
                sb.AppendLine($"[CAUSA_RAIZ] {version.RootCauseSummary}");
            if (!string.IsNullOrWhiteSpace(version.ContentMarkdown))
                sb.AppendLine($"[CONTEUDO]\n{version.ContentMarkdown}");
            if (!string.IsNullOrWhiteSpace(version.ValidationMethod))
                sb.AppendLine($"[METODO_VALIDACAO] {version.ValidationMethod}");
            if (!string.IsNullOrWhiteSpace(version.RiskWarning))
                sb.AppendLine($"[AVISO_RISCO] {version.RiskWarning}");
            if (!string.IsNullOrWhiteSpace(version.RollbackPlan))
                sb.AppendLine($"[PLANO_ROLLBACK] {version.RollbackPlan}");
        }

        // Aplicabilidades
        var applicabilities = await _knowledgeRepo.GetApplicabilitiesByItemIdAsync(knowledgeItemId, ct);
        if (applicabilities.Count > 0)
        {
            sb.AppendLine("[APLICABILIDADES]");
            foreach (var app in applicabilities)
            {
                sb.AppendLine($"- Produto: {app.ProductId}, Versão: {app.ProductVersionId}, Componente: {app.ComponentId}");
            }
        }

        // Tecnologias
        var techs = await _knowledgeRepo.GetTechnologiesByItemIdAsync(knowledgeItemId, ct);
        if (techs.Count > 0)
        {
            sb.AppendLine($"[TECNOLOGIAS] {string.Join(", ", techs)}");
        }

        var normalizedContent = NormalizeText(sb.ToString());
        var hash = ComputeSha256(normalizedContent);

        // ValidationStatus (§12.18)
        string validationStatus = item.Status switch
        {
            "Published" => "Validated",
            "Review" => "PendingValidation",
            "Deprecated" or "Archived" => "Rejected",
            _ => "NotValidated"
        };

        // QualityStatus (§12.15)
        string qualityStatus;
        if (item.Status == "Deprecated" || item.Status == "Archived")
        {
            qualityStatus = "Obsolete";
        }
        else if (item.ReviewDueAt.HasValue && item.ReviewDueAt.Value < DateTime.UtcNow)
        {
            qualityStatus = "NeedsReview";
        }
        else if (version == null || string.IsNullOrWhiteSpace(version.ProblemDescription) || string.IsNullOrWhiteSpace(version.ValidationMethod) || string.IsNullOrWhiteSpace(version.RiskWarning))
        {
            qualityStatus = "Incomplete";
        }
        else if (item.Status == "Published")
        {
            qualityStatus = "Validated";
        }
        else
        {
            qualityStatus = "Complete";
        }

        // Visibility (§12.16 - Não filtra por DepartmentId, deriva da Confidencialidade)
        string visibility = item.Confidentiality switch
        {
            "Public" => "Public",
            "Confidential" => "Confidential",
            "Restricted" => "Restricted",
            _ => "Internal"
        };

        long? primaryProductId = applicabilities.FirstOrDefault(a => a.ProductId.HasValue)?.ProductId;

        var metadata = new
        {
            KnowledgeCode = item.KnowledgeCode,
            ProvenanceType = item.ProvenanceType,
            ProvenanceCaseId = item.ProvenanceCaseId,
            OwnerUserId = item.OwnerUserId,
            OwnerDepartmentId = item.OwnerDepartmentId,
            Technologies = techs.ToList(),
            ApplicabilitiesCount = applicabilities.Count
        };

        var entry = new SearchableContentEntry(
            sourceType: "ValidatedKnowledge",
            sourceId: item.Id,
            title: item.Title,
            normalizedContent: normalizedContent,
            contentHash: hash,
            validationStatus: validationStatus,
            qualityStatus: qualityStatus,
            visibility: visibility,
            sourceUpdatedAt: item.UpdatedAt,
            sourceVersionId: version?.Id,
            clientId: null,
            productId: primaryProductId,
            componentIdsJson: null,
            metadataJson: JsonSerializer.Serialize(metadata)
        );

        var entryId = await _contentRepo.UpsertAsync(entry, ct);
        entry.Id = entryId;

        await _auditService.RecordAsync(
            action: "content.prepare",
            entityType: "searchable_content_entries",
            entityId: entryId.ToString(),
            actorUserId: currentUserId,
            metadata: new { SourceType = "ValidatedKnowledge", SourceId = item.Id, ContentHash = hash, QualityStatus = qualityStatus },
            ct: ct
        );

        return MapToDto(entry);
    }

    public async Task<SearchableContentEntryDto> PrepareCaseAsync(long caseId, long? currentUserId = null, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        var iterations = await _caseRepository.GetIterationsByCaseIdAsync(caseId, ct);
        var lastIteration = iterations.OrderByDescending(i => i.SequenceNumber).FirstOrDefault();

        var resolution = await _caseResolutionRepository.GetByCaseIdAsync(caseId, ct);
        var evidences = await _diagnosticRepository.GetEvidencesByCaseIdAsync(caseId, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"[CASO_HISTORICO] #{@case.CaseNumber}: {@case.OriginalReport}");
        if (!string.IsNullOrWhiteSpace(@case.NormalizedSummary))
            sb.AppendLine($"[RESUMO_NORMALIZADO] {@case.NormalizedSummary}");

        if (!string.IsNullOrWhiteSpace(@case.ErrorCode) || !string.IsNullOrWhiteSpace(@case.ErrorMessage))
            sb.AppendLine($"[ERRO] {@case.ErrorCode}: {@case.ErrorMessage}");

        sb.AppendLine($"[SEVERIDADE] {@case.Severity} | [IMPACTO] {@case.ImpactLevel} | [STATUS] {@case.Status}");

        // Componentes afetados
        if (@case.AffectedComponents.Count > 0)
        {
            sb.AppendLine("[COMPONENTES_AFETADOS]");
            foreach (var comp in @case.AffectedComponents)
            {
                sb.AppendLine($"- Componente #{comp.ComponentId} ({comp.RelationType}, Confiança: {comp.ConfidenceLabel})");
            }
        }

        // Iteração e resolução
        if (lastIteration != null)
        {
            sb.AppendLine($"[ITERACAO] #{lastIteration.SequenceNumber} ({lastIteration.Status}) - Aberta em: {lastIteration.OpenedAt:O}");
            if (!string.IsNullOrWhiteSpace(lastIteration.Reason))
                sb.AppendLine($"[MOTIVO_ITERACAO] {lastIteration.Reason}");
        }

        // Evidências estruturadas com relações a hipóteses (§12.21)
        if (evidences.Count > 0)
        {
            sb.AppendLine("[EVIDENCIAS_ESTRUTURADAS]");
            foreach (var ev in evidences)
            {
                sb.AppendLine($"- Evidência #{ev.Id} [{ev.EvidenceType}]: {ev.Description}");
                var relations = await _diagnosticRepository.GetHypothesisRelationsByEvidenceIdAsync(ev.Id, ct);
                foreach (var rel in relations)
                {
                    sb.AppendLine($"  * Hipótese #{rel.HypothesisId} ({rel.RelationType}): {rel.Justification}");
                }
            }
        }

        // Resolução e Causa Raiz
        if (resolution != null)
        {
            sb.AppendLine($"[RESOLUCAO] Tipo: {resolution.ResolutionType} | Risco Recorrência: {resolution.RecurrenceRisk}");
            if (!string.IsNullOrWhiteSpace(resolution.ResolutionSummary))
                sb.AppendLine($"[RESUMO_RESOLUCAO] {resolution.ResolutionSummary}");
            if (!string.IsNullOrWhiteSpace(resolution.ValidationSummary))
                sb.AppendLine($"[VALIDACAO_RESOLUCAO] {resolution.ValidationSummary}");
            if (resolution.RootCauseId.HasValue)
            {
                var rootCause = await _caseResolutionRepository.GetRootCauseByIdAsync(resolution.RootCauseId.Value, ct);
                if (rootCause != null)
                {
                    sb.AppendLine($"[CAUSA_RAIZ] {rootCause.Name} ({rootCause.Category}) - {rootCause.Description} [Confirmada: {resolution.RootCauseConfirmed}]");
                }
            }
            if (!string.IsNullOrWhiteSpace(resolution.PreventiveActions))
                sb.AppendLine($"[ACOES_PREVENTIVAS] {resolution.PreventiveActions}");
            if (!string.IsNullOrWhiteSpace(resolution.RecurrenceNotes))
                sb.AppendLine($"[NOTAS_RECORRENCIA] {resolution.RecurrenceNotes}");
        }

        var normalizedContent = NormalizeText(sb.ToString());
        var hash = ComputeSha256(normalizedContent);

        // ValidationStatus (§12.18)
        string validationStatus;
        if (@case.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && resolution != null && resolution.RootCauseConfirmed)
        {
            validationStatus = "Validated";
        }
        else if (@case.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
        {
            validationStatus = "PendingValidation";
        }
        else
        {
            validationStatus = "NotValidated";
        }

        // QualityStatus (§12.15)
        string qualityStatus;
        if (@case.Status.Equals("Reopened", StringComparison.OrdinalIgnoreCase))
        {
            qualityStatus = "NeedsReview";
        }
        else if (string.IsNullOrWhiteSpace(@case.NormalizedSummary) || @case.AffectedComponents.Count == 0 || resolution == null)
        {
            qualityStatus = "Incomplete";
        }
        else if (validationStatus == "Validated")
        {
            qualityStatus = "Validated";
        }
        else
        {
            qualityStatus = "Complete";
        }

        // Visibility (§12.16)
        string visibility = "Internal";

        var compIds = @case.AffectedComponents.Select(c => c.ComponentId).ToList();

        var metadata = new
        {
            CaseNumber = @case.CaseNumber,
            Severity = @case.Severity,
            ImpactLevel = @case.ImpactLevel,
            IterationsCount = iterations.Count,
            EvidencesCount = evidences.Count,
            HasResolution = resolution != null,
            RootCauseConfirmed = resolution?.RootCauseConfirmed ?? false
        };

        var entry = new SearchableContentEntry(
            sourceType: "HistoricalCase",
            sourceId: @case.Id,
            title: $"Caso #{@case.CaseNumber}: {(@case.NormalizedSummary ?? @case.OriginalReport)}",
            normalizedContent: normalizedContent,
            contentHash: hash,
            validationStatus: validationStatus,
            qualityStatus: qualityStatus,
            visibility: visibility,
            sourceUpdatedAt: @case.UpdatedAt,
            sourceVersionId: lastIteration?.Id,
            clientId: @case.ClientId,
            productId: @case.ProductId,
            componentIdsJson: JsonSerializer.Serialize(compIds),
            metadataJson: JsonSerializer.Serialize(metadata)
        );

        var entryId = await _contentRepo.UpsertAsync(entry, ct);
        entry.Id = entryId;

        await _auditService.RecordAsync(
            action: "content.prepare",
            entityType: "searchable_content_entries",
            entityId: entryId.ToString(),
            actorUserId: currentUserId,
            metadata: new { SourceType = "HistoricalCase", SourceId = @case.Id, ContentHash = hash, QualityStatus = qualityStatus },
            ct: ct
        );

        return MapToDto(entry);
    }

    public async Task<int> SyncAllPublishedKnowledgeAsync(long? currentUserId = null, CancellationToken ct = default)
    {
        var publishedItems = await _knowledgeRepo.SearchAsync(status: "Published", ct: ct);
        int count = 0;
        foreach (var item in publishedItems)
        {
            await PrepareKnowledgeItemAsync(item.Id, currentUserId, ct);
            count++;
        }
        return count;
    }

    public async Task<int> SyncAllResolvedCasesAsync(long? currentUserId = null, CancellationToken ct = default)
    {
        var cases = await _caseRepository.GetAllAsync(limit: 1000, ct: ct);
        var resolvedCases = cases.Where(c => string.Equals(c.Status, "Resolved", StringComparison.OrdinalIgnoreCase)).ToList();
        int count = 0;
        foreach (var c in resolvedCases)
        {
            await PrepareCaseAsync(c.Id, currentUserId, ct);
            count++;
        }
        return count;
    }

    public async Task<ContentSearchResultDto> SearchAsync(SearchableContentFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await _contentRepo.SearchAsync(
            sourceType: filter.SourceType,
            validationStatus: filter.ValidationStatus,
            qualityStatus: filter.QualityStatus,
            visibility: filter.Visibility,
            clientId: filter.ClientId,
            productId: filter.ProductId,
            searchTerm: filter.SearchTerm,
            skip: skip,
            take: pageSize,
            ct: ct);

        var dtos = items.Select(MapToDto).ToList();
        return new ContentSearchResultDto(dtos, totalCount, page, pageSize);
    }

    public async Task<ContentQualityMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var (allItems, totalCount) = await _contentRepo.SearchAsync(skip: 0, take: 5000, ct: ct);
        var qualityCounts = await _contentRepo.GetCountByQualityStatusAsync(ct);
        var sourceCounts = await _contentRepo.GetCountBySourceTypeAsync(ct);

        int ready = 0;
        int needsMetadata = 0;
        int needsReview = 0;
        int notEligible = 0;

        foreach (var item in allItems)
        {
            var (readiness, _) = DetermineReadiness(item.ValidationStatus, item.QualityStatus, item.Visibility);
            switch (readiness)
            {
                case "Ready":
                    ready++;
                    break;
                case "NeedsMetadata":
                    needsMetadata++;
                    break;
                case "NeedsReview":
                    needsReview++;
                    break;
                case "NotEligible":
                    notEligible++;
                    break;
            }
        }

        return new ContentQualityMetricsDto(
            TotalCount: totalCount,
            ReadyCount: ready,
            NeedsMetadataCount: needsMetadata,
            NeedsReviewCount: needsReview,
            NotEligibleCount: notEligible,
            BySourceType: sourceCounts,
            ByQualityStatus: qualityCounts
        );
    }

    private static SearchableContentEntryDto MapToDto(SearchableContentEntry entry)
    {
        var (readiness, reason) = DetermineReadiness(entry.ValidationStatus, entry.QualityStatus, entry.Visibility);
        string? sourceUrl = entry.SourceType.Equals("ValidatedKnowledge", StringComparison.OrdinalIgnoreCase)
            ? $"/Knowledge/Details/{entry.SourceId}"
            : entry.SourceType.Equals("HistoricalCase", StringComparison.OrdinalIgnoreCase)
                ? $"/Cases/Details/{entry.SourceId}"
                : null;

        return new SearchableContentEntryDto(
            Id: entry.Id,
            SourceType: entry.SourceType,
            SourceId: entry.SourceId,
            SourceVersionId: entry.SourceVersionId,
            Title: entry.Title,
            NormalizedContent: entry.NormalizedContent,
            ContentHash: entry.ContentHash,
            ValidationStatus: entry.ValidationStatus,
            QualityStatus: entry.QualityStatus,
            Visibility: entry.Visibility,
            ReadinessStatus: readiness,
            ReadinessReason: reason,
            ClientId: entry.ClientId,
            ProductId: entry.ProductId,
            ComponentIdsJson: entry.ComponentIdsJson,
            MetadataJson: entry.MetadataJson,
            CreatedAt: entry.CreatedAt,
            UpdatedAt: entry.UpdatedAt,
            SourceUpdatedAt: entry.SourceUpdatedAt,
            SourceUrl: sourceUrl
        );
    }

    private static (string ReadinessStatus, string ReadinessReason) DetermineReadiness(string validationStatus, string qualityStatus, string visibility)
    {
        if (qualityStatus.Equals("Obsolete", StringComparison.OrdinalIgnoreCase) ||
            validationStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return ("NotEligible", "Conteúdo obsoleto, depreciado ou arquivado.");
        }

        if (validationStatus.Equals("NotValidated", StringComparison.OrdinalIgnoreCase))
        {
            return ("NotEligible", "Conteúdo ainda em estágio inicial ou rascunho, não validado.");
        }

        if (qualityStatus.Equals("NeedsReview", StringComparison.OrdinalIgnoreCase) ||
            validationStatus.Equals("PendingValidation", StringComparison.OrdinalIgnoreCase))
        {
            return ("NeedsReview", "Conteúdo com revisão periódica vencida ou pendente de validação formal.");
        }

        if (qualityStatus.Equals("Incomplete", StringComparison.OrdinalIgnoreCase))
        {
            return ("NeedsMetadata", "Faltam metadados estruturados essenciais (produtos, componentes, método de validação ou aviso de risco).");
        }

        if (validationStatus.Equals("Validated", StringComparison.OrdinalIgnoreCase) &&
            (qualityStatus.Equals("Validated", StringComparison.OrdinalIgnoreCase) || qualityStatus.Equals("Complete", StringComparison.OrdinalIgnoreCase)))
        {
            return ("Ready", "Conteúdo validado, completo e pronto para consumo por inteligência assistiva.");
        }

        return ("NeedsReview", "Status necessita de revisão operacional.");
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        var resultLines = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                resultLines.Add(trimmed);
            }
        }

        return string.Join("\n", resultLines);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
