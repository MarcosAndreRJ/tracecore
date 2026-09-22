using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação MySQL/Dapper do repositório de versionamento (Fase 1).
/// </summary>
public class MySqlProductVersionManagementRepository : IProductVersionManagementRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlProductVersionManagementRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ===================== Alterações (ProductVersionChange) =====================

    public async Task<IReadOnlyList<ProductVersionChange>> GetChangesByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, product_version_id AS ProductVersionId, change_type AS ChangeType,
                   title, description, component_id AS ComponentId, error_code AS ErrorCode,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_version_changes
            WHERE product_version_id = @VersionId
            ORDER BY id ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionChange>(sql, new { VersionId = productVersionId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<ProductVersionChange>> GetFixChangesForProductAsync(long productId, int minReleaseOrderExclusive, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT pvc.id, pvc.product_version_id AS ProductVersionId, pvc.change_type AS ChangeType,
                   pvc.title, pvc.description, pvc.component_id AS ComponentId, pvc.error_code AS ErrorCode,
                   pvc.created_at AS CreatedAt, pvc.created_by AS CreatedBy,
                   pvc.updated_at AS UpdatedAt, pvc.updated_by AS UpdatedBy
            FROM product_version_changes pvc
            INNER JOIN product_versions pv ON pv.id = pvc.product_version_id
            WHERE pv.product_id = @ProductId
              AND pv.release_order > @MinReleaseOrder
              AND pvc.change_type = 'Fix'
              AND pv.status = 'Active'
            ORDER BY pv.release_order ASC, pvc.id ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionChange>(sql, new { ProductId = productId, MinReleaseOrder = minReleaseOrderExclusive });
        return list.ToList();
    }

    public async Task<ProductVersionChange?> GetChangeByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, product_version_id AS ProductVersionId, change_type AS ChangeType,
                   title, description, component_id AS ComponentId, error_code AS ErrorCode,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_version_changes
            WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductVersionChange>(sql, new { Id = id });
    }

    public async Task<long> AddChangeAsync(ProductVersionChange change, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_version_changes
                (product_version_id, change_type, title, description, component_id, error_code, created_at, created_by)
            VALUES (@ProductVersionId, @ChangeType, @Title, @Description, @ComponentId, @ErrorCode, @CreatedAt, @CreatedBy);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, change);
        change.Id = id;
        return id;
    }

    public async Task UpdateChangeAsync(ProductVersionChange change, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE product_version_changes
            SET change_type = @ChangeType,
                title = @Title,
                description = @Description,
                component_id = @ComponentId,
                error_code = @ErrorCode,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, change);
    }

    public async Task<bool> DeleteChangeAsync(long id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM product_version_changes WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    // ============= Associação alteração <-> caso (ProductVersionChangeCase) =============

    public async Task<IReadOnlyList<ProductVersionChangeCase>> GetLinkedCasesByChangeIdAsync(long changeId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT product_version_change_id AS ProductVersionChangeId, case_id AS CaseId,
                   relation_type AS RelationType, match_score AS MatchScore,
                   matched_factors_json AS MatchedFactorsJson, linked_by AS LinkedBy, linked_at AS LinkedAt
            FROM product_version_change_cases
            WHERE product_version_change_id = @ChangeId
            ORDER BY linked_at ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionChangeCase>(sql, new { ChangeId = changeId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<ProductVersionChangeCase>> GetChangesLinkedToCaseAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT product_version_change_id AS ProductVersionChangeId, case_id AS CaseId,
                   relation_type AS RelationType, match_score AS MatchScore,
                   matched_factors_json AS MatchedFactorsJson, linked_by AS LinkedBy, linked_at AS LinkedAt
            FROM product_version_change_cases
            WHERE case_id = @CaseId
            ORDER BY linked_at ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionChangeCase>(sql, new { CaseId = caseId });
        return list.ToList();
    }

    public async Task<bool> ChangeCaseLinkExistsAsync(long changeId, long caseId, string relationType, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM product_version_change_cases
            WHERE product_version_change_id = @ChangeId AND case_id = @CaseId AND relation_type = @RelationType;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new { ChangeId = changeId, CaseId = caseId, RelationType = relationType });
        return count > 0;
    }

    public async Task AddChangeCaseLinkAsync(ProductVersionChangeCase link, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_version_change_cases
                (product_version_change_id, case_id, relation_type, match_score, matched_factors_json, linked_by, linked_at)
            VALUES (@ProductVersionChangeId, @CaseId, @RelationType, @MatchScore, @MatchedFactorsJson, @LinkedBy, @LinkedAt);";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, link);
    }

    public async Task<bool> DeleteChangeCaseLinkAsync(long changeId, long caseId, string relationType, CancellationToken ct = default)
    {
        const string sql = @"
            DELETE FROM product_version_change_cases
            WHERE product_version_change_id = @ChangeId AND case_id = @CaseId AND relation_type = @RelationType;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(sql, new { ChangeId = changeId, CaseId = caseId, RelationType = relationType });
        return rows > 0;
    }

    // ===================== Destinação / rollout (ProductVersionAssignment) =====================

    public async Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, product_version_id AS ProductVersionId, client_id AS ClientId,
                   client_unit_id AS ClientUnitId, status, planned_at AS PlannedAt,
                   scheduled_at AS ScheduledAt, deployed_at AS DeployedAt, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_version_assignments
            WHERE product_version_id = @VersionId
            ORDER BY id ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionAssignment>(sql, new { VersionId = productVersionId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<ProductVersionAssignment>> GetAssignmentsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, product_version_id AS ProductVersionId, client_id AS ClientId,
                   client_unit_id AS ClientUnitId, status, planned_at AS PlannedAt,
                   scheduled_at AS ScheduledAt, deployed_at AS DeployedAt, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_version_assignments
            WHERE client_id = @ClientId
            ORDER BY id ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersionAssignment>(sql, new { ClientId = clientId });
        return list.ToList();
    }

    public async Task<ProductVersionAssignment?> GetAssignmentByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, product_version_id AS ProductVersionId, client_id AS ClientId,
                   client_unit_id AS ClientUnitId, status, planned_at AS PlannedAt,
                   scheduled_at AS ScheduledAt, deployed_at AS DeployedAt, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_version_assignments
            WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductVersionAssignment>(sql, new { Id = id });
    }

    public async Task<long> AddAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_version_assignments
                (product_version_id, client_id, client_unit_id, status, planned_at, scheduled_at, deployed_at, notes, created_at, created_by)
            VALUES (@ProductVersionId, @ClientId, @ClientUnitId, @Status, @PlannedAt, @ScheduledAt, @DeployedAt, @Notes, @CreatedAt, @CreatedBy);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, assignment);
        assignment.Id = id;
        return id;
    }

    public async Task UpdateAssignmentAsync(ProductVersionAssignment assignment, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE product_version_assignments
            SET status = @Status,
                scheduled_at = @ScheduledAt,
                deployed_at = @DeployedAt,
                notes = @Notes,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, assignment);
    }

    public async Task<bool> DeleteAssignmentAsync(long id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM product_version_assignments WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> AssignmentExistsAsync(long productVersionId, long clientId, long? clientUnitId, CancellationToken ct = default)
    {
        // MySQL trata NULL != NULL em igualdade, então COALESCE normaliza a unidade nula
        // ("cliente inteiro") para permitir a comparação de duplicidade.
        const string sql = @"
            SELECT COUNT(1)
            FROM product_version_assignments
            WHERE product_version_id = @VersionId
              AND client_id = @ClientId
              AND COALESCE(client_unit_id, 0) = COALESCE(@ClientUnitId, 0);";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new { VersionId = productVersionId, ClientId = clientId, ClientUnitId = clientUnitId });
        return count > 0;
    }

    public async Task<IReadOnlyDictionary<long, int>> GetCaseCountsByVersionIdsAsync(IEnumerable<long> versionIds, CancellationToken ct = default)
    {
        var idList = versionIds.Distinct().ToList();
        var result = idList.ToDictionary(id => id, _ => 0);
        if (idList.Count == 0) return result;

        const string sql = @"
            SELECT product_version_id AS VersionId, COUNT(1) AS TotalCount
            FROM cases
            WHERE product_version_id IN @Ids
            GROUP BY product_version_id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<(long VersionId, int TotalCount)>(sql, new { Ids = idList });
        foreach (var r in rows)
        {
            result[r.VersionId] = r.TotalCount;
        }
        return result;
    }

    public async Task<IReadOnlyList<VersionLinkedCaseDetailDb>> GetLinkedCaseDetailsByVersionIdAsync(long productVersionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT pvc.id AS ChangeId,
                   pvc.title AS ChangeTitle,
                   pvc.change_type AS ChangeType,
                   c.id AS CaseId,
                   c.case_number AS CaseNumber,
                   COALESCE(c.normalized_summary, c.original_report) AS CaseTitle,
                   c.status AS CaseStatus,
                   c.client_id AS ClientId,
                   cli.name AS ClientName,
                   c.product_version_id AS OccurredInVersionId,
                   pv_occ.version_label AS OccurredInVersionLabel,
                   pvcc.relation_type AS RelationType,
                   pvcc.linked_at AS LinkedAt,
                   c.opened_at AS CaseOpenedAt
            FROM product_version_changes pvc
            JOIN product_version_change_cases pvcc ON pvcc.product_version_change_id = pvc.id
            JOIN cases c ON c.id = pvcc.case_id
            LEFT JOIN clients cli ON cli.id = c.client_id
            LEFT JOIN product_versions pv_occ ON pv_occ.id = c.product_version_id
            WHERE pvc.product_version_id = @VersionId
            ORDER BY pvcc.linked_at DESC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<VersionLinkedCaseDetailDb>(sql, new { VersionId = productVersionId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ClientVersionCaseDb>> GetCasesByClientAndPeriodAsync(long clientId, long productId, DateTime from, DateTime? to, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS CaseId,
                   case_number AS CaseNumber,
                   COALESCE(normalized_summary, original_report) AS Title,
                   status AS Status,
                   error_code AS ErrorCode,
                   opened_at AS OpenedAt
            FROM cases
            WHERE client_id = @ClientId
              AND product_id = @ProductId
              AND opened_at >= @From
              AND (@To IS NULL OR opened_at < @To)
            ORDER BY opened_at DESC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ClientVersionCaseDb>(sql, new { ClientId = clientId, ProductId = productId, From = from, To = to });
        return rows.ToList();
    }

    public async Task<int> GetPostReleaseSimilarCasesCountAsync(long productId, long productVersionId, string? errorCode, long? componentId, DateTime releaseDate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(errorCode) && !componentId.HasValue)
            return 0;

        const string sql = @"
            SELECT COUNT(DISTINCT c.id)
            FROM cases c
            LEFT JOIN case_components cc ON cc.case_id = c.id
            WHERE c.product_id = @ProductId
              AND c.opened_at >= @ReleaseDate
              AND (
                  (@ErrorCode IS NOT NULL AND c.error_code = @ErrorCode)
                  OR
                  (@ComponentId IS NOT NULL AND cc.component_id = @ComponentId)
              );";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            ProductId = productId,
            ReleaseDate = releaseDate,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode,
            ComponentId = componentId
        });
    }
}