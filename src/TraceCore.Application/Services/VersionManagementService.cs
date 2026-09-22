using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class VersionManagementService : IVersionManagementService
{
    private readonly IProductVersionManagementRepository _repository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditService _auditService;
    private readonly ICaseRelationRepository _caseRelationRepository;
    private readonly ICaseSimilarityScoringService _scoringService;

    public VersionManagementService(
        IProductVersionManagementRepository repository,
        ICatalogRepository catalogRepository,
        IClientRepository clientRepository,
        ICaseRepository caseRepository,
        IAuditService auditService,
        ICaseRelationRepository caseRelationRepository,
        ICaseSimilarityScoringService scoringService)
    {
        _repository = repository;
        _catalogRepository = catalogRepository;
        _clientRepository = clientRepository;
        _caseRepository = caseRepository;
        _auditService = auditService;
        _caseRelationRepository = caseRelationRepository;
        _scoringService = scoringService;
    }

    // ================ Alterações por versão ================

    public async Task<IReadOnlyList<ProductVersionChangeDto>> GetChangesByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct);
        var changes = await _repository.GetChangesByVersionIdAsync(productVersionId, ct);

        var dtos = new List<ProductVersionChangeDto>();
        foreach (var change in changes)
        {
            dtos.Add(await ToChangeDtoAsync(change, version, ct));
        }
        return dtos;
    }

    public async Task<long> CreateChangeAsync(long productVersionId, string changeType, string title, string? description, long? componentId, string? errorCode, long? currentUserId = null, CancellationToken ct = default)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão de produto com ID {productVersionId} não encontrada.");

        changeType = ChangeTypeOrDefault(changeType);
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("O título da alteração é obrigatório.", nameof(title));
        if (componentId.HasValue)
        {
            var component = await _catalogRepository.GetComponentByIdAsync(componentId.Value, ct);
            if (component == null)
                throw new ArgumentException($"Componente com ID {componentId.Value} não existe.", nameof(componentId));
        }

        var change = new ProductVersionChange(productVersionId, changeType, title, description, componentId, errorCode)
        {
            CreatedBy = currentUserId
        };
        var id = await _repository.AddChangeAsync(change, ct);
        change.Id = id;

        await _auditService.RecordAsync(
            action: "product_version_change.create",
            entityType: "product_version_changes",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { change.Id, change.ProductVersionId, change.ChangeType, change.Title, change.ComponentId, change.ErrorCode },
            ct: ct
        );

        return id;
    }

    public async Task UpdateChangeAsync(long id, string changeType, string title, string? description, long? componentId, string? errorCode, long? currentUserId = null, CancellationToken ct = default)
    {
        var change = await _repository.GetChangeByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Alteração com ID {id} não encontrada.");

        changeType = ChangeTypeOrDefault(changeType);
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("O título da alteração é obrigatório.", nameof(title));
        if (componentId.HasValue)
        {
            var component = await _catalogRepository.GetComponentByIdAsync(componentId.Value, ct);
            if (component == null)
                throw new ArgumentException($"Componente com ID {componentId.Value} não existe.", nameof(componentId));
        }

        var before = new { change.ChangeType, change.Title, change.Description, change.ComponentId, change.ErrorCode };
        change.ChangeType = changeType;
        change.Title = title.Trim();
        change.Description = description?.Trim();
        change.ComponentId = componentId;
        change.ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        change.UpdatedAt = DateTime.UtcNow;
        change.UpdatedBy = currentUserId;

        await _repository.UpdateChangeAsync(change, ct);

        await _auditService.RecordAsync(
            action: "product_version_change.update",
            entityType: "product_version_changes",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { change.ChangeType, change.Title, change.Description, change.ComponentId, change.ErrorCode },
            ct: ct
        );
    }

    public async Task<bool> DeleteChangeAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var change = await _repository.GetChangeByIdAsync(id, ct);
        if (change == null)
            return false;

        var deleted = await _repository.DeleteChangeAsync(id, ct);
        if (deleted)
        {
            await _auditService.RecordAsync(
                action: "product_version_change.delete",
                entityType: "product_version_changes",
                entityId: id.ToString(),
                actorUserId: currentUserId,
                before: new { change.Id, change.ChangeType, change.Title },
                ct: ct
            );
        }
        return deleted;
    }

    // ================ Vínculo alteração <-> caso ================

    public async Task<IReadOnlyList<VersionChangeCaseLinkDto>> GetLinkedCasesAsync(long changeId, CancellationToken ct = default)
    {
        var links = await _repository.GetLinkedCasesByChangeIdAsync(changeId, ct);
        var dtos = new List<VersionChangeCaseLinkDto>();
        foreach (var link in links)
        {
            string? summary = null;
            var caseEntity = await _caseRepository.GetByIdAsync(link.CaseId, ct);
            if (caseEntity != null)
            {
                summary = string.IsNullOrWhiteSpace(caseEntity.NormalizedSummary) ? null : caseEntity.NormalizedSummary;
            }

            dtos.Add(new VersionChangeCaseLinkDto(
                ChangeId: link.ProductVersionChangeId,
                CaseId: link.CaseId,
                CaseNumber: caseEntity?.CaseNumber ?? 0,
                CaseSummary: summary,
                CaseStatus: caseEntity?.Status,
                RelationType: link.RelationType,
                RelationTypeLabel: ProductVersionChangeCase.Label(link.RelationType),
                MatchScore: link.MatchScore,
                MatchedFactorsJson: link.MatchedFactorsJson,
                LinkedBy: link.LinkedBy,
                LinkedAt: link.LinkedAt
            ));
        }
        return dtos;
    }

    public async Task LinkCaseAsync(long changeId, long caseId, string relationType, decimal? matchScore = null, string? matchedFactorsJson = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var change = await _repository.GetChangeByIdAsync(changeId, ct)
            ?? throw new KeyNotFoundException($"Alteração com ID {changeId} não encontrada.");

        var caseEntity = await _caseRepository.GetByIdAsync(caseId, ct)
            ?? throw new KeyNotFoundException($"Caso com ID {caseId} não encontrado.");

        relationType = RelationTypeOrDefault(relationType);
        if (await _repository.ChangeCaseLinkExistsAsync(changeId, caseId, relationType, ct))
        {
            throw new InvalidOperationException(
                $"O caso #{caseEntity.CaseNumber} já está vinculado a esta alteração com o vínculo '{relationType}'.");
        }

        var link = new ProductVersionChangeCase(changeId, caseId, relationType, matchScore, matchedFactorsJson, currentUserId);
        await _repository.AddChangeCaseLinkAsync(link, ct);

        // Importante (Fase 1 / regra 10): vincular uma correção a um caso NUNCA altera
        // Case.ProductVersionId — o caso continua registrando a versão em que a ocorrência
        // aconteceu, não a versão que corrigiu.
        await _auditService.RecordAsync(
            action: "product_version_change.case_link",
            entityType: "product_version_change_cases",
            entityId: $"{changeId}:{caseId}:{relationType}",
            actorUserId: currentUserId,
            after: new { ChangeId = changeId, change.ChangeType, change.Title, CaseId = caseId, CaseNumber = caseEntity.CaseNumber, RelationType = relationType, MatchScore = matchScore },
            ct: ct
        );
    }

    public async Task<bool> UnlinkCaseAsync(long changeId, long caseId, string relationType, long? currentUserId = null, CancellationToken ct = default)
    {
        relationType = RelationTypeOrDefault(relationType);
        var removed = await _repository.DeleteChangeCaseLinkAsync(changeId, caseId, relationType, ct);
        if (removed)
        {
            await _auditService.RecordAsync(
                action: "product_version_change.case_unlink",
                entityType: "product_version_change_cases",
                entityId: $"{changeId}:{caseId}:{relationType}",
                actorUserId: currentUserId,
                before: new { ChangeId = changeId, CaseId = caseId, RelationType = relationType },
                ct: ct
            );
        }
        return removed;
    }

    public async Task<IReadOnlyList<VersionChangeCaseSuggestionDto>> SuggestCasesForChangeAsync(
        VersionChangeCaseSuggestionInput input,
        CancellationToken ct = default)
    {
        var targetVersion = await _catalogRepository.GetProductVersionByIdAsync(input.ProductVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão de produto com ID {input.ProductVersionId} não encontrada.");

        var targetReleaseOrder = targetVersion.ReleaseOrder;
        var productId = input.ProductId > 0 ? input.ProductId : targetVersion.ProductId;

        // 1. Busca ampla de candidatos potenciais
        var candidates = await _caseRelationRepository.GetPotentialSimilarCandidatesAsync(
            excludeCaseId: 0,
            clientId: null,
            productId: productId,
            errorCode: input.ErrorCode,
            limit: 50,
            ct: ct);

        // 2. CORREÇÃO DO FILTRO (Gap OR -> AND): produto obrigatório
        var validCandidates = candidates.Where(c => c.ProductId == productId).ToList();

        // Se a alteração já existir, exclui casos já vinculados
        if (input.ProductVersionChangeId.HasValue)
        {
            var existingLinks = await _repository.GetLinkedCasesByChangeIdAsync(input.ProductVersionChangeId.Value, ct);
            var linkedCaseIds = existingLinks.Select(l => l.CaseId).ToHashSet();
            validCandidates = validCandidates.Where(c => !linkedCaseIds.Contains(c.Id)).ToList();
        }

        if (validCandidates.Count == 0)
            return Array.Empty<VersionChangeCaseSuggestionDto>();

        // Cache de versões dos candidatos para cálculo de ReleaseOrder
        var versionCache = new Dictionary<long, ProductVersion?>();
        foreach (var c in validCandidates)
        {
            if (c.ProductVersionId.HasValue && !versionCache.ContainsKey(c.ProductVersionId.Value))
            {
                var v = await _catalogRepository.GetProductVersionByIdAsync(c.ProductVersionId.Value, ct);
                versionCache[c.ProductVersionId.Value] = v;
            }
        }

        var sourceText = _scoringService.BuildSourceText(input.Title, new[] { input.Description ?? string.Empty });
        var sourceCompIds = input.ComponentId.HasValue ? new[] { input.ComponentId.Value } : Array.Empty<long>();

        // 3 e 4. Pontua com o núcleo determinístico:
        // - Sem bônus de "mesma versão" (enableSameVersionBonus = false)
        // - Com sinal de "versão anterior" via ReleaseOrder (+15)
        var scored = _scoringService.ScoreAndRankCandidates(
            candidates: validCandidates,
            sourceClientId: null,
            sourceProductId: productId,
            sourceVersionId: input.ProductVersionId,
            sourceErrorCode: input.ErrorCode,
            sourceComponentIds: sourceCompIds,
            sourceText: sourceText,
            sourceTags: null,
            enableSameVersionBonus: false,
            additionalScorer: candidate =>
            {
                if (!candidate.ProductVersionId.HasValue)
                    return (0, null);

                if (versionCache.TryGetValue(candidate.ProductVersionId.Value, out var candVersion) && candVersion != null)
                {
                    if (candVersion.ReleaseOrder < targetReleaseOrder)
                    {
                        return (15.0, $"Versão anterior ({candVersion.VersionLabel})");
                    }
                }

                return (0, null);
            },
            topCount: 5);

        var dtos = new List<VersionChangeCaseSuggestionDto>();
        foreach (var item in scored)
        {
            string? versionLabel = null;
            if (item.candidate.ProductVersionId.HasValue &&
                versionCache.TryGetValue(item.candidate.ProductVersionId.Value, out var cv) && cv != null)
            {
                versionLabel = cv.VersionLabel;
            }

            dtos.Add(new VersionChangeCaseSuggestionDto(
                CaseId: item.candidate.Id,
                CaseNumber: item.candidate.CaseNumber,
                CaseTitle: item.candidate.NormalizedSummary ?? item.candidate.OriginalReport,
                CaseStatus: item.candidate.Status,
                ProductVersionLabel: versionLabel,
                Score: item.score,
                MatchedFactors: item.factors
            ));
        }

        return dtos;
    }

    // ================ Destinação / rollout ================

    public async Task<IReadOnlyList<ProductVersionAssignmentDto>> GetAssignmentsByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct);
        var assignments = await _repository.GetAssignmentsByVersionIdAsync(productVersionId, ct);

        var dtos = new List<ProductVersionAssignmentDto>();
        foreach (var a in assignments)
        {
            var client = await _clientRepository.GetByIdAsync(a.ClientId, ct);
            var unit = a.ClientUnitId.HasValue ? await _clientRepository.GetUnitByIdAsync(a.ClientUnitId.Value, ct) : null;

            dtos.Add(new ProductVersionAssignmentDto(
                Id: a.Id,
                ProductVersionId: a.ProductVersionId,
                VersionLabel: version?.VersionLabel,
                ClientId: a.ClientId,
                ClientName: client?.Name,
                ClientUnitId: a.ClientUnitId,
                ClientUnitName: unit?.Name,
                Status: a.Status,
                StatusLabel: ProductVersionAssignment.Label(a.Status),
                PlannedAt: a.PlannedAt,
                ScheduledAt: a.ScheduledAt,
                DeployedAt: a.DeployedAt,
                Notes: a.Notes,
                CreatedAt: a.CreatedAt,
                CreatedBy: a.CreatedBy,
                UpdatedAt: a.UpdatedAt,
                UpdatedBy: a.UpdatedBy
            ));
        }
        return dtos;
    }

    public async Task<long> CreateAssignmentAsync(long productVersionId, long clientId, long? clientUnitId, string? notes = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão de produto com ID {productVersionId} não encontrada.");

        var client = await _clientRepository.GetByIdAsync(clientId, ct)
            ?? throw new KeyNotFoundException($"Cliente com ID {clientId} não encontrado.");

        if (clientUnitId.HasValue)
        {
            var unit = await _clientRepository.GetUnitByIdAsync(clientUnitId.Value, ct);
            if (unit == null || unit.ClientId != clientId)
                throw new ArgumentException("A unidade informada não pertence ao cliente.", nameof(clientUnitId));
        }

        // Regra Fase 1: evita duplicate assignment da mesma versão para o mesmo
        // cliente/unidade (unidade nula = "cliente inteiro").
        if (await _repository.AssignmentExistsAsync(productVersionId, clientId, clientUnitId, ct))
        {
            throw new InvalidOperationException(
                $"Já existe uma destinação da versão '{version.VersionLabel}' para o cliente '{client.Name}'" +
                (clientUnitId.HasValue ? " nesta unidade." : " (cliente inteiro)."));
        }

        var assignment = new ProductVersionAssignment(productVersionId, clientId, clientUnitId, notes, currentUserId);
        var id = await _repository.AddAssignmentAsync(assignment, ct);
        assignment.Id = id;

        // Criar uma destinação NÃO muda a versão corrente do cliente. Somente o deploy
        // confirmado (status -> Deployed) atualiza ClientTechnicalContext.ProductVersionId.
        await _auditService.RecordAsync(
            action: "product_version_assignment.create",
            entityType: "product_version_assignments",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { assignment.Id, assignment.ProductVersionId, assignment.ClientId, assignment.ClientUnitId, assignment.Status, assignment.PlannedAt },
            ct: ct
        );

        return id;
    }

    public async Task ConfirmAssignmentDeployedAsync(long assignmentId, long? environmentId = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            return;

        var before = new { assignment.Status, assignment.ScheduledAt, assignment.DeployedAt };
        var deploymentTime = DateTime.UtcNow;

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            // Regra Fase 2 (1): Deploy confirmado fecha o contexto anterior com EffectiveTo e cria um novo com EffectiveFrom.
            await TransitionClientCurrentVersionAsync(
                assignment.ClientId,
                assignment.ProductVersionId,
                assignment.ClientUnitId,
                environmentId,
                deploymentTime,
                assignment.Id,
                currentUserId,
                ct);

            assignment.Status = "Deployed";
            assignment.DeployedAt = deploymentTime;
            assignment.UpdatedAt = deploymentTime;
            assignment.UpdatedBy = currentUserId;

            await _repository.UpdateAssignmentAsync(assignment, ct);

            scope.Complete();
        }

        await _auditService.RecordAsync(
            action: "product_version_assignment.update",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { assignment.Status, assignment.DeployedAt },
            metadata: new { ClientVersionUpdated = true },
            ct: ct
        );
    }

    public async Task ScheduleAssignmentAsync(long assignmentId, DateTime scheduledAt, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            throw new InvalidOperationException("Não é possível agendar uma destinação já implantada.");

        var before = new { assignment.Status, assignment.ScheduledAt };
        assignment.Status = "Scheduled";
        assignment.ScheduledAt = scheduledAt;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = currentUserId;

        await _repository.UpdateAssignmentAsync(assignment, ct);

        await _auditService.RecordAsync(
            action: "product_version_assignment.update",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { assignment.Status, assignment.ScheduledAt },
            ct: ct
        );
    }

    public async Task FailAssignmentAsync(long assignmentId, string? notes = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            throw new InvalidOperationException("Não é possível marcar como falha uma destinação já implantada.");

        var before = new { assignment.Status, assignment.Notes };
        assignment.Status = "Failed";
        if (!string.IsNullOrWhiteSpace(notes))
        {
            assignment.Notes = string.IsNullOrWhiteSpace(assignment.Notes)
                ? notes.Trim()
                : $"{assignment.Notes}\n[Falha]: {notes.Trim()}";
        }
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = currentUserId;

        await _repository.UpdateAssignmentAsync(assignment, ct);

        await _auditService.RecordAsync(
            action: "product_version_assignment.update",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { assignment.Status, assignment.Notes },
            ct: ct
        );
    }

    public async Task RemoveAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            throw new InvalidOperationException("Não é possível remover uma destinação já implantada.");

        await _repository.DeleteAssignmentAsync(assignmentId, ct);

        await _auditService.RecordAsync(
            action: "product_version_assignment.delete",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: new { assignment.Id, assignment.ProductVersionId, assignment.ClientId, assignment.Status },
            ct: ct
        );
    }

    public async Task ReopenAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            throw new InvalidOperationException("Não é possível reabrir uma destinação já implantada.");

        var before = new { assignment.Status };
        assignment.Status = "Planned";
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = currentUserId;

        await _repository.UpdateAssignmentAsync(assignment, ct);

        await _auditService.RecordAsync(
            action: "product_version_assignment.update",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { assignment.Status },
            metadata: new { Reopened = true },
            ct: ct
        );
    }

    public async Task SkipAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default)
    {
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId, ct)
            ?? throw new KeyNotFoundException($"Destinação com ID {assignmentId} não encontrada.");

        if (assignment.Status == "Deployed")
            throw new InvalidOperationException("Não é possível ignorar uma destinação já implantada.");

        var before = new { assignment.Status };
        assignment.Status = "Skipped";
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = currentUserId;

        await _repository.UpdateAssignmentAsync(assignment, ct);

        await _auditService.RecordAsync(
            action: "product_version_assignment.update",
            entityType: "product_version_assignments",
            entityId: assignmentId.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { assignment.Status },
            ct: ct
        );
    }

    // ================ Atualização manual fora do rollout ================

    public async Task ManualClientVersionUpdateAsync(long clientId, long productId, long newProductVersionId, long? clientUnitId = null, long? environmentId = null, DateTime? effectiveFrom = null, string? notes = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct)
            ?? throw new KeyNotFoundException($"Cliente com ID {clientId} não encontrado.");

        var product = await _catalogRepository.GetProductByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        var version = await _catalogRepository.GetProductVersionByIdAsync(newProductVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão de produto com ID {newProductVersionId} não encontrada.");

        if (version.ProductId != productId)
            throw new ArgumentException("A versão selecionada não pertence ao produto informado.", nameof(newProductVersionId));

        if (clientUnitId.HasValue)
        {
            var unit = await _clientRepository.GetUnitByIdAsync(clientUnitId.Value, ct);
            if (unit == null || unit.ClientId != clientId)
                throw new ArgumentException("A unidade informada não pertence ao cliente.", nameof(clientUnitId));
        }

        var effectiveTime = effectiveFrom ?? DateTime.UtcNow;

        // Se existir um ProductVersionAssignment Planned ou Scheduled para essa combinação, marca como Deployed
        var clientAssignments = await _repository.GetAssignmentsByClientIdAsync(clientId, ct);
        var pendingAssignment = clientAssignments.FirstOrDefault(a =>
            a.ProductVersionId == newProductVersionId &&
            (a.ClientUnitId ?? 0) == (clientUnitId ?? 0) &&
            (a.Status == "Planned" || a.Status == "Scheduled"));

        if (pendingAssignment != null)
        {
            if (!string.IsNullOrWhiteSpace(notes))
            {
                pendingAssignment.Notes = string.IsNullOrWhiteSpace(pendingAssignment.Notes)
                    ? notes.Trim()
                    : $"{pendingAssignment.Notes}\n{notes.Trim()}";
                await _repository.UpdateAssignmentAsync(pendingAssignment, ct);
            }

            await ConfirmAssignmentDeployedAsync(pendingAssignment.Id, environmentId, currentUserId, ct);
        }
        else
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await TransitionClientCurrentVersionAsync(
                clientId,
                newProductVersionId,
                clientUnitId,
                environmentId,
                effectiveTime,
                assignmentId: null,
                currentUserId,
                ct);

            scope.Complete();
        }
    }

    // ================ Helpers ================

    private async Task TransitionClientCurrentVersionAsync(
        long clientId,
        long productVersionId,
        long? clientUnitId,
        long? environmentId,
        DateTime deploymentTime,
        long? assignmentId,
        long? updatedBy,
        CancellationToken ct)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão de produto com ID {productVersionId} não encontrada.");

        var activeContexts = await GetActiveTechnicalContextsAsync(clientId, version.ProductId, clientUnitId, ct);

        ClientTechnicalContext? targetContext = null;

        if (environmentId.HasValue)
        {
            targetContext = activeContexts.FirstOrDefault(c => c.EnvironmentId == environmentId.Value);
        }
        else
        {
            var distinctEnvs = activeContexts.Select(c => c.EnvironmentId).Distinct().Count();
            if (distinctEnvs > 1)
            {
                throw new BusinessRuleValidationException("BR-VERSION-001",
                    "Existem múltiplos ambientes ativos para este sistema no cliente/unidade. Informe o ambiente desejado para a confirmação de deploy.");
            }

            targetContext = activeContexts.OrderByDescending(c => c.EffectiveFrom).FirstOrDefault();
        }

        if (targetContext == null)
        {
            // Não existe contexto ativo anterior para esse produto/ambiente: cria o primeiro
            var initialContext = new ClientTechnicalContext(
                clientId: clientId,
                productId: version.ProductId,
                clientUnitId: clientUnitId,
                productVersionId: productVersionId,
                environmentId: environmentId,
                status: "Active",
                effectiveFrom: deploymentTime,
                effectiveTo: null,
                createdBy: updatedBy
            );
            var initialId = await _clientRepository.AddTechnicalContextAsync(initialContext, ct);

            await _auditService.RecordAsync(
                action: "client.technical_context_create",
                entityType: "client_technical_contexts",
                entityId: initialId.ToString(),
                actorUserId: updatedBy,
                after: new { initialContext.Id, initialContext.ClientId, initialContext.ProductId, initialContext.ProductVersionId, initialContext.EnvironmentId, initialContext.EffectiveFrom },
                metadata: new { Source = "product_version_assignment_deploy", AssignmentId = assignmentId, IsInitialContext = true },
                ct: ct
            );
            return;
        }

        // Idempotência: se já está na mesma versão, não fecha nem recria
        if (targetContext.ProductVersionId == productVersionId)
            return;

        // 1. Fecha o contexto atual preenchendo EffectiveTo
        var beforeClose = new { targetContext.ProductVersionId, targetContext.EffectiveTo };
        targetContext.EffectiveTo = deploymentTime;
        targetContext.UpdatedAt = deploymentTime;
        targetContext.UpdatedBy = updatedBy;

        await _clientRepository.UpdateTechnicalContextAsync(targetContext, ct);

        await _auditService.RecordAsync(
            action: "client.technical_context_close",
            entityType: "client_technical_contexts",
            entityId: targetContext.Id.ToString(),
            actorUserId: updatedBy,
            before: beforeClose,
            after: new { targetContext.EffectiveTo },
            metadata: new { Source = "product_version_assignment_deploy", AssignmentId = assignmentId, ClosedForVersionId = productVersionId },
            ct: ct
        );

        // 2. Cria o novo contexto técnico mantendo o mesmo ambiente e unidade, com EffectiveFrom = deploymentTime
        var newContext = new ClientTechnicalContext(
            clientId: targetContext.ClientId,
            productId: targetContext.ProductId,
            clientUnitId: targetContext.ClientUnitId,
            productVersionId: productVersionId,
            environmentId: targetContext.EnvironmentId,
            status: "Active",
            effectiveFrom: deploymentTime,
            effectiveTo: null,
            createdBy: updatedBy
        );
        var newId = await _clientRepository.AddTechnicalContextAsync(newContext, ct);

        await _auditService.RecordAsync(
            action: "client.technical_context_create",
            entityType: "client_technical_contexts",
            entityId: newId.ToString(),
            actorUserId: updatedBy,
            after: new { newContext.Id, newContext.ClientId, newContext.ProductId, newContext.ProductVersionId, newContext.EnvironmentId, newContext.EffectiveFrom },
            metadata: new { Source = "product_version_assignment_deploy", AssignmentId = assignmentId, PreviousContextId = targetContext.Id },
            ct: ct
        );
    }

    private async Task<ProductVersionChangeDto> ToChangeDtoAsync(ProductVersionChange change, ProductVersion? version, CancellationToken ct)
    {
        string? componentName = null;
        if (change.ComponentId.HasValue)
        {
            var comp = await _catalogRepository.GetComponentByIdAsync(change.ComponentId.Value, ct);
            componentName = comp?.Name;
        }

        var links = await GetLinkedCasesAsync(change.Id, ct);

        return new ProductVersionChangeDto(
            Id: change.Id,
            ProductVersionId: change.ProductVersionId,
            VersionLabel: version?.VersionLabel,
            ChangeType: change.ChangeType,
            ChangeTypeLabel: ProductVersionChange.Label(change.ChangeType),
            Title: change.Title,
            Description: change.Description,
            ComponentId: change.ComponentId,
            ComponentName: componentName,
            ErrorCode: change.ErrorCode,
            CreatedAt: change.CreatedAt,
            CreatedBy: change.CreatedBy,
            UpdatedAt: change.UpdatedAt,
            UpdatedBy: change.UpdatedBy,
            LinkedCases: links
        );
    }

    private static string ChangeTypeOrDefault(string? changeType)
    {
        var normalized = string.IsNullOrWhiteSpace(changeType) ? "Improvement" : changeType.Trim();
        if (!ProductVersionChange.ValidChangeTypes.Contains(normalized, StringComparer.Ordinal))
            throw new ArgumentException(
                $"Tipo de alteração inválido. Valores aceitos: {string.Join(", ", ProductVersionChange.ValidChangeTypes)}.",
                nameof(changeType));
        return normalized;
    }

    private static string RelationTypeOrDefault(string? relationType)
    {
        var normalized = string.IsNullOrWhiteSpace(relationType) ? "FixedBy" : relationType.Trim();
        if (!ProductVersionChangeCase.ValidRelationTypes.Contains(normalized, StringComparer.Ordinal))
            throw new ArgumentException(
                $"Tipo de vínculo inválido. Valores aceitos nesta fase: {string.Join(", ", ProductVersionChangeCase.ValidRelationTypes)}.",
                nameof(relationType));
        return normalized;
    }

    // ================ Versão Ativa do Cliente e Sugestão de Correções (Fase 4) ================

    private async Task<List<ClientTechnicalContext>> GetActiveTechnicalContextsAsync(
        long clientId,
        long productId,
        long? clientUnitId,
        CancellationToken ct)
    {
        var ctxs = await _clientRepository.GetTechnicalContextsByClientIdAsync(clientId, ct);
        return ctxs
            .Where(c => c.ProductId == productId
                     && (c.ClientUnitId ?? 0) == (clientUnitId ?? 0)
                     && c.EffectiveTo == null)
            .ToList();
    }

    public async Task<ClientActiveVersionDto> GetActiveVersionForClientAsync(
        long clientId,
        long productId,
        long? clientUnitId = null,
        CancellationToken ct = default)
    {
        var activeContexts = await GetActiveTechnicalContextsAsync(clientId, productId, clientUnitId, ct);

        if (activeContexts.Count == 0)
        {
            return new ClientActiveVersionDto(
                ProductVersionId: null,
                VersionLabel: null,
                EnvironmentId: null,
                EnvironmentName: null,
                IsAmbiguous: false,
                AmbiguousEnvironments: Array.Empty<AmbiguousEnvironmentDto>(),
                HasContext: false
            );
        }

        var distinctEnvs = activeContexts.Select(c => c.EnvironmentId).Distinct().Count();
        if (distinctEnvs > 1)
        {
            var ambiguousList = new List<AmbiguousEnvironmentDto>();
            foreach (var ctx in activeContexts)
            {
                var env = ctx.EnvironmentId.HasValue ? await _catalogRepository.GetEnvironmentByIdAsync(ctx.EnvironmentId.Value, ct) : null;
                var ver = ctx.ProductVersionId.HasValue ? await _catalogRepository.GetProductVersionByIdAsync(ctx.ProductVersionId.Value, ct) : null;
                ambiguousList.Add(new AmbiguousEnvironmentDto(
                    EnvironmentId: ctx.EnvironmentId,
                    EnvironmentName: env?.Name,
                    ProductVersionId: ctx.ProductVersionId,
                    VersionLabel: ver?.VersionLabel
                ));
            }

            return new ClientActiveVersionDto(
                ProductVersionId: null,
                VersionLabel: null,
                EnvironmentId: null,
                EnvironmentName: null,
                IsAmbiguous: true,
                AmbiguousEnvironments: ambiguousList,
                HasContext: true
            );
        }

        var target = activeContexts.OrderByDescending(c => c.EffectiveFrom).First();
        var targetEnv = target.EnvironmentId.HasValue ? await _catalogRepository.GetEnvironmentByIdAsync(target.EnvironmentId.Value, ct) : null;
        var targetVer = target.ProductVersionId.HasValue ? await _catalogRepository.GetProductVersionByIdAsync(target.ProductVersionId.Value, ct) : null;

        return new ClientActiveVersionDto(
            ProductVersionId: target.ProductVersionId,
            VersionLabel: targetVer?.VersionLabel,
            EnvironmentId: target.EnvironmentId,
            EnvironmentName: targetEnv?.Name,
            IsAmbiguous: false,
            AmbiguousEnvironments: Array.Empty<AmbiguousEnvironmentDto>(),
            HasContext: true
        );
    }

    public async Task<IReadOnlyList<PossibleFixSuggestionDto>> SuggestPossibleFixesAsync(
        long productId,
        long? currentProductVersionId,
        string? title,
        string? description,
        long? componentId,
        string? errorCode,
        CancellationToken ct = default)
    {
        if (!currentProductVersionId.HasValue)
        {
            return Array.Empty<PossibleFixSuggestionDto>();
        }

        var currentVersion = await _catalogRepository.GetProductVersionByIdAsync(currentProductVersionId.Value, ct);
        if (currentVersion == null)
        {
            return Array.Empty<PossibleFixSuggestionDto>();
        }

        var minReleaseOrder = currentVersion.ReleaseOrder;
        var fixChanges = await _repository.GetFixChangesForProductAsync(productId, minReleaseOrder, ct);
        if (fixChanges.Count == 0)
        {
            return Array.Empty<PossibleFixSuggestionDto>();
        }

        // Extrai palavras significativas do título e descrição informados
        var sourceText = _scoringService.BuildSourceText(title ?? string.Empty, new[] { description ?? string.Empty });
        var sourceWords = _scoringService.ExtractSignificantWords(sourceText);

        var rankedList = new List<PossibleFixSuggestionDto>();

        foreach (var change in fixChanges)
        {
            double score = 0;
            var factors = new List<string>();

            // 1. Mesmo código de erro (+35)
            if (!string.IsNullOrWhiteSpace(errorCode) && !string.IsNullOrWhiteSpace(change.ErrorCode) &&
                string.Equals(errorCode.Trim(), change.ErrorCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                score += 35;
                factors.Add("Código de erro idêntico (+35)");
            }

            // 2. Mesmo componente (+25)
            if (componentId.HasValue && change.ComponentId.HasValue && componentId.Value == change.ComponentId.Value)
            {
                score += 25;
                factors.Add("Componente idêntico (+25)");
            }

            // 3. Sobreposição de termos (+5 por termo, max +20)
            var changeText = _scoringService.BuildSourceText(change.Title, new[] { change.Description ?? string.Empty });
            var changeWords = _scoringService.ExtractSignificantWords(changeText);
            var overlapWords = sourceWords.Intersect(changeWords, StringComparer.OrdinalIgnoreCase).Count();
            if (overlapWords > 0)
            {
                var termBonus = Math.Min(overlapWords * 5.0, 20.0);
                score += termBonus;
                factors.Add($"Sobreposição de termos (+{termBonus:0})");
            }

            // Cap máximo no score: 100 pontos
            score = Math.Min(score, 100.0);

            if (score > 0)
            {
                var changeVersion = await _catalogRepository.GetProductVersionByIdAsync(change.ProductVersionId, ct);
                var linkedCases = await _repository.GetLinkedCasesByChangeIdAsync(change.Id, ct);

                rankedList.Add(new PossibleFixSuggestionDto(
                    ChangeId: change.Id,
                    ProductVersionId: change.ProductVersionId,
                    VersionLabel: changeVersion?.VersionLabel ?? "—",
                    ChangeTitle: change.Title,
                    ChangeDescription: change.Description,
                    LinkedCaseCount: linkedCases.Count,
                    Score: Math.Round((decimal)score, 2),
                    MatchedFactors: factors
                ));
            }
        }

        return rankedList
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.ChangeId)
            .Take(5)
            .ToList();
    }

    public async Task<CaseVersionContextDto> GetVersionContextForCaseAsync(long caseId, CancellationToken ct = default)
    {
        var caseEntity = await _caseRepository.GetByIdAsync(caseId, ct)
            ?? throw new KeyNotFoundException($"Caso com ID {caseId} não encontrado.");

        ProductVersion? incidentVersion = null;
        if (caseEntity.ProductVersionId.HasValue)
        {
            incidentVersion = await _catalogRepository.GetProductVersionByIdAsync(caseEntity.ProductVersionId.Value, ct);
        }

        // Correções confirmadas (FixedBy / Related)
        var linkedChangeCases = await _repository.GetChangesLinkedToCaseAsync(caseId, ct);
        var confirmedFixes = new List<CaseConfirmedFixDto>();
        var confirmedChangeIds = new HashSet<long>();

        foreach (var l in linkedChangeCases)
        {
            var chg = await _repository.GetChangeByIdAsync(l.ProductVersionChangeId, ct);
            if (chg != null)
            {
                var ver = await _catalogRepository.GetProductVersionByIdAsync(chg.ProductVersionId, ct);
                confirmedFixes.Add(new CaseConfirmedFixDto(
                    ChangeId: chg.Id,
                    ProductVersionId: chg.ProductVersionId,
                    VersionLabel: ver?.VersionLabel ?? "—",
                    ChangeTitle: chg.Title,
                    RelationType: ProductVersionChangeCase.Label(l.RelationType),
                    LinkedAt: l.LinkedAt
                ));
                confirmedChangeIds.Add(chg.Id);
            }
        }

        // Possíveis correções posteriores
        IReadOnlyList<PossibleFixSuggestionDto> possibleFixes = Array.Empty<PossibleFixSuggestionDto>();
        if (caseEntity.ProductId.HasValue && caseEntity.ProductVersionId.HasValue)
        {
            var primaryComponentId = caseEntity.AffectedComponents.FirstOrDefault()?.ComponentId;
            var rawPossibleFixes = await SuggestPossibleFixesAsync(
                productId: caseEntity.ProductId.Value,
                currentProductVersionId: caseEntity.ProductVersionId,
                title: caseEntity.NormalizedSummary ?? caseEntity.OriginalReport,
                description: caseEntity.OriginalReport,
                componentId: primaryComponentId,
                errorCode: caseEntity.ErrorCode,
                ct: ct
            );

            // Exclui as que já estão confirmadas para mostrar separadamente
            possibleFixes = rawPossibleFixes
                .Where(pf => !confirmedChangeIds.Contains(pf.ChangeId))
                .ToList();
        }

        return new CaseVersionContextDto(
            ProductVersionId: caseEntity.ProductVersionId,
            VersionLabel: incidentVersion?.VersionLabel,
            ReleaseOrder: incidentVersion?.ReleaseOrder,
            ConfirmedFixes: confirmedFixes,
            PossibleFixes: possibleFixes
        );
    }

    // ================ Consolidação, Indicadores, Recorrência e Copiloto (Fase 5) ================

    public async Task<VersionIndicatorsDto> GetVersionIndicatorsAsync(long productVersionId, CancellationToken ct = default)
    {
        var version = await _catalogRepository.GetProductVersionByIdAsync(productVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão com ID {productVersionId} não encontrada.");

        var caseCounts = await _repository.GetCaseCountsByVersionIdsAsync(new[] { productVersionId }, ct);
        int casesOccurred = caseCounts.TryGetValue(productVersionId, out var count) ? count : 0;

        var changes = await _repository.GetChangesByVersionIdAsync(productVersionId, ct);
        int publishedFixes = changes.Count(c => string.Equals(c.ChangeType, "Fix", StringComparison.OrdinalIgnoreCase));

        var uniqueLinkedCaseIds = new HashSet<long>();
        foreach (var ch in changes)
        {
            var links = await _repository.GetLinkedCasesByChangeIdAsync(ch.Id, ct);
            foreach (var l in links)
            {
                uniqueLinkedCaseIds.Add(l.CaseId);
            }
        }

        var assignments = await _repository.GetAssignmentsByVersionIdAsync(productVersionId, ct);
        int planned = assignments.Count(a => string.Equals(a.Status, "Planned", StringComparison.OrdinalIgnoreCase));
        int scheduled = assignments.Count(a => string.Equals(a.Status, "Scheduled", StringComparison.OrdinalIgnoreCase));
        int deployed = assignments.Count(a => string.Equals(a.Status, "Deployed", StringComparison.OrdinalIgnoreCase));
        int pending = planned + scheduled;

        return new VersionIndicatorsDto(
            ProductVersionId: version.Id,
            VersionLabel: version.VersionLabel,
            ReleaseOrder: version.ReleaseOrder,
            CasesOccurredCount: casesOccurred,
            PublishedFixesCount: publishedFixes,
            LinkedCasesCount: uniqueLinkedCaseIds.Count,
            PlannedClientsCount: planned,
            ScheduledClientsCount: scheduled,
            DeployedClientsCount: deployed,
            PendingClientsCount: pending
        );
    }

    public async Task<IReadOnlyList<VersionIndicatorsDto>> GetVersionIndicatorsForProductAsync(long productId, CancellationToken ct = default)
    {
        var versions = await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
        var sortedVersions = versions.OrderBy(v => v.ReleaseOrder).ToList();
        var versionIds = sortedVersions.Select(v => v.Id).ToList();

        var caseCounts = await _repository.GetCaseCountsByVersionIdsAsync(versionIds, ct);

        var list = new List<VersionIndicatorsDto>();
        foreach (var v in sortedVersions)
        {
            int casesOccurred = caseCounts.TryGetValue(v.Id, out var count) ? count : 0;
            var changes = await _repository.GetChangesByVersionIdAsync(v.Id, ct);
            int publishedFixes = changes.Count(c => string.Equals(c.ChangeType, "Fix", StringComparison.OrdinalIgnoreCase));

            var uniqueLinkedCaseIds = new HashSet<long>();
            foreach (var ch in changes)
            {
                var links = await _repository.GetLinkedCasesByChangeIdAsync(ch.Id, ct);
                foreach (var l in links)
                {
                    uniqueLinkedCaseIds.Add(l.CaseId);
                }
            }

            var assignments = await _repository.GetAssignmentsByVersionIdAsync(v.Id, ct);
            int planned = assignments.Count(a => string.Equals(a.Status, "Planned", StringComparison.OrdinalIgnoreCase));
            int scheduled = assignments.Count(a => string.Equals(a.Status, "Scheduled", StringComparison.OrdinalIgnoreCase));
            int deployed = assignments.Count(a => string.Equals(a.Status, "Deployed", StringComparison.OrdinalIgnoreCase));
            int pending = planned + scheduled;

            list.Add(new VersionIndicatorsDto(
                ProductVersionId: v.Id,
                VersionLabel: v.VersionLabel,
                ReleaseOrder: v.ReleaseOrder,
                CasesOccurredCount: casesOccurred,
                PublishedFixesCount: publishedFixes,
                LinkedCasesCount: uniqueLinkedCaseIds.Count,
                PlannedClientsCount: planned,
                ScheduledClientsCount: scheduled,
                DeployedClientsCount: deployed,
                PendingClientsCount: pending
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<VersionCaseCountDto>> GetCasesCountByVersionForProductAsync(long productId, CancellationToken ct = default)
    {
        var versions = await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
        var sortedVersions = versions.OrderBy(v => v.ReleaseOrder).ToList();
        var versionIds = sortedVersions.Select(v => v.Id).ToList();

        var caseCounts = await _repository.GetCaseCountsByVersionIdsAsync(versionIds, ct);

        return sortedVersions.Select(v => new VersionCaseCountDto(
            ProductVersionId: v.Id,
            VersionLabel: v.VersionLabel,
            ReleaseOrder: v.ReleaseOrder,
            CaseCount: caseCounts.TryGetValue(v.Id, out var c) ? c : 0
        )).ToList();
    }

    public async Task<IReadOnlyList<VersionLinkedCaseDetailDto>> GetVersionLinkedCasesAsync(long productVersionId, CancellationToken ct = default)
    {
        var rawList = await _repository.GetLinkedCaseDetailsByVersionIdAsync(productVersionId, ct);
        return rawList.Select(r => new VersionLinkedCaseDetailDto(
            ChangeId: r.ChangeId,
            ChangeTitle: r.ChangeTitle,
            ChangeType: r.ChangeType,
            ChangeTypeLabel: ProductVersionChange.Label(r.ChangeType),
            CaseId: r.CaseId,
            CaseNumber: r.CaseNumber,
            CaseTitle: r.CaseTitle,
            CaseStatus: r.CaseStatus,
            ClientId: r.ClientId,
            ClientName: r.ClientName,
            OccurredInVersionId: r.OccurredInVersionId,
            OccurredInVersionLabel: r.OccurredInVersionLabel,
            RelationType: r.RelationType,
            RelationTypeLabel: ProductVersionChangeCase.Label(r.RelationType),
            LinkedAt: r.LinkedAt,
            CaseOpenedAt: r.CaseOpenedAt
        )).ToList();
    }

    public async Task<FixRecurrenceObservationDto> GetFixRecurrenceAsync(long changeId, CancellationToken ct = default)
    {
        var change = await _repository.GetChangeByIdAsync(changeId, ct)
            ?? throw new KeyNotFoundException($"Alteração com ID {changeId} não encontrada.");

        var version = await _catalogRepository.GetProductVersionByIdAsync(change.ProductVersionId, ct)
            ?? throw new KeyNotFoundException($"Versão com ID {change.ProductVersionId} não encontrada.");

        var links = await _repository.GetLinkedCasesByChangeIdAsync(changeId, ct);
        int historicalCasesCount = links.Count;

        var assignments = await _repository.GetAssignmentsByVersionIdAsync(version.Id, ct);
        int deployedClientsCount = assignments.Count(a => string.Equals(a.Status, "Deployed", StringComparison.OrdinalIgnoreCase));

        DateTime releaseDate = version.ReleasedAt ?? change.CreatedAt;
        int postReleaseOccurrences = await _repository.GetPostReleaseSimilarCasesCountAsync(
            version.ProductId,
            version.Id,
            change.ErrorCode,
            change.ComponentId,
            releaseDate,
            ct);

        string message = postReleaseOccurrences switch
        {
            0 => "Nenhuma ocorrência semelhante registrada após a liberação da versão.",
            1 => "1 ocorrência semelhante foi registrada após adoção da versão.",
            _ => $"{postReleaseOccurrences} ocorrências semelhantes foram registradas após adoção da versão."
        };

        string? componentName = null;
        if (change.ComponentId.HasValue)
        {
            var comp = await _catalogRepository.GetComponentByIdAsync(change.ComponentId.Value, ct);
            componentName = comp?.Name;
        }

        return new FixRecurrenceObservationDto(
            ChangeId: change.Id,
            ChangeTitle: change.Title,
            ProductVersionId: version.Id,
            VersionLabel: version.VersionLabel,
            ErrorCode: change.ErrorCode,
            ComponentId: change.ComponentId,
            ComponentName: componentName,
            HistoricalCasesCount: historicalCasesCount,
            DeployedClientsCount: deployedClientsCount,
            PostReleaseOccurrencesCount: postReleaseOccurrences,
            ObservationMessage: message
        );
    }

    public async Task<ClientVersionTimelineDto> GetClientVersionTimelineWithCasesAsync(long clientId, long productId, long? clientUnitId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct)
            ?? throw new KeyNotFoundException($"Cliente com ID {clientId} não encontrado.");

        var product = await _catalogRepository.GetProductByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        string? unitName = null;
        if (clientUnitId.HasValue)
        {
            var unit = await _clientRepository.GetUnitByIdAsync(clientUnitId.Value, ct);
            unitName = unit?.Name;
        }

        var allContexts = await _clientRepository.GetTechnicalContextsByClientIdAsync(clientId, ct);
        var contexts = allContexts
            .Where(c => c.ProductId == productId && (c.ClientUnitId ?? 0) == (clientUnitId ?? 0))
            .OrderBy(c => c.EffectiveFrom)
            .ToList();

        var environments = (await _catalogRepository.GetAllEnvironmentsAsync(ct)).ToDictionary(e => e.Id, e => e.Name);
        var versions = (await _catalogRepository.GetVersionsByProductIdAsync(productId, ct)).ToDictionary(v => v.Id, v => v.VersionLabel);

        var periods = new List<ClientVersionPeriodDto>();
        foreach (var ctx in contexts)
        {
            var casesInPeriod = await _repository.GetCasesByClientAndPeriodAsync(clientId, productId, ctx.EffectiveFrom, ctx.EffectiveTo, ct);
            var caseDtos = casesInPeriod.Select(c => new ClientPeriodCaseDto(
                CaseId: c.CaseId,
                CaseNumber: c.CaseNumber,
                Title: c.Title,
                Status: c.Status,
                ErrorCode: c.ErrorCode,
                OpenedAt: c.OpenedAt
            )).ToList();

            string? versionLabel = ctx.ProductVersionId.HasValue && versions.TryGetValue(ctx.ProductVersionId.Value, out var vl) ? vl : null;
            string? envName = ctx.EnvironmentId.HasValue && environments.TryGetValue(ctx.EnvironmentId.Value, out var en) ? en : null;

            periods.Add(new ClientVersionPeriodDto(
                ProductVersionId: ctx.ProductVersionId,
                VersionLabel: versionLabel,
                EnvironmentId: ctx.EnvironmentId,
                EnvironmentName: envName,
                EffectiveFrom: ctx.EffectiveFrom,
                EffectiveTo: ctx.EffectiveTo,
                IsCurrent: ctx.EffectiveTo == null,
                CasesOccurred: caseDtos
            ));
        }

        return new ClientVersionTimelineDto(
            ClientId: client.Id,
            ClientName: client.Name,
            ProductId: product.Id,
            ProductName: product.Name,
            ClientUnitId: clientUnitId,
            ClientUnitName: unitName,
            Periods: periods
        );
    }

    public async Task<ClientVersionCopilotContextDto> GetClientVersionCopilotContextAsync(long clientId, long productId, long? clientUnitId = null, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct)
            ?? throw new KeyNotFoundException($"Cliente com ID {clientId} não encontrado.");

        var product = await _catalogRepository.GetProductByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        var activeVersion = await GetActiveVersionForClientAsync(clientId, productId, clientUnitId, ct);
        var allVersions = await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
        var sortedVersions = allVersions.OrderBy(v => v.ReleaseOrder).ToList();

        ProductVersion? currentVersionEntity = null;
        if (activeVersion.ProductVersionId.HasValue)
        {
            currentVersionEntity = sortedVersions.FirstOrDefault(v => v.Id == activeVersion.ProductVersionId.Value);
        }

        VersionReleaseSummaryDto? currentVersionDto = currentVersionEntity != null
            ? new VersionReleaseSummaryDto(currentVersionEntity.Id, currentVersionEntity.VersionLabel, currentVersionEntity.ReleaseOrder, currentVersionEntity.ReleasedAt, currentVersionEntity.Status)
            : null;

        var prevVersions = new List<VersionReleaseSummaryDto>();
        var laterVersions = new List<VersionReleaseSummaryDto>();

        if (currentVersionEntity != null)
        {
            foreach (var v in sortedVersions)
            {
                var summary = new VersionReleaseSummaryDto(v.Id, v.VersionLabel, v.ReleaseOrder, v.ReleasedAt, v.Status);
                if (v.ReleaseOrder < currentVersionEntity.ReleaseOrder)
                    prevVersions.Add(summary);
                else if (v.ReleaseOrder > currentVersionEntity.ReleaseOrder)
                    laterVersions.Add(summary);
            }
        }
        else
        {
            laterVersions = sortedVersions.Select(v => new VersionReleaseSummaryDto(v.Id, v.VersionLabel, v.ReleaseOrder, v.ReleasedAt, v.Status)).ToList();
        }

        int minOrderExclusive = currentVersionEntity?.ReleaseOrder ?? 0;
        var laterFixesRaw = await _repository.GetFixChangesForProductAsync(productId, minOrderExclusive, ct);

        var laterFixDtos = new List<LaterVersionFixCopilotDto>();
        foreach (var fix in laterFixesRaw)
        {
            var fixVersion = sortedVersions.FirstOrDefault(v => v.Id == fix.ProductVersionId);
            var links = await _repository.GetLinkedCasesByChangeIdAsync(fix.Id, ct);
            var assignments = await _repository.GetAssignmentsByVersionIdAsync(fix.ProductVersionId, ct);
            int deployedCount = assignments.Count(a => string.Equals(a.Status, "Deployed", StringComparison.OrdinalIgnoreCase));

            var caseNumbers = new List<ulong>();
            foreach (var l in links)
            {
                var c = await _caseRepository.GetByIdAsync(l.CaseId, ct);
                if (c != null) caseNumbers.Add(c.CaseNumber);
            }

            string? compName = null;
            if (fix.ComponentId.HasValue)
            {
                var comp = await _catalogRepository.GetComponentByIdAsync(fix.ComponentId.Value, ct);
                compName = comp?.Name;
            }

            laterFixDtos.Add(new LaterVersionFixCopilotDto(
                ChangeId: fix.Id,
                ProductVersionId: fix.ProductVersionId,
                VersionLabel: fixVersion?.VersionLabel ?? "—",
                ReleaseOrder: fixVersion?.ReleaseOrder ?? 0,
                Title: fix.Title,
                Description: fix.Description,
                ErrorCode: fix.ErrorCode,
                ComponentName: compName,
                LinkedHistoricalCasesCount: links.Count,
                LinkedCaseNumbers: caseNumbers,
                DeployedClientsCount: deployedCount
            ));
        }

        var clientAssignments = await _repository.GetAssignmentsByClientIdAsync(clientId, ct);
        var plannedAssignments = clientAssignments
            .Where(a => string.Equals(a.Status, "Planned", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
            .Select(a => {
                var v = sortedVersions.FirstOrDefault(ver => ver.Id == a.ProductVersionId);
                return new ProductVersionAssignmentDto(
                    Id: a.Id,
                    ProductVersionId: a.ProductVersionId,
                    VersionLabel: v?.VersionLabel,
                    ClientId: a.ClientId,
                    ClientName: client.Name,
                    ClientUnitId: a.ClientUnitId,
                    ClientUnitName: null,
                    Status: a.Status,
                    StatusLabel: ProductVersionAssignment.Label(a.Status),
                    PlannedAt: a.PlannedAt,
                    ScheduledAt: a.ScheduledAt,
                    DeployedAt: a.DeployedAt,
                    Notes: a.Notes,
                    CreatedAt: a.CreatedAt,
                    CreatedBy: a.CreatedBy,
                    UpdatedAt: a.UpdatedAt,
                    UpdatedBy: a.UpdatedBy
                );
            }).ToList();

        string summaryStatus = currentVersionDto != null
            ? $"Cliente '{client.Name}' está na versão v{currentVersionDto.VersionLabel} do produto '{product.Name}'. Existem {laterVersions.Count} versão(ões) posterior(es) lançada(s) e {laterFixDtos.Count} correção(ões) cadastrada(s)."
            : $"Cliente '{client.Name}' não possui versão ativa definida para o produto '{product.Name}'. Existem {laterVersions.Count} versão(ões) lançada(s) no catálogo.";

        return new ClientVersionCopilotContextDto(
            ClientId: client.Id,
            ClientName: client.Name,
            ProductId: product.Id,
            ProductName: product.Name,
            CurrentVersion: currentVersionDto,
            PreviousVersions: prevVersions,
            LaterVersions: laterVersions,
            LaterVersionFixes: laterFixDtos,
            PlannedAssignments: plannedAssignments,
            StatusSummary: summaryStatus
        );
    }

    public async Task<IReadOnlyList<LaterVersionFixCopilotDto>> SearchVersionFixesForCopilotAsync(long productId, long? currentProductVersionId, string? query, string? errorCode, CancellationToken ct = default)
    {
        var allVersions = await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
        var sortedVersions = allVersions.OrderBy(v => v.ReleaseOrder).ToList();

        int minOrderExclusive = 0;
        if (currentProductVersionId.HasValue)
        {
            var cur = sortedVersions.FirstOrDefault(v => v.Id == currentProductVersionId.Value);
            if (cur != null)
                minOrderExclusive = cur.ReleaseOrder;
        }

        var fixes = await _repository.GetFixChangesForProductAsync(productId, minOrderExclusive, ct);

        // Filtro em memória por query ou errorCode se informados
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            fixes = fixes.Where(f => string.Equals(f.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            fixes = fixes.Where(f =>
                f.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (f.Description != null && f.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var result = new List<LaterVersionFixCopilotDto>();
        foreach (var fix in fixes)
        {
            var fixVersion = sortedVersions.FirstOrDefault(v => v.Id == fix.ProductVersionId);
            var links = await _repository.GetLinkedCasesByChangeIdAsync(fix.Id, ct);
            var assignments = await _repository.GetAssignmentsByVersionIdAsync(fix.ProductVersionId, ct);
            int deployedCount = assignments.Count(a => string.Equals(a.Status, "Deployed", StringComparison.OrdinalIgnoreCase));

            var caseNumbers = new List<ulong>();
            foreach (var l in links)
            {
                var c = await _caseRepository.GetByIdAsync(l.CaseId, ct);
                if (c != null) caseNumbers.Add(c.CaseNumber);
            }

            string? compName = null;
            if (fix.ComponentId.HasValue)
            {
                var comp = await _catalogRepository.GetComponentByIdAsync(fix.ComponentId.Value, ct);
                compName = comp?.Name;
            }

            result.Add(new LaterVersionFixCopilotDto(
                ChangeId: fix.Id,
                ProductVersionId: fix.ProductVersionId,
                VersionLabel: fixVersion?.VersionLabel ?? "—",
                ReleaseOrder: fixVersion?.ReleaseOrder ?? 0,
                Title: fix.Title,
                Description: fix.Description,
                ErrorCode: fix.ErrorCode,
                ComponentName: compName,
                LinkedHistoricalCasesCount: links.Count,
                LinkedCaseNumbers: caseNumbers,
                DeployedClientsCount: deployedCount
            ));
        }

        return result;
    }
}