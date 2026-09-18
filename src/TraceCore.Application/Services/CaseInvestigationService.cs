using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

// TODO: M07 / 05_MOTOR_DE_DIAGNOSTICO.md (BR-070 a BR-076) — Motor de diagnóstico adaptativo e grafo de verificações
// consumirão estas hipóteses e passos diagnósticos em fases futuras, sem acoplamento direto neste momento.
// TODO: BR-027 a BR-029 — Causa raiz formal e case_resolutions serão implementadas na Fase 5.
// TODO: BR-030 — Relacionamento entre casos (duplicado, semelhante, recorrência).
// TODO: 10_MODELO_DE_DADOS.md — Vínculo estruturado case_hypothesis_evidence para reutilização de evidências entre hipóteses.
// TODO: BR-024 — Fluxo formal para reabertura de hipótese descartada por engano sem apagar rastros.
// TODO: Mudança automática de cases.status a partir de hipóteses/tentativas — status do caso continua desacoplado desta fase.

public class CaseInvestigationService : ICaseInvestigationService
{
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditEventRepository _auditEventRepository;

    public CaseInvestigationService(
        IDiagnosticRepository diagnosticRepository,
        ICaseRepository caseRepository,
        ICatalogRepository catalogRepository,
        IUserRepository userRepository,
        IAuditEventRepository auditEventRepository)
    {
        _diagnosticRepository = diagnosticRepository;
        _caseRepository = caseRepository;
        _catalogRepository = catalogRepository;
        _userRepository = userRepository;
        _auditEventRepository = auditEventRepository;
    }

