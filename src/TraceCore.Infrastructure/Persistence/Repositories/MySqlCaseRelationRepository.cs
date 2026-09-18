using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlCaseRelationRepository : ICaseRelationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCaseRelationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, 
                   source_case_id AS SourceCaseId, 
                   target_case_id AS TargetCaseId, 
                   relation_type AS RelationType, 
                   similarity_score AS SimilarityScore, 
                   matched_factors_json AS MatchedFactorsJson, 
                   created_by AS CreatedBy, 
                   created_at AS CreatedAt
            FROM case_relations
            WHERE source_case_id = @CaseId OR target_case_id = @CaseId
            ORDER BY similarity_score DESC, created_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<CaseRelation>(sql, new { CaseId = caseId });
        return rows.ToList();
    }

    public async Task SaveSimilarRelationsAsync(long sourceCaseId, IEnumerable<CaseRelation> relations, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        try
        {
            // Remove relações Similar anteriores deste sourceCaseId
            const string deleteSql = @"
                DELETE FROM case_relations 
                WHERE source_case_id = @SourceCaseId AND relation_type = 'Similar';";
            await conn.ExecuteAsync(deleteSql, new { SourceCaseId = sourceCaseId }, tx);

            const string insertSql = @"
                INSERT INTO case_relations (
                    source_case_id, target_case_id, relation_type, similarity_score, matched_factors_json, created_by, created_at
                ) VALUES (
                    @SourceCaseId, @TargetCaseId, @RelationType, @SimilarityScore, @MatchedFactorsJson, @CreatedBy, @CreatedAt
                );";

            foreach (var r in relations)
            {
                await conn.ExecuteAsync(insertSql, new
                {
                    r.SourceCaseId,
                    r.TargetCaseId,
                    r.RelationType,
                    r.SimilarityScore,
                    r.MatchedFactorsJson,
                    r.CreatedBy,
                    r.CreatedAt
                }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<long> AddManualRelationAsync(CaseRelation relation, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_relations (
                source_case_id, target_case_id, relation_type, similarity_score, matched_factors_json, created_by, created_at
            ) VALUES (
                @SourceCaseId, @TargetCaseId, @RelationType, @SimilarityScore, @MatchedFactorsJson, @CreatedBy, @CreatedAt
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            relation.SourceCaseId,
            relation.TargetCaseId,
            relation.RelationType,
            relation.SimilarityScore,
            relation.MatchedFactorsJson,
            relation.CreatedBy,
            relation.CreatedAt
        });
    }

    public async Task<IReadOnlyList<Case>> GetPotentialSimilarCandidatesAsync(long excludeCaseId, long? productId, string? errorCode, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                c.id AS Id,
                c.case_number AS CaseNumber,
                c.original_report AS OriginalReport,
                c.normalized_summary AS NormalizedSummary,
                c.status AS Status,
                c.severity AS Severity,
                c.product_id AS ProductId,
                c.product_version_id AS ProductVersionId,
                c.error_code AS ErrorCode,
                c.opened_at AS OpenedAt,
                c.resolved_at AS ResolvedAt
            FROM cases c
            WHERE c.id != @ExcludeCaseId
            ORDER BY c.opened_at DESC
            LIMIT @Limit;";

        var cases = (await conn.QueryAsync<Case>(sql, new { ExcludeCaseId = excludeCaseId, Limit = limit })).ToList();

        if (cases.Count > 0)
        {
            var caseIds = cases.Select(c => c.Id).ToList();
            const string compSql = @"
                SELECT case_id AS CaseId, component_id AS ComponentId, relation_type AS RelationType, confidence_label AS ConfidenceLabel
                FROM case_components
                WHERE case_id IN @CaseIds;";

            var components = await conn.QueryAsync<CaseComponent>(compSql, new { CaseIds = caseIds });
            var compLookup = components.ToLookup(cc => cc.CaseId);

            foreach (var c in cases)
            {
                c.AffectedComponents = compLookup[c.Id].ToList();
            }
        }

        return cases;
    }

    public async Task<IReadOnlyList<DiagnosticStep>> GetSuccessfulDiagnosticStepsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        var idList = caseIds.ToList();
        if (idList.Count == 0) return new List<DiagnosticStep>();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ds.id AS Id,
                ds.diagnostic_session_id AS DiagnosticSessionId,
                ds.title AS Title,
                ds.action_description AS ActionDescription,
                ds.expected_result AS ExpectedResult,
                ds.observed_result AS ObservedResult,
                ds.outcome AS Outcome,
                ds.performed_at AS PerformedAt,
                ds.performed_by AS PerformedBy,
                ds.hypothesis_id AS HypothesisId
            FROM diagnostic_steps ds
            INNER JOIN diagnostic_sessions s ON s.id = ds.diagnostic_session_id
            WHERE s.case_id IN @CaseIds AND ds.outcome = 'Worked'
            ORDER BY ds.performed_at ASC;";

        var rows = await conn.QueryAsync<DiagnosticStep>(sql, new { CaseIds = idList });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CaseResolution>> GetResolutionsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        var idList = caseIds.ToList();
        if (idList.Count == 0) return new List<CaseResolution>();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                cr.id AS Id,
                cr.case_id AS CaseId,
                cr.case_iteration_id AS CaseIterationId,
                cr.resolution_summary AS ResolutionSummary,
                cr.validation_summary AS ValidationSummary,
                cr.root_cause_id AS RootCauseId,
                cr.root_cause_confirmed AS RootCauseConfirmed,
                cr.responsible_department_id AS ResponsibleDepartmentId,
                cr.resolved_by AS ResolvedBy,
                cr.resolved_at AS ResolvedAt
            FROM case_resolutions cr
            WHERE cr.case_id IN @CaseIds;";

        var rows = await conn.QueryAsync<CaseResolution>(sql, new { CaseIds = idList });
        return rows.ToList();
    }
}
