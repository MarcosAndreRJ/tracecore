using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseResolutionRepository _caseResolutionRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditEventRepository _auditEventRepository;

    public KnowledgeService(
        IKnowledgeRepository knowledgeRepository,
        ICaseRepository caseRepository,
        ICaseResolutionRepository caseResolutionRepository,
        ICatalogRepository catalogRepository,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IAuditEventRepository auditEventRepository)
    {
        _knowledgeRepository = knowledgeRepository;
        _caseRepository = caseRepository;
        _caseResolutionRepository = caseResolutionRepository;
        _catalogRepository = catalogRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _auditEventRepository = auditEventRepository;
    }

    public async Task<long> CreateKnowledgeDraftAsync(CreateKnowledgeDraftCommand command, long userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new BusinessRuleValidationException("BR-040", "O título da solução/artigo é obrigatório.");

        // Gerar código único para o item
        var code = await GenerateUniqueKnowledgeCodeAsync(command.KnowledgeType, ct);

        var item = new KnowledgeItem(
            knowledgeCode: code,
            title: command.Title,
            summary: command.Summary,
            knowledgeType: command.KnowledgeType,
            provenanceType: command.ProvenanceType,
            createdBy: userId,
            provenanceCaseId: command.ProvenanceCaseId,
            provenanceReference: command.ProvenanceReference,
            ownerUserId: userId,
            ownerDepartmentId: command.OwnerDepartmentId
        );

        var itemId = await _knowledgeRepository.CreateItemAsync(item, ct);

        // Criar a Versão 1 inicial (em Draft)
        var version = new KnowledgeVersion(
            knowledgeItemId: itemId,
            versionNo: 1,
            contentMarkdown: command.ContentMarkdown,
            createdBy: userId,
            problemDescription: command.ProblemDescription,
            rootCauseSummary: command.RootCauseSummary,
            validationMethod: command.ValidationMethod,
            riskWarning: command.RiskWarning,
            rollbackPlan: command.RollbackPlan,
            changeSummary: "Criação inicial do rascunho de conhecimento"
        );

        var versionId = await _knowledgeRepository.CreateVersionAsync(version, ct);

        // Aplicabilidade
        if (command.Applicabilities != null && command.Applicabilities.Count > 0)
        {
            foreach (var app in command.Applicabilities)
            {
                await _knowledgeRepository.AddApplicabilityAsync(new KnowledgeApplicability(
                    knowledgeItemId: itemId,
                    productId: app.ProductId,
                    productVersionId: app.ProductVersionId,
                    componentId: app.ComponentId,
                    environmentId: app.EnvironmentId,
                    applicabilityType: app.ApplicabilityType,
                    notes: app.Notes
                ), ct);
            }
        }

        // Passos
        if (command.Steps != null && command.Steps.Count > 0)
        {
            var steps = command.Steps.Select(s => new KnowledgeStep(
                knowledgeVersionId: versionId,
                sequenceNo: s.SequenceNo,
                title: s.Title,
                description: s.Description,
                stepType: s.StepType,
                command: s.Command,
                expectedOutput: s.ExpectedOutput
            ));
            await _knowledgeRepository.AddStepsAsync(steps, ct);
        }

        // Sintomas
        if (command.Symptoms != null && command.Symptoms.Count > 0)
        {
            var symptoms = command.Symptoms
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new KnowledgeSymptom(versionId, s));
            await _knowledgeRepository.AddSymptomsAsync(symptoms, ct);
        }

        // Tecnologias
        if (command.Technologies != null && command.Technologies.Count > 0)
        {
            await _knowledgeRepository.SetTechnologiesAsync(itemId, command.Technologies, ct);
        }

        // Tags
        if (command.Tags != null && command.Tags.Count > 0)
        {
            await _knowledgeRepository.SetTagsAsync(itemId, command.Tags, ct);
        }

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeDraftCreated",
            entityType: "KnowledgeItem",
            entityId: itemId.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { Code = code, item.Title, item.ProvenanceType })
        ), ct);

        return itemId;
    }

    public async Task<long> CreateDraftFromCaseAsync(CreateDraftFromCaseCommand command, long userId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", command.CaseId);

        var resolution = await _caseResolutionRepository.GetByCaseIdAsync(command.CaseId, ct);

        // Prepara título objetivo a partir do resumo normalizado ou caso
        var title = !string.IsNullOrWhiteSpace(@case.NormalizedSummary)
            ? $"Solução: {@case.NormalizedSummary}"
            : (!string.IsNullOrWhiteSpace(@case.ObservedBehavior) ? $"Solução: {@case.ObservedBehavior}" : $"Solução para Caso #{@case.CaseNumber}");

        var summary = resolution?.ResolutionSummary ?? @case.NormalizedSummary ?? "Solução estruturada derivada de caso resolvido.";
        var problemDesc = @case.ObservedBehavior ?? @case.OriginalReport;
        
        string? rootCauseSummary = null;
        if (resolution?.RootCauseId.HasValue == true)
        {
            var rc = await _caseResolutionRepository.GetRootCauseByIdAsync(resolution.RootCauseId.Value, ct);
            rootCauseSummary = rc?.Name;
        }

        var validationMethod = resolution?.ValidationSummary;
        var preventiveActions = resolution?.PreventiveActions;
        var contentMarkdown = !string.IsNullOrWhiteSpace(preventiveActions)
            ? $"### Ações Preventivas Recomendadas\n\n{preventiveActions}\n\n### Resumo da Solução\n\n{resolution?.ResolutionSummary}"
            : (resolution?.ResolutionSummary ?? "Procedimento de solução.");

        // Identifica componente causador (RootCause) ou afetado
        var rootCauseComp = @case.AffectedComponents.FirstOrDefault(c => c.RelationType == "RootCause")
                            ?? @case.AffectedComponents.FirstOrDefault();

        var applicabilities = new List<CreateKnowledgeApplicabilityInput>();
        if (@case.ProductId.HasValue || rootCauseComp != null || @case.EnvironmentId.HasValue)
        {
            applicabilities.Add(new CreateKnowledgeApplicabilityInput(
                ProductId: @case.ProductId,
                ProductVersionId: @case.ProductVersionId,
                ComponentId: rootCauseComp?.ComponentId,
                EnvironmentId: @case.EnvironmentId,
                ApplicabilityType: "Applies",
                Notes: $"Importado automaticamente do Caso #{@case.CaseNumber}"
            ));
        }

        var draftCommand = new CreateKnowledgeDraftCommand(
            Title: title,
            Summary: summary,
            KnowledgeType: "Solution",
            ProvenanceType: "Case", // BR-046
            ProvenanceCaseId: @case.Id,
            ProvenanceReference: $"CAS-{@case.CaseNumber}",
            OwnerDepartmentId: resolution?.ResponsibleDepartmentId ?? @case.CurrentDepartmentId,
            ContentMarkdown: contentMarkdown,
            ProblemDescription: problemDesc,
            RootCauseSummary: rootCauseSummary,
            ValidationMethod: validationMethod,
            RiskWarning: resolution?.RecurrenceRisk == "High" ? "Risco elevado de recorrência caso as pré-condições não sejam atendidas." : null,
            RollbackPlan: resolution?.RecurrenceRisk == "High" ? "Reverter alterações operacionais e reiniciar serviço para o estado prévio." : null,
            Applicabilities: applicabilities,
            Tags: new List<string> { "caso-resolvido", "licao-aprendida" }
        );

        return await CreateKnowledgeDraftAsync(draftCommand, userId, ct);
    }

    public async Task SubmitForReviewAsync(SubmitForReviewCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        // BR-040: apenas Draft pode ir para Review
        item.SubmitForReview();
        await _knowledgeRepository.UpdateItemAsync(item, ct);

        // Atualiza versão atual para InReview
        var version = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
        if (version != null && version.Status == "Draft")
        {
            version.Status = "InReview";
            await _knowledgeRepository.UpdateVersionAsync(version, ct);
        }

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeSubmittedForReview",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { item.KnowledgeCode, item.Status })
        ), ct);
    }

    public async Task ApproveAndPublishAsync(ApproveAndPublishCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        // BR-043: Bloquear publicação se aplicabilidade estiver vazia
        var apps = await _knowledgeRepository.GetApplicabilitiesByItemIdAsync(item.Id, ct);
        if (apps.Count == 0)
        {
            throw new BusinessRuleValidationException(
                "BR-043",
                "A publicação de uma solução exige a declaração prévia de aplicabilidade (ao menos um produto, componente ou ambiente aplicável).");
        }

        // Obtém a versão que será publicada
        var version = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
        if (version == null)
            throw new InvalidOperationException($"Versão {item.CurrentVersionNo} do item {item.KnowledgeCode} não foi encontrada.");

        // BR-044: Bloquear publicação se método de validação estiver vazio
        if (string.IsNullOrWhiteSpace(version.ValidationMethod))
        {
            throw new BusinessRuleValidationException(
                "BR-044",
                "A publicação de uma solução exige obrigatoriamente a especificação do método de validação do resultado.");
        }

        // BR-045: Se houver aviso de risco relevante, rollback_plan é obrigatório
        if (!string.IsNullOrWhiteSpace(version.RiskWarning) && string.IsNullOrWhiteSpace(version.RollbackPlan))
        {
            throw new BusinessRuleValidationException(
                "BR-045",
                "Ao declarar aviso de risco relevante no procedimento, o plano de rollback correspondente torna-se obrigatório para publicação.");
        }

        var now = DateTime.UtcNow;

        // BR-041: version recebe aprovador e data
        version.Approve(approvedBy: userId, approvedAt: now);
        await _knowledgeRepository.UpdateVersionAsync(version, ct);

        // BR-040: item transita para Published
        item.Publish(version.VersionNo, approvedBy: userId, approvedAt: now, nextReviewDue: command.NextReviewDue);
        await _knowledgeRepository.UpdateItemAsync(item, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgePublished",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { item.KnowledgeCode, VersionNo = version.VersionNo, ApprovedBy = userId, ApprovedAt = now })
        ), ct);
    }

    public async Task<int> CreateNewVersionAsync(CreateNewVersionCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        // Bloco 7.A.5: Se a versão anterior estiver com status 'Approved', marca como 'Superseded'
        var previousVersion = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
        if (previousVersion != null && previousVersion.Status == "Approved")
        {
            previousVersion.Supersede();
            await _knowledgeRepository.UpdateVersionAsync(previousVersion, ct);
        }

        // BR-042: Modificação cria nova versão lógica; anterior permanece intacta
        var newVersionNo = item.CurrentVersionNo + 1;

        var newVersion = new KnowledgeVersion(
            knowledgeItemId: item.Id,
            versionNo: newVersionNo,
            contentMarkdown: command.ContentMarkdown,
            createdBy: userId,
            problemDescription: command.ProblemDescription,
            rootCauseSummary: command.RootCauseSummary,
            validationMethod: command.ValidationMethod,
            riskWarning: command.RiskWarning,
            rollbackPlan: command.RollbackPlan,
            changeSummary: command.ChangeSummary ?? $"Revisão {newVersionNo}"
        );

        var versionId = await _knowledgeRepository.CreateVersionAsync(newVersion, ct);

        if (command.Steps != null && command.Steps.Count > 0)
        {
            var steps = command.Steps.Select(s => new KnowledgeStep(
                knowledgeVersionId: versionId,
                sequenceNo: s.SequenceNo,
                title: s.Title,
                description: s.Description,
                stepType: s.StepType,
                command: s.Command,
                expectedOutput: s.ExpectedOutput
            ));
            await _knowledgeRepository.AddStepsAsync(steps, ct);
        }

        if (command.Symptoms != null && command.Symptoms.Count > 0)
        {
            var symptoms = command.Symptoms.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new KnowledgeSymptom(versionId, s));
            await _knowledgeRepository.AddSymptomsAsync(symptoms, ct);
        }

        item.IncrementVersion(newVersionNo);
        // Ao criar nova revisão de conteúdo publicado, o item volta a requerer revisão/aprovação
        item.Status = "Review";
        await _knowledgeRepository.UpdateItemAsync(item, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeVersionCreated",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { item.KnowledgeCode, NewVersionNo = newVersionNo, command.ChangeSummary })
        ), ct);

        return newVersionNo;
    }

    public async Task DeprecateKnowledgeAsync(DeprecateKnowledgeCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        // BR-049: descontinuar mantém histórico íntegro, nunca exclusão
        item.Deprecate(DateTime.UtcNow, command.ReplacementKnowledgeId);
        await _knowledgeRepository.UpdateItemAsync(item, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeDeprecated",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { item.KnowledgeCode, command.ReplacementKnowledgeId })
        ), ct);
    }

    public async Task ArchiveKnowledgeAsync(ArchiveKnowledgeCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        item.Archive(DateTime.UtcNow);
        await _knowledgeRepository.UpdateItemAsync(item, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeArchived",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { item.KnowledgeCode, command.Reason })
        ), ct);
    }

    public async Task<long> RecordUsageAsync(RecordKnowledgeUsageCommand command, long userId, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(command.KnowledgeItemId, ct);
        if (item == null)
            throw new EntityNotFoundException("Item de Conhecimento", command.KnowledgeItemId);

        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", command.CaseId);

        var currentVersion = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
        var versionId = currentVersion?.Id ?? 1;

        // BR-047: outcome deve ser classificado
        var usage = new KnowledgeUsage(
            knowledgeItemId: item.Id,
            knowledgeVersionId: versionId,
            caseId: @case.Id,
            usedBy: userId,
            outcome: command.Outcome,
            notes: command.Notes,
            contextMatchJson: command.ContextMatchJson
        );

        var usageId = await _knowledgeRepository.RecordUsageAsync(usage, ct);

        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "KnowledgeUsageRecorded",
            entityType: "KnowledgeItem",
            entityId: item.Id.ToString(),
            actorUserId: userId,
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { CaseId = @case.Id, command.Outcome, usageId })
        ), ct);

        return usageId;
    }

    public async Task<IReadOnlyList<KnowledgeItemSummaryDto>> SearchKnowledgeAsync(
        string? search = null,
        long? productId = null,
        string? status = null,
        string? provenance = null,
        string? category = null,
        CancellationToken ct = default)
    {
        var items = await _knowledgeRepository.SearchAsync(search, productId, status, provenance, category, ct);
        var dtos = new List<KnowledgeItemSummaryDto>();

        foreach (var item in items)
        {
            var apps = await _knowledgeRepository.GetApplicabilitiesByItemIdAsync(item.Id, ct);
            var usages = await _knowledgeRepository.GetUsagesByItemIdAsync(item.Id, ct);
            var version = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
            var tags = await _knowledgeRepository.GetTagsByItemIdAsync(item.Id, ct);

            // BR-047, BR-048: taxa de sucesso e amostra calculadas
            int totalUsages = usages.Count;
            int successful = usages.Count(u => u.Outcome == "Worked" || u.Outcome == "PartiallyWorked");
            string successRate = totalUsages > 0
                ? $"{(successful * 100.0 / totalUsages):F1}%"
                : "N/A";
            string successSample = totalUsages > 0
                ? $"{successful} de {totalUsages} casos"
                : "Sem utilizações registradas";

            // Metadados de produto e componente
            string productCode = "";
            string productName = "";
            string componentName = "";
            var firstApp = apps.FirstOrDefault();
            if (firstApp?.ProductId.HasValue == true)
            {
                var prod = await _catalogRepository.GetProductByIdAsync(firstApp.ProductId.Value, ct);
                productCode = prod?.Code ?? "";
                productName = prod?.Name ?? "";
            }
            if (firstApp?.ComponentId.HasValue == true)
            {
                var comp = await _catalogRepository.GetComponentByIdAsync(firstApp.ComponentId.Value, ct);
                componentName = comp?.Name ?? "";
            }

            // Nomes de autor e revisor
            string authorName = "Equipe Técnica";
            if (item.CreatedBy > 0)
            {
                var u = await _userRepository.GetByIdAsync(item.CreatedBy, ct);
                if (u != null) authorName = u.Name;
            }

            string reviewerName = "Pendente de revisão";
            if (version?.ApprovedBy.HasValue == true)
            {
                var rev = await _userRepository.GetByIdAsync(version.ApprovedBy.Value, ct);
                if (rev != null) reviewerName = rev.Name;
            }

            dtos.Add(new KnowledgeItemSummaryDto(
                Id: item.Id,
                Code: item.KnowledgeCode,
                Title: item.Title,
                Summary: item.Summary,
                ProductCode: productCode,
                ProductName: productName,
                Component: componentName,
                ApplicableVersions: firstApp?.Notes ?? "Todas as versões suportadas",
                Status: item.Status,
                ProvenanceType: item.ProvenanceType.ToLowerInvariant(),
                ProvenanceRef: item.ProvenanceReference ?? (item.ProvenanceCaseId.HasValue ? $"CAS-{item.ProvenanceCaseId}" : null),
                Author: authorName,
                Reviewer: reviewerName,
                SuccessRate: successRate,
                SuccessSample: successSample,
                TotalUsages: totalUsages,
                SuccessfulUsages: successful,
                RiskLevel: !string.IsNullOrWhiteSpace(version?.RiskWarning) ? "Médio" : "Baixo",
                HasRollbackPlan: !string.IsNullOrWhiteSpace(version?.RollbackPlan),
                UpdatedAt: item.UpdatedAt,
                Category: item.KnowledgeType.ToLowerInvariant(),
                Tags: tags.ToList()
            ));
        }

        return dtos;
    }

    public async Task<KnowledgeDetailDto?> GetKnowledgeDetailAsync(long id, CancellationToken ct = default)
    {
        var item = await _knowledgeRepository.GetByIdAsync(id, ct);
        if (item == null) return null;

        var version = await _knowledgeRepository.GetVersionByItemAndNumberAsync(item.Id, item.CurrentVersionNo, ct);
        var allVersions = await _knowledgeRepository.GetVersionsByItemIdAsync(item.Id, ct);
        var apps = await _knowledgeRepository.GetApplicabilitiesByItemIdAsync(item.Id, ct);
        var usages = await _knowledgeRepository.GetUsagesByItemIdAsync(item.Id, ct);
        var steps = version != null ? await _knowledgeRepository.GetStepsByVersionIdAsync(version.Id, ct) : [];
        var techs = await _knowledgeRepository.GetTechnologiesByItemIdAsync(item.Id, ct);
        var tags = await _knowledgeRepository.GetTagsByItemIdAsync(item.Id, ct);

        // Autor
        string authorName = "Equipe Técnica";
        if (item.CreatedBy > 0)
        {
            var u = await _userRepository.GetByIdAsync(item.CreatedBy, ct);
            if (u != null) authorName = u.Name;
        }

        // Revisor
        string reviewerName = "Aguardando revisão formal";
        if (version?.ApprovedBy.HasValue == true)
        {
            var rev = await _userRepository.GetByIdAsync(version.ApprovedBy.Value, ct);
            if (rev != null) reviewerName = rev.Name;
        }

        // Rastreabilidade com caso de origem
        string sourceCaseTitle = "";
        string sourceRef = item.ProvenanceReference ?? "";
        if (item.ProvenanceCaseId.HasValue)
        {
            var sc = await _caseRepository.GetByIdAsync(item.ProvenanceCaseId.Value, ct);
            if (sc != null)
            {
                sourceCaseTitle = sc.NormalizedSummary ?? sc.ObservedBehavior ?? $"Caso #{sc.CaseNumber}";
                sourceRef = $"CAS-{sc.CaseNumber}";
            }
        }

        // Matriz de aplicabilidade
        string productCode = "";
        string productName = "";
        string componentName = "";
        var appEnvironments = new List<string>();
        var prerequisites = new List<string>();

        var firstApp = apps.FirstOrDefault();
        if (firstApp?.ProductId.HasValue == true)
        {
            var prod = await _catalogRepository.GetProductByIdAsync(firstApp.ProductId.Value, ct);
            productCode = prod?.Code ?? "";
            productName = prod?.Name ?? "";
        }
        if (firstApp?.ComponentId.HasValue == true)
        {
            var comp = await _catalogRepository.GetComponentByIdAsync(firstApp.ComponentId.Value, ct);
            componentName = comp?.Name ?? "";
        }

        foreach (var app in apps)
        {
            if (app.EnvironmentId.HasValue)
            {
                var env = await _catalogRepository.GetEnvironmentByIdAsync(app.EnvironmentId.Value, ct);
                if (env != null) appEnvironments.Add(env.Name);
            }
            if (!string.IsNullOrWhiteSpace(app.Notes))
            {
                prerequisites.Add(app.Notes);
            }
        }
        if (appEnvironments.Count == 0) appEnvironments.Add("Todos os ambientes de produção e homologação");
        if (prerequisites.Count == 0) prerequisites.Add("Acesso operacional e credencial corporativa autorizada.");

        // Estatística de eficácia
        int totalUsages = usages.Count;
        int successful = usages.Count(u => u.Outcome == "Worked" || u.Outcome == "PartiallyWorked");
        string successRate = totalUsages > 0 ? $"{(successful * 100.0 / totalUsages):F1}%" : "N/A";
        string successSample = totalUsages > 0 ? $"{successful} de {totalUsages} casos" : "Sem utilizações registradas";

        // Casos associados
        var associatedCases = new List<AssociatedCaseRefDto>();
        foreach (var u in usages.Take(10))
        {
            var c = await _caseRepository.GetByIdAsync(u.CaseId, ct);
            associatedCases.Add(new AssociatedCaseRefDto(
                CaseId: u.CaseId,
                Code: c != null ? $"CAS-{c.CaseNumber}" : $"#{u.CaseId}",
                Title: c?.NormalizedSummary ?? c?.ObservedBehavior ?? $"Caso #{u.CaseId}",
                ResolutionDate: u.UsedAt,
                Outcome: u.Outcome == "Worked" ? "Resolvido com Sucesso" : (u.Outcome == "PartiallyWorked" ? "Sucesso Parcial" : "Sem Êxito")
            ));
        }

        // Histórico de versões
        var revisionDtos = new List<RevisionHistoryDto>();
        foreach (var v in allVersions)
        {
            string changerName = "Sistema";
            if (v.CreatedBy > 0)
            {
                var cu = await _userRepository.GetByIdAsync(v.CreatedBy, ct);
                if (cu != null) changerName = cu.Name;
            }

            revisionDtos.Add(new RevisionHistoryDto(
                Version: $"v{v.VersionNo}.0",
                ChangedBy: changerName,
                ChangedAt: v.CreatedAt,
                Reason: v.ChangeSummary ?? "Atualização de procedimento e diretrizes técnicas"
            ));
        }

        // Passos procedimentais
        var stepDtos = steps.Select(s => new KnowledgeStepDto(
            StepNumber: s.SequenceNo,
            Title: s.Title,
            Description: s.Description,
            Command: s.Command,
            ExpectedOutput: s.ExpectedOutput,
            StepType: s.StepType
        )).ToList();

        return new KnowledgeDetailDto(
            Id: item.Id,
            Code: item.KnowledgeCode,
            Title: item.Title,
            Version: $"v{item.CurrentVersionNo}.0",
            LifecycleStatus: item.Status,
            IsAiGenerated: string.Equals(item.ProvenanceType, "Ai", StringComparison.OrdinalIgnoreCase),
            Author: authorName,
            Reviewer: reviewerName,
            PublishedAt: item.PublishedAt,
            LastReviewedAt: item.LastReviewedAt,
            NextReviewDue: item.ReviewDueAt,
            ProvenanceType: item.ProvenanceType.ToLowerInvariant(),
            SourceCaseId: item.ProvenanceCaseId,
            SourceCaseTitle: sourceCaseTitle,
            SourceReference: sourceRef,
            ProductCode: productCode,
            ProductName: productName,
            Component: componentName,
            ApplicableVersions: firstApp?.Notes ?? "Todas as versões homologadas",
            ApplicableEnvironments: appEnvironments,
            Prerequisites: prerequisites,
            ProblemDescription: version?.ProblemDescription ?? item.Summary,
            RootCauseSummary: version?.RootCauseSummary,
            RiskLevel: !string.IsNullOrWhiteSpace(version?.RiskWarning) ? "Médio" : "Baixo",
            RiskWarning: version?.RiskWarning ?? "Procedimento de baixo risco operacional.",
            RollbackPlan: version?.RollbackPlan ?? "Desfazer as alterações e restaurar configuração padrão.",
            Steps: stepDtos,
            ValidationMethod: version?.ValidationMethod ?? "Não informado",
            ValidationCommand: null,
            ExpectedValidationOutput: null,
            TotalUsages: totalUsages,
            SuccessfulUsages: successful,
            SuccessRate: successRate,
            SuccessSample: successSample,
            AssociatedCases: associatedCases,
            Revisions: revisionDtos,
            Technologies: techs.ToList(),
            Tags: tags.ToList()
        );
    }

    public async Task<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount, string OverallSuccessRate)> GetKnowledgeDashboardMetricsAsync(CancellationToken ct = default)
    {
        var (total, published, review, stale) = await _knowledgeRepository.GetDashboardCountsAsync(ct);

        // Calcula taxa global de sucesso sobre todos os usos registrados
        // Para in-memory e MySQL podemos agregar sobre search ou repositório
        var allItems = await _knowledgeRepository.SearchAsync(ct: ct);
        int totalUsagesAll = 0;
        int successfulAll = 0;

        foreach (var item in allItems)
        {
            var usages = await _knowledgeRepository.GetUsagesByItemIdAsync(item.Id, ct);
            totalUsagesAll += usages.Count;
            successfulAll += usages.Count(u => u.Outcome == "Worked" || u.Outcome == "PartiallyWorked");
        }

        string overallRate = totalUsagesAll > 0
            ? $"{(successfulAll * 100.0 / totalUsagesAll):F1}% ({successfulAll} de {totalUsagesAll} casos)"
            : "0% (0 casos)";

        return (total, published, review, stale, overallRate);
    }

    private async Task<string> GenerateUniqueKnowledgeCodeAsync(string knowledgeType, CancellationToken ct)
    {
        var prefix = knowledgeType?.ToLowerInvariant() switch
        {
            "lessonlearned" => "LIC",
            "operationalprocedure" => "POP",
            "technicalarticle" => "ART",
            "runbook" => "RUN",
            _ => "SOL"
        };

        var all = await _knowledgeRepository.SearchAsync(ct: ct);
        var nextNum = all.Count + 1;
        var code = $"{prefix}-{nextNum:D3}";

        // Assegura unicidade
        while (await _knowledgeRepository.GetByCodeAsync(code, ct) != null)
        {
            nextNum++;
            code = $"{prefix}-{nextNum:D3}";
        }

        return code;
    }
}
