using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

// TODO: BR-029 — Reabertura de caso preservará a resolução anterior e iniciará nova iteração (Fase 6).
// TODO: BR-030 — Relacionamento entre casos (duplicado, semelhante, recorrência) será implementado em fases futuras.
// TODO: BR-046 / M05 — Publicação formal do caso como item na Base de Conhecimento consumirá o campo 'preventive_actions'
// e os dados aqui registrados para criar conhecimento reutilizável.

public class CaseResolutionService : ICaseResolutionService
{
    private readonly ICaseResolutionRepository _caseResolutionRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IDiagnosticRepository _diagnosticRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditEventRepository _auditEventRepository;

    public CaseResolutionService(
        ICaseResolutionRepository caseResolutionRepository,
        ICaseRepository caseRepository,
        IDiagnosticRepository diagnosticRepository,
        ICatalogRepository catalogRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IAuditEventRepository auditEventRepository)
    {
        _caseResolutionRepository = caseResolutionRepository;
        _caseRepository = caseRepository;
        _diagnosticRepository = diagnosticRepository;
        _catalogRepository = catalogRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _auditEventRepository = auditEventRepository;
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
        var resolutionType = string.Equals(command.ResolutionType, "Workaround", StringComparison.OrdinalIgnoreCase)
            ? "Workaround"
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
            TotalHypothesesTestedCount: totalHypotheses
        );
    }
}
