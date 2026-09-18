using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public class CaseService : ICaseService
{
    private readonly ICaseRepository _caseRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IAuditService _auditService;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".log", ".txt", ".pdf", ".json", ".csv", ".xml", ".zip"
    };

    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".vbs", ".js", ".msi", ".com", ".scr"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public CaseService(
        ICaseRepository caseRepository,
        IClientRepository clientRepository,
        ICatalogRepository catalogRepository,
        IAttachmentRepository attachmentRepository,
        IFileStorage fileStorage,
        IAuditService auditService,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository)
    {
        _caseRepository = caseRepository;
        _clientRepository = clientRepository;
        _catalogRepository = catalogRepository;
        _attachmentRepository = attachmentRepository;
        _fileStorage = fileStorage;
        _auditService = auditService;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<CaseDto> OpenCaseAsync(OpenCaseCommand command, long? currentUserId = null, CancellationToken ct = default)
    {
        // BR-020: Relato original é obrigatório e nunca pode ser sobrescrito
        if (string.IsNullOrWhiteSpace(command.OriginalReport))
        {
            throw new ArgumentException("Relato original da ocorrência é obrigatório.", nameof(command.OriginalReport));
        }

        // Reserva atômica de case_number estável e não reaproveitável
        var caseNumber = await _caseRepository.NextCaseNumberAsync(ct);

        var @case = new Case(
            originalReport: command.OriginalReport,
            caseNumber: caseNumber,
            severity: command.Severity ?? "Medium",
            impactLevel: command.ImpactLevel,
            clientId: command.ClientId,
            productId: command.ProductId,
            productVersionId: command.ProductVersionId,
            environmentId: command.EnvironmentId,
            errorCode: command.ErrorCode,
            errorMessage: command.ErrorMessage,
            scopeType: command.ScopeType,
            externalReference: command.ExternalReference,
            sourceType: "Manual",
            currentDepartmentId: command.CurrentDepartmentId,
            currentOwnerUserId: command.CurrentOwnerUserId,
            createdBy: currentUserId,
            openedAt: command.OpenedAt ?? DateTime.UtcNow
        );

        // Sintomas textuais iniciais
        if (command.Symptoms != null)
        {
            foreach (var symptom in command.Symptoms)
            {
                @case.AddSymptom(symptom);
            }
        }

        // BR-023: Múltiplos componentes afetados (0 a N)
        if (command.ComponentIds != null)
        {
            foreach (var compId in command.ComponentIds.Distinct())
            {
                @case.AddComponent(compId, "Affected");
            }
        }

        // Evidências textuais iniciais
        if (command.Evidences != null)
        {
            foreach (var ev in command.Evidences)
            {
                @case.AddEvidence(ev.EvidenceType, ev.Description, createdBy: currentUserId);
            }
        }

        // Persiste o caso
        var caseId = await _caseRepository.AddAsync(@case, ct);
        @case.Id = caseId;

        // Processamento de anexos físicos e metadados (ADR-P004 / 12_SEGURANCA §8)
        if (command.Attachments != null && command.Attachments.Count > 0)
        {
            foreach (var attInput in command.Attachments)
            {
                var attachment = await ProcessAndStoreAttachmentAsync(caseId, attInput, currentUserId ?? 1, ct);
                // Vincula evidência do tipo correspondente
                @case.AddEvidence(
                    evidenceType: attInput.MimeType.StartsWith("image/") ? "Screenshot" : "Log",
                    description: $"Anexo enviado: {attInput.FileName}",
                    attachmentId: attachment.Id,
                    createdBy: currentUserId
                );
            }
        }

        // BR-100: Registro de Auditoria imutável
        await _auditService.RecordAsync(
            action: "CaseOpened",
            entityType: "Case",
            entityId: caseId.ToString(),
            actorUserId: currentUserId,
            metadata: new
            {
                CaseNumber = caseNumber,
                Severity = @case.Severity,
                ClientId = @case.ClientId,
                ProductId = @case.ProductId,
                ComponentsCount = @case.AffectedComponents.Count
            },
            ct: ct
        );

        return await MapToDtoAsync(@case, ct);
    }

    public async Task<CaseDto?> GetCaseByIdAsync(long id, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByIdAsync(id, ct);
        if (@case == null) return null;
        return await MapToDtoAsync(@case, ct);
    }

    public async Task<CaseDto?> GetCaseByNumberAsync(ulong caseNumber, CancellationToken ct = default)
    {
        var @case = await _caseRepository.GetByCaseNumberAsync(caseNumber, ct);
        if (@case == null) return null;
        return await MapToDtoAsync(@case, ct);
    }

    public async Task<IReadOnlyList<CaseDto>> GetAllCasesAsync(int limit = 50, CancellationToken ct = default)
    {
        var cases = await _caseRepository.GetAllAsync(limit, ct);
        var list = new List<CaseDto>();
        foreach (var c in cases)
        {
            list.Add(await MapToDtoAsync(c, ct));
        }
        return list;
    }

    public async Task UpdateNormalizedSummaryAsync(long caseId, string? normalizedSummary, long? currentUserId = null, CancellationToken ct = default)
    {
        // BR-020 e BR-021: Atualiza apenas o resumo normalizado, sem tocar no relato original
        await _caseRepository.UpdateNormalizedSummaryAsync(caseId, normalizedSummary, currentUserId, ct);

        await _auditService.RecordAsync(
            action: "CaseSummaryNormalized",
            entityType: "Case",
            entityId: caseId.ToString(),
            actorUserId: currentUserId,
            after: new { NormalizedSummary = normalizedSummary },
            ct: ct
        );
    }

    public async Task<CaseIterationDto> ReopenCaseAsync(long caseId, string reason, long reopenedBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleValidationException("BR-029", "O motivo da reabertura é obrigatório.");

        var @case = await _caseRepository.GetByIdAsync(caseId, ct);
        if (@case == null)
            throw new EntityNotFoundException("Caso", caseId);

        if (!string.Equals(@case.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleValidationException("BR-029", "Apenas casos com status 'Resolved' podem ser reabertos.");

        var newIteration = @case.Reopen(reason, reopenedBy);
        var iterationId = await _caseRepository.AddIterationAsync(newIteration, ct);
        newIteration.Id = iterationId;

        await _caseRepository.UpdateCaseReopenStatusAsync(caseId, "Reopened", reopenedBy, ct);

        await _auditService.RecordAsync(
            action: "CaseReopened",
            entityType: "Case",
            entityId: caseId.ToString(),
            actorUserId: reopenedBy,
            after: new { SequenceNumber = newIteration.SequenceNumber, Reason = reason },
            ct: ct
        );

        var opener = await _userRepository.GetByIdAsync(reopenedBy, ct);

        return new CaseIterationDto(
            newIteration.Id,
            newIteration.CaseId,
            newIteration.SequenceNumber,
            newIteration.OpenedAt,
            newIteration.OpenedBy,
            opener?.Name,
            newIteration.Reason,
            newIteration.ClosedAt,
            newIteration.Status
        );
    }

    public async Task<IReadOnlyList<ClientDto>> GetClientsAsync(CancellationToken ct = default)
    {
        var clients = await _clientRepository.GetAllAsync(ct);
        return clients.Select(c => new ClientDto(c.Id, c.Code, c.Name, c.Status)).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var products = await _catalogRepository.GetAllProductsAsync(ct);
        return products.Select(p => new ProductDto(p.Id, p.Code, p.Name, p.Description, p.Status)).ToList();
    }

    public async Task<IReadOnlyList<ProductVersionDto>> GetVersionsByProductAsync(long productId, CancellationToken ct = default)
    {
        var versions = await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
        return versions.Select(v => new ProductVersionDto(v.Id, v.ProductId, v.VersionLabel, v.Status)).ToList();
    }

    public async Task<IReadOnlyList<EnvironmentDto>> GetEnvironmentsAsync(CancellationToken ct = default)
    {
        var envs = await _catalogRepository.GetAllEnvironmentsAsync(ct);
        return envs.Select(e => new EnvironmentDto(e.Id, e.Name, e.EnvironmentType)).ToList();
    }

    public async Task<IReadOnlyList<ComponentDto>> GetComponentsAsync(long? productId = null, CancellationToken ct = default)
    {
        var comps = await _catalogRepository.GetAllComponentsAsync(productId, ct);
        return comps.Select(c => new ComponentDto(c.Id, c.ProductId, c.Code, c.Name, c.ComponentType, c.Description, c.OwnerDepartmentId, c.Status)).ToList();
    }

    private async Task<Attachment> ProcessAndStoreAttachmentAsync(long caseId, AttachmentInputDto input, long uploadedBy, CancellationToken ct)
    {
        var extension = Path.GetExtension(input.FileName);
        if (string.IsNullOrWhiteSpace(extension) || BlockedExtensions.Contains(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Extensão de arquivo não permitida: '{extension}'. Formatos aceitos: imagens, logs, txt, pdf, json, csv, xml, zip.", nameof(input.FileName));
        }

        // Lê o stream para memória para cálculo seguro de hash e tamanho
        using var memoryStream = new MemoryStream();
        if (input.ContentStream.CanSeek)
        {
            input.ContentStream.Position = 0;
        }
        await input.ContentStream.CopyToAsync(memoryStream, ct);

        if (memoryStream.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"Arquivo excede o limite máximo permitido de {MaxFileSizeBytes / (1024 * 1024)} MB.", nameof(input.FileName));
        }

        memoryStream.Position = 0;

        // Calcula SHA-256 do arquivo físico
        string sha256Hex;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(memoryStream, ct);
            sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        memoryStream.Position = 0;

        // TODO: 12_SEGURANCA §8 - Verificação por antivírus em upload quando ferramenta estiver disponível no ambiente (ex: ClamAV/Defender).

        // Grava no disco com nome baseado em GUID/Hash via IFileStorage (ADR-P004)
        var storageKey = await _fileStorage.SaveAsync(input.FileName, memoryStream, input.MimeType, ct);

        var attachment = new Attachment(
            entityType: "Case",
            entityId: caseId,
            fileName: input.FileName,
            mimeType: input.MimeType,
            sizeBytes: (ulong)memoryStream.Length,
            sha256: sha256Hex,
            storageKey: storageKey,
            uploadedBy: uploadedBy,
            confidentiality: input.Confidentiality
        );

        var attachmentId = await _attachmentRepository.AddAsync(attachment, ct);
        attachment.Id = attachmentId;

        return attachment;
    }

    private async Task<CaseDto> MapToDtoAsync(Case c, CancellationToken ct)
    {
        string? clientName = null;
        if (c.ClientId.HasValue)
        {
            var client = await _clientRepository.GetByIdAsync(c.ClientId.Value, ct);
            clientName = client?.Name;
        }

        string? productName = null;
        if (c.ProductId.HasValue)
        {
            var product = await _catalogRepository.GetProductByIdAsync(c.ProductId.Value, ct);
            productName = product?.Name;
        }

        string? versionLabel = null;
        if (c.ProductVersionId.HasValue)
        {
            var version = await _catalogRepository.GetProductVersionByIdAsync(c.ProductVersionId.Value, ct);
            versionLabel = version?.VersionLabel;
        }

        string? envName = null;
        if (c.EnvironmentId.HasValue)
        {
            var env = await _catalogRepository.GetEnvironmentByIdAsync(c.EnvironmentId.Value, ct);
            envName = env?.Name;
        }

        string? ownerName = null;
        if (c.CurrentOwnerUserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(c.CurrentOwnerUserId.Value, ct);
            ownerName = user?.Name;
        }

        string? deptName = null;
        if (c.CurrentDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(c.CurrentDepartmentId.Value, ct);
            deptName = dept?.Name;
        }

        var symptomsDto = c.Symptoms.Select(s => new CaseSymptomDto(s.Id, s.CaseId, s.SymptomCode, s.SymptomText, s.Source, s.Confirmed)).ToList();

        var componentsDto = new List<CaseComponentDto>();
        foreach (var comp in c.AffectedComponents)
        {
            var compEntity = await _catalogRepository.GetComponentByIdAsync(comp.ComponentId, ct);
            componentsDto.Add(new CaseComponentDto(
                comp.CaseId,
                comp.ComponentId,
                compEntity?.Name,
                compEntity?.Code,
                comp.RelationType,
                comp.ConfidenceLabel
            ));
        }

        var attachments = await _attachmentRepository.GetByEntityAsync("Case", c.Id, ct);
        var attachmentsDto = attachments.Select(a => new AttachmentDto(
            a.Id, a.EntityType, a.EntityId, a.FileName, a.MimeType, a.SizeBytes, a.Sha256, a.StorageKey, a.Confidentiality, a.UploadedBy, a.UploadedAt
        )).ToList();

        var evidencesDto = c.Evidences.Select(e =>
        {
            var att = attachments.FirstOrDefault(a => a.Id == e.AttachmentId);
            return new CaseEvidenceDto(e.Id, e.CaseId, e.EvidenceType, e.Description, e.AttachmentId, att?.FileName, e.CreatedBy, e.CreatedAt);
        }).ToList();

        var iterations = await _caseRepository.GetIterationsByCaseIdAsync(c.Id, ct);
        var iterationsDto = new List<CaseIterationDto>();
        foreach (var iter in iterations)
        {
            var opener = await _userRepository.GetByIdAsync(iter.OpenedBy, ct);
            iterationsDto.Add(new CaseIterationDto(
                iter.Id,
                iter.CaseId,
                iter.SequenceNumber,
                iter.OpenedAt,
                iter.OpenedBy,
                opener?.Name,
                iter.Reason,
                iter.ClosedAt,
                iter.Status
            ));
        }

        return new CaseDto(
            Id: c.Id,
            CaseNumber: c.CaseNumber,
            ExternalReference: c.ExternalReference,
            SourceType: c.SourceType,
            ClientId: c.ClientId,
            ClientName: clientName,
            ProductId: c.ProductId,
            ProductName: productName,
            ProductVersionId: c.ProductVersionId,
            VersionLabel: versionLabel,
            EnvironmentId: c.EnvironmentId,
            EnvironmentName: envName,
            OriginalReport: c.OriginalReport,
            NormalizedSummary: c.NormalizedSummary,
            ExpectedBehavior: c.ExpectedBehavior,
            ObservedBehavior: c.ObservedBehavior,
            ErrorCode: c.ErrorCode,
            ErrorMessage: c.ErrorMessage,
            ScopeType: c.ScopeType,
            Severity: c.Severity,
            ImpactLevel: c.ImpactLevel,
            Status: c.Status,
            CurrentOwnerUserId: c.CurrentOwnerUserId,
            CurrentOwnerUserName: ownerName,
            CurrentDepartmentId: c.CurrentDepartmentId,
            CurrentDepartmentName: deptName,
            RootCauseStatus: c.RootCauseStatus,
            OpenedAt: c.OpenedAt,
            CreatedAt: c.CreatedAt,
            CreatedBy: c.CreatedBy,
            UpdatedAt: c.UpdatedAt,
            UpdatedBy: c.UpdatedBy,
            RowVersion: c.RowVersion,
            Symptoms: symptomsDto,
            AffectedComponents: componentsDto,
            Evidences: evidencesDto,
            Attachments: attachmentsDto,
            Resolution: null,
            Iterations: iterationsDto
        );
    }
}
