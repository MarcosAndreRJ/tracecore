using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

/// <summary>
/// Serviço do Versionamento Inteligente (Fase 1): itens de alteração de cada release,
/// vínculo de alterações com casos (sem nunca alterar a versão do caso) e destinação
/// (rollout) de versões para clientes.
/// </summary>
public interface IVersionManagementService
{
    // ================ Alterações por versão ================

    Task<IReadOnlyList<ProductVersionChangeDto>> GetChangesByVersionIdAsync(long productVersionId, CancellationToken ct = default);
    Task<long> CreateChangeAsync(long productVersionId, string changeType, string title, string? description = null, long? componentId = null, string? errorCode = null, long? currentUserId = null, CancellationToken ct = default);
    Task UpdateChangeAsync(long id, string changeType, string title, string? description = null, long? componentId = null, string? errorCode = null, long? currentUserId = null, CancellationToken ct = default);
    Task<bool> DeleteChangeAsync(long id, long? currentUserId = null, CancellationToken ct = default);

    // ================ Vínculo alteração <-> caso e sugestões (Fase 3) ================

    Task<IReadOnlyList<VersionChangeCaseLinkDto>> GetLinkedCasesAsync(long changeId, CancellationToken ct = default);
    Task LinkCaseAsync(long changeId, long caseId, string relationType, decimal? matchScore = null, string? matchedFactorsJson = null, long? currentUserId = null, CancellationToken ct = default);
    Task<bool> UnlinkCaseAsync(long changeId, long caseId, string relationType, long? currentUserId = null, CancellationToken ct = default);
    Task<IReadOnlyList<VersionChangeCaseSuggestionDto>> SuggestCasesForChangeAsync(VersionChangeCaseSuggestionInput input, CancellationToken ct = default);

    // ================ Destinação / rollout ================

    Task<IReadOnlyList<ProductVersionAssignmentDto>> GetAssignmentsByVersionIdAsync(long productVersionId, CancellationToken ct = default);
    Task<long> CreateAssignmentAsync(long productVersionId, long clientId, long? clientUnitId, string? notes = null, long? currentUserId = null, CancellationToken ct = default);
    Task ConfirmAssignmentDeployedAsync(long assignmentId, long? environmentId = null, long? currentUserId = null, CancellationToken ct = default);
    Task ScheduleAssignmentAsync(long assignmentId, System.DateTime scheduledAt, long? currentUserId = null, CancellationToken ct = default);
    Task FailAssignmentAsync(long assignmentId, string? notes = null, long? currentUserId = null, CancellationToken ct = default);
    Task RemoveAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default);
    Task ReopenAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default);
    Task SkipAssignmentAsync(long assignmentId, long? currentUserId = null, CancellationToken ct = default);

    // ================ Atualização manual de versão do cliente ================

    Task ManualClientVersionUpdateAsync(long clientId, long productId, long newProductVersionId, long? clientUnitId = null, long? environmentId = null, System.DateTime? effectiveFrom = null, string? notes = null, long? currentUserId = null, CancellationToken ct = default);

    // ================ Versão Ativa do Cliente e Sugestão de Correções (Fase 4) ================

    Task<ClientActiveVersionDto> GetActiveVersionForClientAsync(long clientId, long productId, long? clientUnitId = null, CancellationToken ct = default);
    Task<IReadOnlyList<PossibleFixSuggestionDto>> SuggestPossibleFixesAsync(long productId, long? currentProductVersionId, string? title, string? description, long? componentId, string? errorCode, CancellationToken ct = default);
    Task<CaseVersionContextDto> GetVersionContextForCaseAsync(long caseId, CancellationToken ct = default);

    // ================ Consolidação, Indicadores, Recorrência e Copiloto (Fase 5) ================

    Task<VersionIndicatorsDto> GetVersionIndicatorsAsync(long productVersionId, CancellationToken ct = default);
    Task<IReadOnlyList<VersionIndicatorsDto>> GetVersionIndicatorsForProductAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<VersionCaseCountDto>> GetCasesCountByVersionForProductAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<VersionLinkedCaseDetailDto>> GetVersionLinkedCasesAsync(long productVersionId, CancellationToken ct = default);
    Task<FixRecurrenceObservationDto> GetFixRecurrenceAsync(long changeId, CancellationToken ct = default);
    Task<ClientVersionTimelineDto> GetClientVersionTimelineWithCasesAsync(long clientId, long productId, long? clientUnitId = null, CancellationToken ct = default);
    Task<ClientVersionCopilotContextDto> GetClientVersionCopilotContextAsync(long clientId, long productId, long? clientUnitId = null, CancellationToken ct = default);
    Task<IReadOnlyList<LaterVersionFixCopilotDto>> SearchVersionFixesForCopilotAsync(long productId, long? currentProductVersionId, string? query, string? errorCode, CancellationToken ct = default);
}