    public async Task<CaseHypothesisDto> RegisterHypothesisAsync(
        RegisterHypothesisCommand command,
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
        {
            throw new EntityNotFoundException("Caso", command.CaseId);
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new BusinessRuleValidationException("BR-023", "O título da hipótese é obrigatório.");
        }

        if (command.ComponentId.HasValue)
        {
            var comp = await _catalogRepository.GetComponentByIdAsync(command.ComponentId.Value, ct);
            if (comp == null)
            {
                throw new EntityNotFoundException("Componente", command.ComponentId.Value);
            }
        }

        // Garante que uma sessão de diagnóstico esteja aberta implicitamente para o caso
        await EnsureOpenSessionAsync(command.CaseId, currentUserId ?? 1, ct);

        // BR-023: Um caso pode ter múltiplas hipóteses simultâneas. Não bloquear criação enquanto outra estiver aberta.
        var hypothesis = new CaseHypothesis(
            caseId: command.CaseId,
            title: command.Title,
            description: command.Description,
            componentId: command.ComponentId,
            createdBy: currentUserId,
            createdAt: DateTime.UtcNow
        );

        if (!string.IsNullOrWhiteSpace(command.SourceType))
        {
            hypothesis.SourceType = command.SourceType.Trim();
        }

        var id = await _diagnosticRepository.AddHypothesisAsync(hypothesis, ct);
        hypothesis.Id = id;

        // BR-100: Auditoria
        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "HypothesisRegistered",
            entityType: "CaseHypothesis",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            afterJson: $"{hypothesis.Title} [{hypothesis.Status}]"
        ), ct);

        return await MapToHypothesisDtoAsync(hypothesis, ct);
    }

    public async Task<DiagnosticStepDto> RegisterDiagnosticStepAsync(
        RegisterDiagnosticStepCommand command,
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
        {
            throw new EntityNotFoundException("Caso", command.CaseId);
        }

        // Validação dos campos obrigatórios conforme BR-025 na fronteira da aplicação
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new BusinessRuleValidationException("BR-025", "A ação/título da tentativa de diagnóstico é obrigatória.");
        }

        if (!currentUserId.HasValue || currentUserId.Value <= 0)
        {
            throw new BusinessRuleValidationException("BR-025", "O autor da tentativa de diagnóstico é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Objective))
        {
            throw new BusinessRuleValidationException("BR-025", "O objetivo da tentativa de diagnóstico é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.InputEvidenceSummary))
        {
            throw new BusinessRuleValidationException("BR-025", "A evidência anterior/de entrada é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(command.ResultSummary))
        {
            throw new BusinessRuleValidationException("BR-025", "O resultado observado da tentativa é obrigatório.");
        }

        // Validação do vocabulário fixo de outcome conforme BR-026
        if (!Enum.TryParse<DiagnosticStepOutcome>(command.Outcome, ignoreCase: true, out var outcomeEnum))
        {
            throw new BusinessRuleValidationException("BR-026",
                $"Classificação do resultado '{command.Outcome}' inválida. Valores permitidos: Worked, PartiallyWorked, DidNotWork, NotApplicable, Inconclusive.");
        }

        CaseHypothesis? hypothesis = null;
        if (command.HypothesisId.HasValue)
        {
            hypothesis = await _diagnosticRepository.GetHypothesisByIdAsync(command.HypothesisId.Value, ct);
            if (hypothesis == null || hypothesis.CaseId != command.CaseId)
            {
                throw new BusinessRuleValidationException("BR-023", $"Hipótese com ID {command.HypothesisId.Value} não pertence ao caso {command.CaseId}.");
            }
        }

        // Garante sessão aberta
        var session = await EnsureOpenSessionAsync(command.CaseId, currentUserId.Value, ct);

        // Próximo sequence_no dentro da sessão
        var sequenceNo = await _diagnosticRepository.GetNextStepSequenceNoAsync(session.Id, ct);

        var step = new DiagnosticStep(
            diagnosticSessionId: session.Id,
            sequenceNo: sequenceNo,
            stepType: command.StepType ?? DiagnosticStepTypes.Verification,
            title: command.Title,
            objective: command.Objective,
            inputEvidenceSummary: command.InputEvidenceSummary,
            resultSummary: command.ResultSummary,
            outcome: outcomeEnum,
            performedBy: currentUserId.Value,
            hypothesisId: command.HypothesisId,
            instruction: command.Instruction,
            riskLevel: command.RiskLevel ?? "Low",
            durationSeconds: command.DurationSeconds,
            performedAt: DateTime.UtcNow
        );

        var stepId = await _diagnosticRepository.AddStepAsync(step, ct);
        step.Id = stepId;

        // BR-100: Auditoria
        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "DiagnosticStepRegistered",
            entityType: "DiagnosticStep",
            entityId: stepId.ToString(),
            actorUserId: currentUserId,
            afterJson: $"{step.Title} -> {step.Outcome} (Session {session.Id}, Seq {sequenceNo})"
        ), ct);

        string? hypothesisTitle = hypothesis?.Title;
        string? performedByName = null;
        var user = await _userRepository.GetByIdAsync(currentUserId.Value, ct);
        if (user != null) performedByName = user.Name;

        EvidenceSuggestionDto? suggestedEvidence = null;
        if (step.HypothesisId.HasValue)
        {
            var suggestedRel = step.Outcome switch
            {
                nameof(DiagnosticStepOutcome.Worked) => nameof(EvidenceRelationType.Supports),
                nameof(DiagnosticStepOutcome.DidNotWork) => nameof(EvidenceRelationType.Contradicts),
                _ => nameof(EvidenceRelationType.Inconclusive)
            };

            suggestedEvidence = new EvidenceSuggestionDto(
                CaseId: command.CaseId,
                HypothesisId: step.HypothesisId.Value,
                HypothesisTitle: hypothesisTitle,
                DiagnosticStepId: step.Id,
                SuggestedEvidenceType: "DiagnosticTest",
                SuggestedDescription: $"Resultado do teste '{step.Title}': {step.ResultSummary}",
                SuggestedRelationType: suggestedRel,
                SuggestedJustification: $"Evidência empírica obtida no passo de teste seq #{step.SequenceNo} ({step.Outcome}) avaliando a hipótese '{hypothesisTitle}'."
            );
        }

        return new DiagnosticStepDto(
            Id: step.Id,
            DiagnosticSessionId: step.DiagnosticSessionId,
            SequenceNo: step.SequenceNo,
            StepType: step.StepType,
            HypothesisId: step.HypothesisId,
            HypothesisTitle: hypothesisTitle,
            Title: step.Title,
            Objective: step.Objective,
            Instruction: step.Instruction,
            InputEvidenceSummary: step.InputEvidenceSummary,
            ResultSummary: step.ResultSummary,
            Outcome: step.Outcome,
            RiskLevel: step.RiskLevel,
            DurationSeconds: step.DurationSeconds,
            PerformedBy: step.PerformedBy,
            PerformedByName: performedByName,
            PerformedAt: step.PerformedAt,
            MetadataJson: step.MetadataJson,
            SuggestedEvidence: suggestedEvidence
        );
    }

    public async Task<CaseHypothesisDto> EvaluateHypothesisAsync(
        EvaluateHypothesisCommand command,
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var hypothesis = await _diagnosticRepository.GetHypothesisByIdAsync(command.HypothesisId, ct);
        if (hypothesis == null)
        {
            throw new EntityNotFoundException("Hipótese", command.HypothesisId);
        }

        if (string.IsNullOrWhiteSpace(command.Justification))
        {
            throw new BusinessRuleValidationException("BR-024", "A justificativa técnica da avaliação da hipótese é obrigatória.");
        }

        if (!Enum.TryParse<HypothesisStatus>(command.NewStatus, ignoreCase: true, out var targetStatus))
        {
            throw new BusinessRuleValidationException("BR-024", $"Status '{command.NewStatus}' inválido para hipótese. Valores válidos: Discarded, Supported.");
        }

        if (targetStatus == HypothesisStatus.Proposed)
        {
            // BR-024: Não permitir voltar de Discarded/Supported para Proposed apagando a decisão
            throw new BusinessRuleValidationException("BR-024", "Não é permitido regredir uma hipótese avaliada de volta para 'Proposed'. Para manter rastro histórico, registre nova avaliação ou novo teste.");
        }

        if (command.EvidenceStepId.HasValue)
        {
            var step = await _diagnosticRepository.GetStepByIdAsync(command.EvidenceStepId.Value, ct);
            if (step == null)
            {
                throw new EntityNotFoundException("Passo Diagnóstico", command.EvidenceStepId.Value);
            }
        }

        var oldStatus = hypothesis.Status;
        hypothesis.Evaluate(targetStatus, command.Justification, DateTime.UtcNow);

        await _diagnosticRepository.UpdateHypothesisAsync(hypothesis, ct);

        // BR-100: Auditoria
        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "HypothesisEvaluated",
            entityType: "CaseHypothesis",
            entityId: hypothesis.Id.ToString(),
            actorUserId: currentUserId,
            beforeJson: oldStatus,
            afterJson: $"{hypothesis.Status} - Justificativa: {hypothesis.Justification}"
        ), ct);

        return await MapToHypothesisDtoAsync(hypothesis, ct);
    }

    public async Task<CaseInvestigationTimelineDto> GetInvestigationTimelineAsync(long caseId, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
        {
            throw new EntityNotFoundException("Caso", caseId);
        }

        var session = await _diagnosticRepository.GetOpenSessionByCaseIdAsync(caseId, ct);
        var hypotheses = await _diagnosticRepository.GetHypothesesByCaseIdAsync(caseId, ct);
        var steps = await _diagnosticRepository.GetStepsByCaseIdAsync(caseId, ct);

        // Cache de usuários e componentes para enriquecer os DTOs
        var userNames = new Dictionary<long, string>();
        var componentNames = new Dictionary<long, string>();

        async Task<string?> GetUserNameAsync(long? userId)
        {
            if (!userId.HasValue) return null;
            if (userNames.TryGetValue(userId.Value, out var name)) return name;
            var u = await _userRepository.GetByIdAsync(userId.Value, ct);
            if (u != null)
            {
                userNames[userId.Value] = u.Name;
                return u.Name;
            }
            return null;
        }

        async Task<string?> GetComponentNameAsync(long? compId)
        {
            if (!compId.HasValue) return null;
            if (componentNames.TryGetValue(compId.Value, out var name)) return name;
            var c = await _catalogRepository.GetComponentByIdAsync(compId.Value, ct);
            if (c != null)
            {
                componentNames[compId.Value] = c.Name;
                return c.Name;
            }
            return null;
        }

        var stepDtos = new List<DiagnosticStepDto>();
        var hypMap = hypotheses.ToDictionary(h => h.Id);

        foreach (var s in steps)
        {
            string? hypTitle = null;
            if (s.HypothesisId.HasValue && hypMap.TryGetValue(s.HypothesisId.Value, out var h))
            {
                hypTitle = h.Title;
            }

            var performedByName = await GetUserNameAsync(s.PerformedBy);
            stepDtos.Add(new DiagnosticStepDto(
                Id: s.Id,
                DiagnosticSessionId: s.DiagnosticSessionId,
                SequenceNo: s.SequenceNo,
                StepType: s.StepType,
                HypothesisId: s.HypothesisId,
                HypothesisTitle: hypTitle,
                Title: s.Title,
                Objective: s.Objective,
                Instruction: s.Instruction,
                InputEvidenceSummary: s.InputEvidenceSummary,
                ResultSummary: s.ResultSummary,
                Outcome: s.Outcome,
                RiskLevel: s.RiskLevel,
                DurationSeconds: s.DurationSeconds,
                PerformedBy: s.PerformedBy,
                PerformedByName: performedByName,
                PerformedAt: s.PerformedAt,
                MetadataJson: s.MetadataJson
            ));
        }

        var hypDtos = new List<CaseHypothesisDto>();
        foreach (var h in hypotheses)
        {
            var compName = await GetComponentNameAsync(h.ComponentId);
            var createdByName = await GetUserNameAsync(h.CreatedBy);
            var relatedSteps = stepDtos.Where(s => s.HypothesisId == h.Id).ToList();

            hypDtos.Add(new CaseHypothesisDto(
                Id: h.Id,
                CaseId: h.CaseId,
                ComponentId: h.ComponentId,
                ComponentName: compName,
                Title: h.Title,
                Description: h.Description,
                Status: h.Status,
                SourceType: h.SourceType,
                Justification: h.Justification,
                CreatedAt: h.CreatedAt,
                CreatedBy: h.CreatedBy,
                CreatedByName: createdByName,
                UpdatedAt: h.UpdatedAt,
                Steps: relatedSteps
            ));
        }

        DiagnosticSessionDto? sessionDto = null;
        if (session != null)
        {
            var startedByName = await GetUserNameAsync(session.StartedBy);
            sessionDto = new DiagnosticSessionDto(
                Id: session.Id,
                CaseId: session.CaseId,
                Status: session.Status,
                StartedAt: session.StartedAt,
                StartedBy: session.StartedBy,
                StartedByName: startedByName,
                EndedAt: session.EndedAt
            );
        }

        // Timeline unificada agregando hipóteses e passos em ordem cronológica
        var timeline = new List<InvestigationTimelineItemDto>();

        foreach (var h in hypDtos)
        {
            timeline.Add(new InvestigationTimelineItemDto(
                Id: h.Id,
                ItemType: "Hypothesis",
                TimestampUtc: h.CreatedAt,
                Title: $"Hipótese: {h.Title}",
                Subtitle: h.ComponentName != null ? $"Componente: {h.ComponentName}" : null,
                StatusOrOutcome: h.Status,
                HypothesisId: h.Id,
                HypothesisTitle: h.Title,
                PerformedByName: h.CreatedByName,
                Details: h.Justification ?? h.Description
            ));
        }

        foreach (var s in stepDtos)
        {
            timeline.Add(new InvestigationTimelineItemDto(
                Id: s.Id,
                ItemType: "Step",
                TimestampUtc: s.PerformedAt,
                Title: $"Teste #{s.SequenceNo}: {s.Title}",
                Subtitle: s.HypothesisTitle != null ? $"Ligado à: {s.HypothesisTitle}" : "Geral",
                StatusOrOutcome: s.Outcome,
                HypothesisId: s.HypothesisId,
                HypothesisTitle: s.HypothesisTitle,
                PerformedByName: s.PerformedByName,
                Details: $"Objetivo: {s.Objective} | Resultado: {s.ResultSummary}"
            ));
        }

        timeline = timeline.OrderBy(t => t.TimestampUtc).ThenBy(t => t.Id).ToList();

        return new CaseInvestigationTimelineDto(
            CaseId: @case.Id,
            CaseNumber: @case.CaseNumber,
            ActiveSession: sessionDto,
            Hypotheses: hypDtos,
            Steps: stepDtos,
            Timeline: timeline
        );
    }

    public async Task<IReadOnlyList<CaseHypothesisDto>> GetHypothesesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var hypotheses = await _diagnosticRepository.GetHypothesesByCaseIdAsync(caseId, ct);
        var list = new List<CaseHypothesisDto>();
        foreach (var h in hypotheses)
        {
            list.Add(await MapToHypothesisDtoAsync(h, ct));
        }
        return list;
    }

    private async Task<DiagnosticSession> EnsureOpenSessionAsync(long caseId, long currentUserId, CancellationToken ct)
    {
        var session = await _diagnosticRepository.GetOpenSessionByCaseIdAsync(caseId, ct);
        if (session == null)
        {
            session = new DiagnosticSession(caseId, currentUserId);
            var id = await _diagnosticRepository.CreateSessionAsync(session, ct);
            session.Id = id;
        }
        return session;
    }

    private async Task<CaseHypothesisDto> MapToHypothesisDtoAsync(CaseHypothesis hypothesis, CancellationToken ct)
    {
        string? compName = null;
        if (hypothesis.ComponentId.HasValue)
        {
            var comp = await _catalogRepository.GetComponentByIdAsync(hypothesis.ComponentId.Value, ct);
            if (comp != null) compName = comp.Name;
        }

        string? createdByName = null;
        if (hypothesis.CreatedBy.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(hypothesis.CreatedBy.Value, ct);
            if (user != null) createdByName = user.Name;
        }

        var steps = await _diagnosticRepository.GetStepsByHypothesisIdAsync(hypothesis.Id, ct);
        var stepDtos = steps.Select(s => new DiagnosticStepDto(
            Id: s.Id,
            DiagnosticSessionId: s.DiagnosticSessionId,
            SequenceNo: s.SequenceNo,
            StepType: s.StepType,
            HypothesisId: s.HypothesisId,
            HypothesisTitle: hypothesis.Title,
            Title: s.Title,
            Objective: s.Objective,
            Instruction: s.Instruction,
            InputEvidenceSummary: s.InputEvidenceSummary,
            ResultSummary: s.ResultSummary,
            Outcome: s.Outcome,
            RiskLevel: s.RiskLevel,
            DurationSeconds: s.DurationSeconds,
            PerformedBy: s.PerformedBy,
            PerformedByName: null,
            PerformedAt: s.PerformedAt,
            MetadataJson: s.MetadataJson
        )).ToList();

        return new CaseHypothesisDto(
            Id: hypothesis.Id,
            CaseId: hypothesis.CaseId,
            ComponentId: hypothesis.ComponentId,
            ComponentName: compName,
            Title: hypothesis.Title,
            Description: hypothesis.Description,
            Status: hypothesis.Status,
            SourceType: hypothesis.SourceType,
            Justification: hypothesis.Justification,
            CreatedAt: hypothesis.CreatedAt,
            CreatedBy: hypothesis.CreatedBy,
            CreatedByName: createdByName,
            UpdatedAt: hypothesis.UpdatedAt,
            Steps: stepDtos
        );
    }

    public async Task<CaseEvidenceDto> RecordEvidenceAsync(
        RecordEvidenceCommand command,
        long currentUserId,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var @case = await _caseRepository.GetByIdAsync(command.CaseId, ct);
        if (@case == null)
        {
            throw new EntityNotFoundException("Caso", command.CaseId);
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            throw new BusinessRuleValidationException("BR-025", "A descrição da evidência é obrigatória.");
        }

        // Iteração atual se não informada
        var iterationId = command.CaseIterationId ?? 0;
        if (iterationId == 0)
        {
            var iter = await _caseRepository.GetCurrentIterationAsync(command.CaseId, ct);
            iterationId = iter?.Id ?? 0;
        }

        var evidence = new CaseEvidence(
            caseId: command.CaseId,
            evidenceType: command.EvidenceType,
            description: command.Description,
            attachmentId: command.AttachmentId,
            createdBy: currentUserId,
            caseIterationId: iterationId,
            diagnosticStepId: command.DiagnosticStepId
        );

        var evidenceId = await _diagnosticRepository.AddEvidenceAsync(evidence, ct);
        evidence.Id = evidenceId;

        // Processa relações N:N com hipóteses
        var relDtos = new List<CaseHypothesisEvidenceDto>();
        if (command.HypothesisRelations != null && command.HypothesisRelations.Count > 0)
        {
            foreach (var relInput in command.HypothesisRelations)
            {
                if (!Enum.TryParse<EvidenceRelationType>(relInput.RelationType, ignoreCase: true, out var relType))
                {
                    relType = EvidenceRelationType.Inconclusive;
                }

                var hyp = await _diagnosticRepository.GetHypothesisByIdAsync(relInput.HypothesisId, ct);
                var rel = new CaseHypothesisEvidence(
                    evidenceId: evidenceId,
                    hypothesisId: relInput.HypothesisId,
                    relationType: relType,
                    justification: relInput.Justification,
                    createdBy: currentUserId
                );

                await _diagnosticRepository.AddHypothesisEvidenceRelationAsync(rel, ct);

                relDtos.Add(new CaseHypothesisEvidenceDto(
                    Id: rel.Id,
                    EvidenceId: evidenceId,
                    HypothesisId: rel.HypothesisId,
                    HypothesisTitle: hyp?.Title,
                    RelationType: rel.RelationType.ToString(),
                    Justification: rel.Justification,
                    CreatedBy: rel.CreatedBy,
                    CreatedByName: null,
                    CreatedAt: rel.CreatedAt
                ));
            }
        }

        // BR-100: Auditoria
        await _auditEventRepository.AddAsync(new AuditEvent(
            action: "EvidenceRecorded",
            entityType: "CaseEvidence",
            entityId: evidenceId.ToString(),
            actorUserId: currentUserId,
            afterJson: $"Evidence '{evidence.EvidenceType}' recorded with {relDtos.Count} hypothesis relations"
        ), ct);

        var user = await _userRepository.GetByIdAsync(currentUserId, ct);

        return new CaseEvidenceDto(
            Id: evidence.Id,
            CaseId: evidence.CaseId,
            EvidenceType: evidence.EvidenceType,
            Description: evidence.Description,
            AttachmentId: evidence.AttachmentId,
            AttachmentFileName: null,
            CreatedBy: evidence.CreatedBy,
            CreatedAt: evidence.CreatedAt,
            CaseIterationId: evidence.CaseIterationId,
            DiagnosticStepId: evidence.DiagnosticStepId,
            CreatedByName: user?.Name,
            HypothesisRelations: relDtos
        );
    }

    public async Task<IReadOnlyList<CaseEvidenceDto>> GetEvidencesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var evidences = await _diagnosticRepository.GetEvidencesByCaseIdAsync(caseId, ct);
        var result = new List<CaseEvidenceDto>();

        foreach (var ev in evidences)
        {
            string? userName = null;
            if (ev.CreatedBy.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(ev.CreatedBy.Value, ct);
                userName = user?.Name;
            }

            var relDtos = new List<CaseHypothesisEvidenceDto>();
            foreach (var r in ev.HypothesisRelations)
            {
                var hyp = await _diagnosticRepository.GetHypothesisByIdAsync(r.HypothesisId, ct);
                relDtos.Add(new CaseHypothesisEvidenceDto(
                    Id: r.Id,
                    EvidenceId: r.EvidenceId,
                    HypothesisId: r.HypothesisId,
                    HypothesisTitle: hyp?.Title,
                    RelationType: r.RelationType.ToString(),
                    Justification: r.Justification,
                    CreatedBy: r.CreatedBy,
                    CreatedByName: null,
                    CreatedAt: r.CreatedAt
                ));
            }

            result.Add(new CaseEvidenceDto(
                Id: ev.Id,
                CaseId: ev.CaseId,
                EvidenceType: ev.EvidenceType,
                Description: ev.Description,
                AttachmentId: ev.AttachmentId,
                AttachmentFileName: null,
                CreatedBy: ev.CreatedBy,
                CreatedAt: ev.CreatedAt,
                CaseIterationId: ev.CaseIterationId,
                DiagnosticStepId: ev.DiagnosticStepId,
                CreatedByName: userName,
                HypothesisRelations: relDtos
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<CaseEvidenceDto>> GetEvidencesByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        var evidences = await _diagnosticRepository.GetEvidencesByHypothesisIdAsync(hypothesisId, ct);
        var result = new List<CaseEvidenceDto>();

        foreach (var ev in evidences)
        {
            string? userName = null;
            if (ev.CreatedBy.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(ev.CreatedBy.Value, ct);
                userName = user?.Name;
            }

            var relDtos = new List<CaseHypothesisEvidenceDto>();
            foreach (var r in ev.HypothesisRelations)
            {
                var hyp = await _diagnosticRepository.GetHypothesisByIdAsync(r.HypothesisId, ct);
                relDtos.Add(new CaseHypothesisEvidenceDto(
                    Id: r.Id,
                    EvidenceId: r.EvidenceId,
                    HypothesisId: r.HypothesisId,
                    HypothesisTitle: hyp?.Title,
                    RelationType: r.RelationType.ToString(),
                    Justification: r.Justification,
                    CreatedBy: r.CreatedBy,
                    CreatedByName: null,
                    CreatedAt: r.CreatedAt
                ));
            }

            result.Add(new CaseEvidenceDto(
                Id: ev.Id,
                CaseId: ev.CaseId,
                EvidenceType: ev.EvidenceType,
                Description: ev.Description,
                AttachmentId: ev.AttachmentId,
                AttachmentFileName: null,
                CreatedBy: ev.CreatedBy,
                CreatedAt: ev.CreatedAt,
                CaseIterationId: ev.CaseIterationId,
                DiagnosticStepId: ev.DiagnosticStepId,
                CreatedByName: userName,
                HypothesisRelations: relDtos
            ));
        }

        return result;
    }
}
