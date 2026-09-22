using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

// TODO: BR-029 — Reabertura de caso preservará a resolução anterior e iniciará nova iteração (Fase 6).
// TODO: BR-030 — Relacionamento entre casos (duplicado, semelhante, recorrência) será implementado em fases futuras.
// TODO: BR-046 / M05 — Publicação formal do caso como item na Base de Conhecimento consumirá o campo 'preventive_actions'
// e os dados aqui registrados para criar conhecimento reutilizável.

public class CaseResolutionService : ICaseResolutionService
{
    private static readonly HashSet<string> ValidResolutionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Definitive", "Workaround", "NeedsFollowUp", "Inconclusive", "NotAnIssue", "ClientEnvironment"
    };

    private readonly ICaseResolutionRepository _caseResolutionRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly ICaseRelationRepository _caseRelationRepository;

    public CaseResolutionService(
        ICaseResolutionRepository caseResolutionRepository,
        ICaseRepository caseRepository,
        IDiagnosticRepository diagnosticRepository,
        ICatalogRepository catalogRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IAuditEventRepository auditEventRepository,
        ICaseRelationRepository caseRelationRepository)
    {
        _caseResolutionRepository = caseResolutionRepository;
        _caseRepository = caseRepository;
        _diagnosticRepository = diagnosticRepository;
        _catalogRepository = catalogRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _auditEventRepository = auditEventRepository;
        _caseRelationRepository = caseRelationRepository;
    }

    public async Task<CaseResolutionDto> ResolveCaseAsync(
        ResolveCaseCommand command,
        long currentUserId,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
        {
            throw new EntityNotFoundException("Caso", command.CaseId);
        }

        // Bloco 7.A.3 / BR-029: Resolve a iteração ATUAL (mais recente aberta)
        if (@case.Status == "Resolved")
        {
            throw new BusinessRuleValidationException("BR-029", "Este caso já se encontra resolvido. Reabra o caso antes de registrar novo encerramento.");
        }

        var currentIteration = await _caseRepository.GetCurrentIterationAsync(command.CaseId, ct);
        if (currentIteration == null)
        {
            throw new BusinessRuleValidationException("BR-029", "Nenhuma iteração encontrada para este caso.");
        }
        if (currentIteration.Status != "Open")
        {
            throw new BusinessRuleValidationException("BR-029", "A iteração atual deste caso já se encontra encerrada. Reabra o caso antes de registrar novo encerramento.");
        }

        var existingResolution = await _caseResolutionRepository.GetByIterationIdAsync(currentIteration.Id, ct);
        if (existingResolution != null)
        {
            throw new BusinessRuleValidationException("BR-029", "Já existe uma resolução cadastrada para a iteração atual deste caso.");
        }

        // BR-027: Ação de resolução e validação do resultado são estritamente obrigatórias
        if (string.IsNullOrWhiteSpace(command.ResolutionSummary))
        {
            throw new BusinessRuleValidationException("BR-027", "A ação de resolução é obrigatória para considerar o caso como resolvido.");
        }

        if (string.IsNullOrWhiteSpace(command.ValidationSummary))
        {
            throw new BusinessRuleValidationException("BR-027", "A forma de validação do resultado é obrigatória para considerar o caso como resolvido.");
        }

        // Requisitos estruturados do produto (Bloco 5.1 / Encerramento Estruturado)
        var resolutionType = ValidResolutionTypes.Contains(command.ResolutionType ?? string.Empty)
            ? command.ResolutionType!.Trim()
            : "Definitive";

        var recurrenceRisk = command.RecurrenceRisk?.Trim() switch
        {
            "High" => "High",
            "Medium" => "Medium",
            _ => "Low"
        };

        // Tratamento de Causa Raiz e regra BR-028:
        // Se rootCauseId não for informado nem criado novo, ou se rootCauseConfirmed for false,
        // o caso DEVE ser explicitamente marcado como 'NotConfirmed' (nunca 'NotEvaluated').
        long? finalRootCauseId = command.RootCauseId;

        if (!finalRootCauseId.HasValue && !string.IsNullOrWhiteSpace(command.NewRootCauseName))
        {
            var newRc = new RootCause(
                name: command.NewRootCauseName.Trim(),
                code: $"RC-{DateTime.UtcNow.Ticks % 1000000:D6}",
                category: command.NewRootCauseCategory?.Trim() ?? "Geral",
                description: "Causa raiz cadastrada no encerramento do caso #" + command.CaseId
            );
            finalRootCauseId = await _caseResolutionRepository.AddRootCauseAsync(newRc, ct);
        }

        string rootCauseStatus;
        bool rootCauseConfirmed = command.RootCauseConfirmed;

        if (finalRootCauseId.HasValue && rootCauseConfirmed)
        {
            rootCauseStatus = "Confirmed";
        }
        else
        {
            // BR-028: Caso resolvido sem causa raiz confirmada DEVE ficar explicitamente marcado como NotConfirmed
            rootCauseStatus = "NotConfirmed";
            rootCauseConfirmed = false;
        }

        var resolvedAt = DateTime.UtcNow;

        // Persistência da resolução estruturada vinculada à iteração atual
        var resolution = new CaseResolution(
            caseId: command.CaseId,
            resolutionSummary: command.ResolutionSummary.Trim(),
            validationSummary: command.ValidationSummary.Trim(),
            resolvedBy: currentUserId,
            rootCauseId: finalRootCauseId,
            rootCauseConfirmed: rootCauseConfirmed,
            responsibleDepartmentId: command.ResponsibleDepartmentId,
            resolutionType: resolutionType,
            recurrenceRisk: recurrenceRisk,
            recurrenceNotes: command.RecurrenceNotes?.Trim(),
            preventiveActions: command.PreventiveActions?.Trim(),
            effortMinutes: command.EffortMinutes,
            resolvedAt: resolvedAt
        )
        {
            CaseIterationId = currentIteration.Id
        };

        var resolutionId = await _caseResolutionRepository.AddResolutionAsync(resolution, ct);

        // Bloco 7.A.3: Atualiza status da iteração atual para 'Resolved'
        await _caseRepository.UpdateIterationStatusAsync(currentIteration.Id, "Resolved", resolvedAt, ct);

        // Atualização atômica do status do Caso (Status -> 'Resolved', RootCauseStatus -> Confirmed/NotConfirmed)
        await _caseRepository.UpdateCaseResolutionStatusAsync(
            command.CaseId,
            status: "Resolved",
            rootCauseStatus: rootCauseStatus,
            resolvedAt: resolvedAt,
            resolvedBy: currentUserId,
            ct: ct
        );

        // Marca componente responsável como 'RootCause' na tabela existente case_components
        if (command.ResponsibleComponentId.HasValue && command.ResponsibleComponentId.Value > 0)
        {
            await _caseRepository.UpdateComponentRelationAsync(
                command.CaseId,
                command.ResponsibleComponentId.Value,
                relationType: "RootCause",
                ct: ct
            );
        }

        // Hipótese(s) do próprio caso apontadas como causa raiz real investigada (pode ser mais
        // de uma — causa composta). Distinto de RootCauseId (taxonomia corporativa genérica).
        var hypothesisIds = (command.RootCauseHypothesisIds ?? new List<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (hypothesisIds.Count > 0)
        {
            var validHypothesisIds = new List<long>();
            foreach (var hypId in hypothesisIds)
            {
                var hyp = await _diagnosticRepository.GetHypothesisByIdAsync(hypId, ct);
                // Hipótese precisa pertencer a este caso — defesa contra ids de outro caso
                // (ex.: reaproveitados por engano numa aplicação em lote a casos vinculados).
                if (hyp == null || hyp.CaseId != command.CaseId) continue;

                validHypothesisIds.Add(hypId);
                if (hyp.Status != HypothesisStatus.Supported.ToString())
                {
                    hyp.Evaluate(HypothesisStatus.Supported, $"Confirmada como causa raiz no encerramento estruturado do caso #{command.CaseId} (BR-027/BR-028).", resolvedAt);
                    await _diagnosticRepository.UpdateHypothesisAsync(hyp, ct);
                }
            }

            if (validHypothesisIds.Count > 0)
            {
                await _caseResolutionRepository.SetResolutionHypothesesAsync(resolutionId, validHypothesisIds, ct);
            }
        }

        // Vínculo manual "Causa Comum" com casos semelhantes ainda em aberto que o analista
        // confirmou serem o mesmo problema — não fecha esses casos automaticamente (ver
        // ApplyResolutionToLinkedCasesAsync para a ação explícita de aplicar a mesma resolução).
        var linkedCaseIds = (command.LinkedSimilarCaseIds ?? new List<long>())
            .Where(id => id > 0 && id != command.CaseId)
            .Distinct()
            .ToList();

        if (linkedCaseIds.Count > 0)
        {
            var existingRelations = await _caseRelationRepository.GetRelationsByCaseIdAsync(command.CaseId, ct);
            foreach (var targetId in linkedCaseIds)
            {
                bool alreadyLinked = existingRelations.Any(r =>
                    (r.SourceCaseId == command.CaseId && r.TargetCaseId == targetId ||
                     r.SourceCaseId == targetId && r.TargetCaseId == command.CaseId) &&
                    string.Equals(r.RelationType, "CommonCause", StringComparison.OrdinalIgnoreCase));

                if (!alreadyLinked)
                {
                    var relation = new CaseRelation(
                        sourceCaseId: command.CaseId,
                        targetCaseId: targetId,
                        relationType: CaseRelationType.CommonCause,
                        matchedFactors: new[] { "Confirmado manualmente no encerramento do caso" },
                        createdBy: currentUserId
                    );
                    await _caseRelationRepository.AddManualRelationAsync(relation, ct);
                }
            }
        }

        // Trilha de Auditoria (BR-004 / BR-100)
        var auditDetails = JsonSerializer.Serialize(new
        {
            ResolutionId = resolutionId,
            ResolutionType = resolutionType,
            RootCauseStatus = rootCauseStatus,
            RootCauseId = finalRootCauseId,
            ResponsibleDepartmentId = command.ResponsibleDepartmentId,
            ResponsibleComponentId = command.ResponsibleComponentId
        });

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "CaseResolved",
            entityType: "Case",
            entityId: command.CaseId.ToString(),
            actorUserId: currentUserId,
            metadataJson: auditDetails
        ), ct);

        return await MapToDtoAsync(resolution, @case, ct);
    }

    public async Task<CaseResolutionDto?> GetResolutionByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var resolution = await _caseResolutionRepository.GetByCaseIdAsync(caseId, ct);
        if (resolution == null) return null;

        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null) return null;

        return await MapToDtoAsync(resolution, @case, ct);
    }

    public async Task<IReadOnlyList<RootCauseDto>> GetRootCausesAsync(CancellationToken ct = default)
    {
        var list = await _caseResolutionRepository.GetAllRootCausesAsync(ct);
        return list.Select(rc => new RootCauseDto(rc.Id, rc.Code, rc.Name, rc.Category, rc.Description)).ToList();
    }

    public async Task<long> CreateRootCauseAsync(string name, string? code, string? category, string? description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("BR-028", "O nome da causa raiz é obrigatório.");

        var rootCause = new RootCause(name, code, category, description);
        return await _caseResolutionRepository.AddRootCauseAsync(rootCause, ct);
    }

    public async Task UpdateRootCauseAsync(long id, string name, string? code, string? category, string? description, CancellationToken ct = default)
    {
        var existing = await _caseResolutionRepository.GetRootCauseByIdAsync(id, ct);
        if (existing == null)
            throw new EntityNotFoundException("Causa Raiz", id);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("BR-028", "O nome da causa raiz é obrigatório.");

        existing.Name = name.Trim();
        existing.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        existing.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        existing.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        await _caseResolutionRepository.UpdateRootCauseAsync(existing, ct);
    }

    public async Task<int> DeleteRootCauseAsync(long id, CancellationToken ct = default)
    {
        var usageCount = await _caseResolutionRepository.CountResolutionsUsingRootCauseAsync(id, ct);
        var deleted = await _caseResolutionRepository.DeleteRootCauseAsync(id, ct);
        if (!deleted)
            throw new EntityNotFoundException("Causa Raiz", id);

        // FK root_cause_id é ON DELETE SET NULL — resoluções que a usavam ficam sem causa
        // raiz corporativa vinculada, mas mantêm o restante do registro intacto.
        return usageCount;
    }

    public Task<int> CountResolutionsUsingRootCauseAsync(long id, CancellationToken ct = default)
    {
        return _caseResolutionRepository.CountResolutionsUsingRootCauseAsync(id, ct);
    }

    public async Task<IReadOnlyList<long>> ApplyResolutionToLinkedCasesAsync(long sourceCaseId, long currentUserId, CancellationToken ct = default)
    {
        var sourceResolution = await _caseResolutionRepository.GetByCaseIdAsync(sourceCaseId, ct);
        if (sourceResolution == null)
        {
            throw new BusinessRuleValidationException("BR-030", "O caso de origem ainda não possui uma resolução registrada.");
        }

        var relations = await _caseRelationRepository.GetRelationsByCaseIdAsync(sourceCaseId, ct);
        var commonCauseTargetIds = relations
            .Where(r => string.Equals(r.RelationType, "CommonCause", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.SourceCaseId == sourceCaseId ? r.TargetCaseId : r.SourceCaseId)
            .Distinct()
            .ToList();

        var closedCaseIds = new List<long>();

        foreach (var targetId in commonCauseTargetIds)
        {
            var applied = await TryApplyResolutionToCaseAsync(sourceResolution, targetId, currentUserId, ct);
            if (applied) closedCaseIds.Add(targetId);
        }

        return closedCaseIds;
    }

    // Vínculo manual imediato (ver OnPostAddRelationAsync em Cases/Details): quando o
    // analista relaciona o caso atual a um caso já Resolvido e opta explicitamente por
    // encerrar o caso atual também, aplica a MESMA resolução do caso já fechado.
    public async Task<bool> ApplyResolutionFromRelatedCaseAsync(long caseId, long relatedResolvedCaseId, long currentUserId, CancellationToken ct = default)
    {
        var relatedResolution = await _caseResolutionRepository.GetByCaseIdAsync(relatedResolvedCaseId, ct);
        if (relatedResolution == null)
            throw new BusinessRuleValidationException("BR-030", "O caso relacionado ainda não possui uma resolução registrada.");

        return await TryApplyResolutionToCaseAsync(relatedResolution, caseId, currentUserId, ct);
    }

    private async Task<bool> TryApplyResolutionToCaseAsync(CaseResolution sourceResolution, long targetCaseId, long currentUserId, CancellationToken ct)
    {
        var targetCase = await _caseRepository.GetByIdAsync(targetCaseId, ct);
        if (targetCase == null) return false;

        bool isOpen = targetCase.Status == "Open" || targetCase.Status == "Reopened" || targetCase.Status == "Investigating";
        if (!isOpen) return false;

        try
        {
            await ResolveCaseAsync(new ResolveCaseCommand(
                CaseId: targetCaseId,
                ResolutionSummary: sourceResolution.ResolutionSummary,
                ValidationSummary: sourceResolution.ValidationSummary,
                RootCauseId: sourceResolution.RootCauseId,
                RootCauseConfirmed: sourceResolution.RootCauseConfirmed,
                ResponsibleDepartmentId: sourceResolution.ResponsibleDepartmentId,
                ResolutionType: sourceResolution.ResolutionType,
                RecurrenceRisk: sourceResolution.RecurrenceRisk,
                RecurrenceNotes: sourceResolution.RecurrenceNotes,
                PreventiveActions: sourceResolution.PreventiveActions
                // RootCauseHypothesisIds propositalmente omitido: hipóteses são específicas
                // de cada caso, os ids do caso de origem não fazem sentido no caso alvo.
            ), currentUserId, ct);

            return true;
        }
        catch (BusinessRuleValidationException)
        {
            // Caso já resolvido/reaberto entre a checagem e a aplicação — não interrompe o fluxo chamador.
            return false;
        }
    }

    private async Task<CaseResolutionDto> MapToDtoAsync(CaseResolution resolution, Case @case, CancellationToken ct)
    {
        string? rootCauseName = null;
        string? rootCauseCode = null;
        string? rootCauseCategory = null;
        if (resolution.RootCauseId.HasValue)
        {
            var rc = await _caseResolutionRepository.GetRootCauseByIdAsync(resolution.RootCauseId.Value, ct);
            if (rc != null)
            {
                rootCauseName = rc.Name;
                rootCauseCode = rc.Code;
                rootCauseCategory = rc.Category;
            }
        }

        string? departmentName = null;
        if (resolution.ResponsibleDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(resolution.ResponsibleDepartmentId.Value, ct);
            departmentName = dept?.Name;
        }

        long? rootCauseCompId = null;
        string? rootCauseCompName = null;
        var rcComp = @case.AffectedComponents.FirstOrDefault(c => c.RelationType == "RootCause");
        if (rcComp != null)
        {
            rootCauseCompId = rcComp.ComponentId;
            var compEntity = await _catalogRepository.GetComponentByIdAsync(rcComp.ComponentId, ct);
            rootCauseCompName = compEntity?.Name;
        }

        string? resolverName = null;
        var user = await _userRepository.GetByIdAsync(resolution.ResolvedBy, ct);
        resolverName = user?.Name;

        // Métricas da Fase 4 reaproveitadas para cálculo de esforço e tentativas
        var steps = await _diagnosticRepository.GetStepsByCaseIdAsync(@case.Id, ct);
        var hypotheses = await _diagnosticRepository.GetHypothesesByCaseIdAsync(@case.Id, ct);

        int totalDiagnosticDuration = steps.Sum(s => s.DurationSeconds ?? 0);
        int failedAttempts = steps.Count(s => s.Outcome == "DidNotWork" || s.Outcome == "ConclusiveRefuted");
        int successfulAttempts = steps.Count(s => s.Outcome == "Worked" || s.Outcome == "PartiallyWorked" || s.Outcome == "ConclusiveSupported");
        int totalHypotheses = hypotheses.Count;

        double elapsedMinutes = (resolution.ResolvedAt - @case.OpenedAt).TotalMinutes;
        if (elapsedMinutes < 0) elapsedMinutes = 0;

        var rootCauseHypotheses = new List<CaseResolutionHypothesisDto>();
        foreach (var hypId in resolution.RootCauseHypothesisIds)
        {
            var h = hypotheses.FirstOrDefault(x => x.Id == hypId)
                ?? await _diagnosticRepository.GetHypothesisByIdAsync(hypId, ct);
            if (h != null) rootCauseHypotheses.Add(new CaseResolutionHypothesisDto(h.Id, h.Title));
        }

        return new CaseResolutionDto(
            Id: resolution.Id,
            CaseId: resolution.CaseId,
            ResolutionSummary: resolution.ResolutionSummary,
            ValidationSummary: resolution.ValidationSummary,
            RootCauseId: resolution.RootCauseId,
            RootCauseName: rootCauseName,
            RootCauseCode: rootCauseCode,
            RootCauseCategory: rootCauseCategory,
            RootCauseConfirmed: resolution.RootCauseConfirmed,
            ResponsibleDepartmentId: resolution.ResponsibleDepartmentId,
            ResponsibleDepartmentName: departmentName,
            ResponsibleComponentId: rootCauseCompId,
            ResponsibleComponentName: rootCauseCompName,
            ResolutionType: resolution.ResolutionType,
            RecurrenceRisk: resolution.RecurrenceRisk,
            RecurrenceNotes: resolution.RecurrenceNotes,
            PreventiveActions: resolution.PreventiveActions,
            EffortMinutes: resolution.EffortMinutes,
            ResolvedBy: resolution.ResolvedBy,
            ResolvedByName: resolverName,
            ResolvedAt: resolution.ResolvedAt,
            ElapsedMinutes: Math.Round(elapsedMinutes, 1),
            TotalDiagnosticDurationSeconds: totalDiagnosticDuration,
            FailedAttemptsCount: failedAttempts,
            SuccessfulAttemptsCount: successfulAttempts,
            TotalHypothesesTestedCount: totalHypotheses,
            RootCauseHypotheses: rootCauseHypotheses
        );
    }
}
