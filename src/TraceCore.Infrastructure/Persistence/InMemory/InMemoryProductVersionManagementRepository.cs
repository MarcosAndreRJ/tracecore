using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.InMemory;

/// <summary>
/// Implementação InMemory do repositório de versionamento (Fase 1) — somente testes.
/// Espelha o comportamento do MySqlProductVersionManagementRepository.
/// </summary>
public class InMemoryProductVersionManagementRepository : IProductVersionManagementRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryProductVersionManagementRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<ProductVersionChange>> GetChangesByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        IReadOnlyList<ProductVersionChange> list = _store.ProductVersionChanges.Values
            .Where(c => c.ProductVersionId == productVersionId)
            .OrderBy(c => c.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ProductVersionChange>> GetFixChangesForProductAsync(long productId, int minReleaseOrderExclusive, CancellationToken ct = default)
    {
        var targetVersions = _store.ProductVersions.Values
            .Where(v => v.ProductId == productId && v.ReleaseOrder > minReleaseOrderExclusive && v.Status == "Active")
            .ToDictionary(v => v.Id, v => v.ReleaseOrder);

        IReadOnlyList<ProductVersionChange> list = _store.ProductVersionChanges.Values
            .Where(c => c.ChangeType == "Fix" && targetVersions.ContainsKey(c.ProductVersionId))
            .OrderBy(c => targetVersions[c.ProductVersionId])
            .ThenBy(c => c.Id)
            .ToList();

        return Task.FromResult(list);
    }

    public Task<ProductVersionChange?> GetChangeByIdAsync(long id, CancellationToken ct = default)
    {
        _store.ProductVersionChanges.TryGetValue(id, out var change);
        return Task.FromResult(change);
    }

    public Task<long> AddChangeAsync(ProductVersionChange change, CancellationToken ct = default)
    {
        change.Id = _store.NextVersionChangeId();
        _store.ProductVersionChanges[change.Id] = change;
        return Task.FromResult(change.Id);
    }

    public Task UpdateChangeAsync(ProductVersionChange change, CancellationToken ct = default)
    {
        _store.ProductVersionChanges[change.Id] = change;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteChangeAsync(long id, CancellationToken ct = default)
    {
        lock (_store.ProductVersionChangeCases)
        {
            _store.ProductVersionChangeCases.RemoveAll(l => l.ProductVersionChangeId == id);
        }
        return Task.FromResult(_store.ProductVersionChanges.TryRemove(id, out _));
    }

    public Task<IReadOnlyList<ProductVersionChangeCase>> GetLinkedCasesByChangeIdAsync(long changeId, CancellationToken ct = default)
    {
        List<ProductVersionChangeCase> list;
        lock (_store.ProductVersionChangeCases)
        {
            list = _store.ProductVersionChangeCases
                .Where(l => l.ProductVersionChangeId == changeId)
                .ToList();
        }
        return Task.FromResult<IReadOnlyList<ProductVersionChangeCase>>(list);
    }

    public Task<IReadOnlyList<ProductVersionChangeCase>> GetChangesLinkedToCaseAsync(long caseId, CancellationToken ct = default)
    {
        List<ProductVersionChangeCase> list;
        lock (_store.ProductVersionChangeCases)
        {
            list = _store.ProductVersionChangeCases
                .Where(l => l.CaseId == caseId)
                .OrderBy(l => l.LinkedAt)
                .ToList();
        }
        return Task.FromResult<IReadOnlyList<ProductVersionChangeCase>>(list);
    }

    public Task<bool> ChangeCaseLinkExistsAsync(long changeId, long caseId, string relationType, CancellationToken ct = default)
    {
        lock (_store.ProductVersionChangeCases)
        {
            var exists = _store.ProductVersionChangeCases.Any(l =>
                l.ProductVersionChangeId == changeId && l.CaseId == caseId && l.RelationType == relationType);
            return Task.FromResult(exists);
        }
    }

    public Task AddChangeCaseLinkAsync(ProductVersionChangeCase link, CancellationToken ct = default)
    {
        lock (_store.ProductVersionChangeCases)
        {
            _store.ProductVersionChangeCases.Add(link);
        }
        return Task.CompletedTask;
    }

    public Task<bool> DeleteChangeCaseLinkAsync(long changeId, long caseId, string relationType, CancellationToken ct = default)
    {
        lock (_store.ProductVersionChangeCases)
        {
            var removed = _store.ProductVersionChangeCases.RemoveAll(l =>
                l.ProductVersionChangeId == changeId && l.CaseId == caseId && l.RelationType == relationType);
            return Task.FromResult(removed > 0);
        }
    }

    public Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        IReadOnlyList<ProductVersionAssignment> list = _store.ProductVersionAssignments.Values
            .Where(a => a.ProductVersionId == productVersionId)
            .OrderBy(a => a.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        IReadOnlyList<ProductVersionAssignment> list = _store.ProductVersionAssignments.Values
            .Where(a => a.ClientId == clientId)
            .OrderBy(a => a.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<ProductVersionAssignment?> GetAssignmentByIdAsync(long id, CancellationToken ct = default)
    {
        _store.ProductVersionAssignments.TryGetValue(id, out var assignment);
        return Task.FromResult(assignment);
    }

    public Task<long> AddAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default)
    {
        assignment.Id = _store.NextVersionAssignmentId();
        _store.ProductVersionAssignments[assignment.Id] = assignment;
        return Task.FromResult(assignment.Id);
    }

    public Task UpdateAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default)
    {
        _store.ProductVersionAssignments[assignment.Id] = assignment;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAssignmentAsync(long id, CancellationToken ct = default)
    {
        return Task.FromResult(_store.ProductVersionAssignments.TryRemove(id, out _));
    }

    public Task<bool> AssignmentExistsAsync(long productVersionId, long clientId, long? clientUnitId, CancellationToken ct = default)
    {
        // Compara valores nulos equivalentes (unidade nula = "cliente inteiro").
        var exists = _store.ProductVersionAssignments.Values.Any(a =>
            a.ProductVersionId == productVersionId &&
            a.ClientId == clientId &&
            (a.ClientUnitId ?? 0) == (clientUnitId ?? 0));
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyDictionary<long, int>> GetCaseCountsByVersionIdsAsync(IEnumerable<long> versionIds, CancellationToken ct = default)
    {
        var set = new HashSet<long>(versionIds);
        var counts = _store.Cases.Values
            .Where(c => c.ProductVersionId.HasValue && set.Contains(c.ProductVersionId.Value))
            .GroupBy(c => c.ProductVersionId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var id in set)
        {
            if (!counts.ContainsKey(id))
                counts[id] = 0;
        }
        return Task.FromResult<IReadOnlyDictionary<long, int>>(counts);
    }

    public Task<IReadOnlyList<VersionLinkedCaseDetailDb>> GetLinkedCaseDetailsByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        var changes = _store.ProductVersionChanges.Values
            .Where(c => c.ProductVersionId == productVersionId)
            .ToDictionary(c => c.Id);

        var list = new List<VersionLinkedCaseDetailDb>();
        lock (_store.ProductVersionChangeCases)
        {
            foreach (var link in _store.ProductVersionChangeCases)
            {
                if (changes.TryGetValue(link.ProductVersionChangeId, out var change))
                {
                    if (_store.Cases.TryGetValue(link.CaseId, out var c))
                    {
                        Client? client = null;
                        if (c.ClientId.HasValue)
                        {
                            _store.Clients.TryGetValue(c.ClientId.Value, out client);
                        }

                        string? occurredInLabel = null;
                        if (c.ProductVersionId.HasValue && _store.ProductVersions.TryGetValue(c.ProductVersionId.Value, out var occPv))
                        {
                            occurredInLabel = occPv.VersionLabel;
                        }
                        list.Add(new VersionLinkedCaseDetailDb(
                            ChangeId: change.Id,
                            ChangeTitle: change.Title,
                            ChangeType: change.ChangeType,
                            CaseId: c.Id,
                            CaseNumber: c.CaseNumber,
                            CaseTitle: c.NormalizedSummary ?? c.OriginalReport,
                            CaseStatus: c.Status.ToString(),
                            ClientId: c.ClientId,
                            ClientName: client?.Name,
                            OccurredInVersionId: c.ProductVersionId,
                            OccurredInVersionLabel: occurredInLabel,
                            RelationType: link.RelationType,
                            LinkedAt: link.LinkedAt,
                            CaseOpenedAt: c.OpenedAt
                        ));
                    }
                }
            }
        }
        return Task.FromResult<IReadOnlyList<VersionLinkedCaseDetailDb>>(list.OrderByDescending(x => x.LinkedAt).ToList());
    }

    public Task<IReadOnlyList<ClientVersionCaseDb>> GetCasesByClientAndPeriodAsync(long clientId, long productId, DateTime from, DateTime? to, CancellationToken ct = default)
    {
        var cases = _store.Cases.Values
            .Where(c => c.ClientId == clientId && c.ProductId == productId && c.OpenedAt >= from && (to == null || c.OpenedAt < to.Value))
            .OrderByDescending(c => c.OpenedAt)
            .Select(c => new ClientVersionCaseDb(
                CaseId: c.Id,
                CaseNumber: c.CaseNumber,
                Title: c.NormalizedSummary ?? c.OriginalReport,
                Status: c.Status.ToString(),
                ErrorCode: c.ErrorCode,
                OpenedAt: c.OpenedAt
            ))
            .ToList();
        return Task.FromResult<IReadOnlyList<ClientVersionCaseDb>>(cases);
    }

    public Task<int> GetPostReleaseSimilarCasesCountAsync(long productId, long productVersionId, string? errorCode, long? componentId, DateTime releaseDate, CancellationToken ct = default)
    {
        var count = _store.Cases.Values.Count(c =>
            c.ProductId == productId &&
            c.OpenedAt >= releaseDate &&
            ((!string.IsNullOrWhiteSpace(errorCode) && string.Equals(c.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase)) ||
             (componentId.HasValue && c.AffectedComponents.Any(ac => ac.ComponentId == componentId.Value))));
        return Task.FromResult(count);
    }
}