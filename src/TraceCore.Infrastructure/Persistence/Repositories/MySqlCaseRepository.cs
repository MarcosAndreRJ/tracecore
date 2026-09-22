using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlCaseRepository : ICaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ulong> NextCaseNumberAsync(CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_number_seq VALUES (NULL);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var number = await conn.ExecuteScalarAsync<ulong>(sql);
        return number;
    }

    public async Task<long> AddAsync(Case @case, CancellationToken ct = default)
    {
        const string insertCaseSql = @"
            INSERT INTO cases (
                case_number, external_reference, source_type, client_id, product_id,
                product_version_id, environment_id, original_report, normalized_summary,
                expected_behavior, observed_behavior, error_code, error_message,
                scope_type, severity, impact_level, status, current_owner_user_id,
                current_department_id, root_cause_status, opened_at, first_response_at,
                resolved_at, closed_at, created_at, created_by, updated_at, updated_by, row_version
            ) VALUES (
                @CaseNumber, @ExternalReference, @SourceType, @ClientId, @ProductId,
                @ProductVersionId, @EnvironmentId, @OriginalReport, @NormalizedSummary,
                @ExpectedBehavior, @ObservedBehavior, @ErrorCode, @ErrorMessage,
                @ScopeType, @Severity, @ImpactLevel, @Status, @CurrentOwnerUserId,
                @CurrentDepartmentId, @RootCauseStatus, @OpenedAt, @FirstResponseAt,
                @ResolvedAt, @ClosedAt, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @RowVersion
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var caseId = await conn.ExecuteScalarAsync<long>(insertCaseSql, new
        {
            CaseNumber = (long)@case.CaseNumber,
            @case.ExternalReference,
            @case.SourceType,
            @case.ClientId,
            @case.ProductId,
            @case.ProductVersionId,
            @case.EnvironmentId,
            @case.OriginalReport,
            @case.NormalizedSummary,
            @case.ExpectedBehavior,
            @case.ObservedBehavior,
            @case.ErrorCode,
            @case.ErrorMessage,
            @case.ScopeType,
            @case.Severity,
            @case.ImpactLevel,
            @case.Status,
            @case.CurrentOwnerUserId,
            @case.CurrentDepartmentId,
            @case.RootCauseStatus,
            @case.OpenedAt,
            @case.FirstResponseAt,
            @case.ResolvedAt,
            @case.ClosedAt,
            @case.CreatedAt,
            @case.CreatedBy,
            @case.UpdatedAt,
            @case.UpdatedBy,
            @case.RowVersion
        });

        @case.Id = caseId;

        // Persiste sintomas
        if (@case.Symptoms.Count > 0)
        {
            const string insertSymptomSql = @"
                INSERT INTO case_symptoms (case_id, symptom_code, symptom_text, source, confirmed)
                VALUES (@CaseId, @SymptomCode, @SymptomText, @Source, @Confirmed);
                SELECT LAST_INSERT_ID();";

            foreach (var symptom in @case.Symptoms)
            {
                symptom.CaseId = caseId;
                symptom.Id = await conn.ExecuteScalarAsync<long>(insertSymptomSql, symptom);
            }
        }

        // Persiste componentes associados (BR-023: múltiplos componentes)
        if (@case.AffectedComponents.Count > 0)
        {
            const string insertCompSql = @"
                INSERT INTO case_components (case_id, component_id, relation_type, confidence_label)
                VALUES (@CaseId, @ComponentId, @RelationType, @ConfidenceLabel);";

            foreach (var comp in @case.AffectedComponents)
            {
                comp.CaseId = caseId;
                await conn.ExecuteAsync(insertCompSql, comp);
            }
        }

        // Bloco 7.A.3: Persiste Iteração 1 na criação do caso
        const string insertIterSql = @"
            INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, status)
            VALUES (@CaseId, @SequenceNumber, @OpenedAt, @OpenedBy, @Reason, @Status);
            SELECT LAST_INSERT_ID();";

        var iter = @case.Iterations.FirstOrDefault() ?? new CaseIteration(caseId, 1, @case.CreatedBy ?? 1, "Abertura inicial do caso", @case.OpenedAt, "Open");
        iter.CaseId = caseId;
        iter.Id = await conn.ExecuteScalarAsync<long>(insertIterSql, iter);
        @case.Iterations = new List<CaseIteration> { iter };

        // Persiste evidências
        if (@case.Evidences.Count > 0)
        {
            const string insertEvidenceSql = @"
                INSERT INTO case_evidences (case_id, case_iteration_id, evidence_type, description, attachment_id, created_by, created_at)
                VALUES (@CaseId, @CaseIterationId, @EvidenceType, @Description, @AttachmentId, @CreatedBy, @CreatedAt);
                SELECT LAST_INSERT_ID();";

            foreach (var evidence in @case.Evidences)
            {
                evidence.CaseId = caseId;
                evidence.CaseIterationId = iter.Id;
                evidence.Id = await conn.ExecuteScalarAsync<long>(insertEvidenceSql, evidence);
            }
        }

        return caseId;
    }

    public async Task<Case?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string caseSql = @"
            SELECT 
                id AS Id, case_number AS CaseNumber, external_reference AS ExternalReference,
                source_type AS SourceType, client_id AS ClientId, product_id AS ProductId,
                product_version_id AS ProductVersionId, environment_id AS EnvironmentId,
                original_report AS OriginalReport, normalized_summary AS NormalizedSummary,
                expected_behavior AS ExpectedBehavior, observed_behavior AS ObservedBehavior,
                error_code AS ErrorCode, error_message AS ErrorMessage, scope_type AS ScopeType,
                severity AS Severity, impact_level AS ImpactLevel, status AS Status,
                current_owner_user_id AS CurrentOwnerUserId, current_department_id AS CurrentDepartmentId,
                root_cause_status AS RootCauseStatus, opened_at AS OpenedAt,
                first_response_at AS FirstResponseAt, resolved_at AS ResolvedAt, closed_at AS ClosedAt,
                created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt,
                updated_by AS UpdatedBy, row_version AS RowVersion
            FROM cases
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var @case = await conn.QuerySingleOrDefaultAsync<Case>(caseSql, new { Id = id });
        if (@case == null) return null;

        await LoadCaseAssociationsAsync(conn, @case);
        return @case;
    }

    public async Task<Case?> GetByCaseNumberAsync(ulong caseNumber, CancellationToken ct = default)
    {
        const string caseSql = @"
            SELECT 
                id AS Id, case_number AS CaseNumber, external_reference AS ExternalReference,
                source_type AS SourceType, client_id AS ClientId, product_id AS ProductId,
                product_version_id AS ProductVersionId, environment_id AS EnvironmentId,
                original_report AS OriginalReport, normalized_summary AS NormalizedSummary,
                expected_behavior AS ExpectedBehavior, observed_behavior AS ObservedBehavior,
                error_code AS ErrorCode, error_message AS ErrorMessage, scope_type AS ScopeType,
                severity AS Severity, impact_level AS ImpactLevel, status AS Status,
                current_owner_user_id AS CurrentOwnerUserId, current_department_id AS CurrentDepartmentId,
                root_cause_status AS RootCauseStatus, opened_at AS OpenedAt,
                first_response_at AS FirstResponseAt, resolved_at AS ResolvedAt, closed_at AS ClosedAt,
                created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt,
                updated_by AS UpdatedBy, row_version AS RowVersion
            FROM cases
            WHERE case_number = @CaseNumber;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var @case = await conn.QuerySingleOrDefaultAsync<Case>(caseSql, new { CaseNumber = (long)caseNumber });
        if (@case == null) return null;

        await LoadCaseAssociationsAsync(conn, @case);
        return @case;
    }

    public async Task<IReadOnlyList<Case>> GetAllAsync(int limit = 50, CancellationToken ct = default)
    {
        var sql = $@"
            SELECT 
                id AS Id, case_number AS CaseNumber, external_reference AS ExternalReference,
                source_type AS SourceType, client_id AS ClientId, product_id AS ProductId,
                product_version_id AS ProductVersionId, environment_id AS EnvironmentId,
                original_report AS OriginalReport, normalized_summary AS NormalizedSummary,
                expected_behavior AS ExpectedBehavior, observed_behavior AS ObservedBehavior,
                error_code AS ErrorCode, error_message AS ErrorMessage, scope_type AS ScopeType,
                severity AS Severity, impact_level AS ImpactLevel, status AS Status,
                current_owner_user_id AS CurrentOwnerUserId, current_department_id AS CurrentDepartmentId,
                root_cause_status AS RootCauseStatus, opened_at AS OpenedAt,
                first_response_at AS FirstResponseAt, resolved_at AS ResolvedAt, closed_at AS ClosedAt,
                created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt,
                updated_by AS UpdatedBy, row_version AS RowVersion
            FROM cases
            ORDER BY opened_at DESC
            LIMIT {limit};";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = (await conn.QueryAsync<Case>(sql)).ToList();
        foreach (var c in list)
        {
            await LoadCaseAssociationsAsync(conn, c);
        }
        return list;
    }

    public async Task UpdateNormalizedSummaryAsync(long caseId, string? normalizedSummary, long? updatedBy, CancellationToken ct = default)
    {
        // BR-020 e BR-021: Atualiza apenas o resumo normalizado, sem tocar no relato original
        const string sql = @"
            UPDATE cases
            SET normalized_summary = @NormalizedSummary,
                updated_at = NOW(),
                updated_by = @UpdatedBy,
                row_version = row_version + 1
            WHERE id = @CaseId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { CaseId = caseId, NormalizedSummary = normalizedSummary, UpdatedBy = updatedBy });
    }

    public async Task UpdateCaseResolutionStatusAsync(long caseId, string status, string rootCauseStatus, DateTime resolvedAt, long resolvedBy, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE cases
            SET status = @Status,
                root_cause_status = @RootCauseStatus,
                resolved_at = @ResolvedAt,
                updated_at = @ResolvedAt,
                updated_by = @ResolvedBy,
                row_version = row_version + 1
            WHERE id = @CaseId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            CaseId = caseId,
            Status = status,
            RootCauseStatus = rootCauseStatus,
            ResolvedAt = resolvedAt,
            ResolvedBy = resolvedBy
        });
    }

    public async Task UpdateComponentRelationAsync(long caseId, long componentId, string relationType, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_components (case_id, component_id, relation_type, confidence_label)
            VALUES (@CaseId, @ComponentId, @RelationType, 'ConfirmedRootCause')
            ON DUPLICATE KEY UPDATE relation_type = @RelationType;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            CaseId = caseId,
            ComponentId = componentId,
            RelationType = relationType
        });
    }

    public async Task<long> AddSymptomAsync(CaseSymptom symptom, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_symptoms (case_id, symptom_code, symptom_text, source, confirmed)
            VALUES (@CaseId, @SymptomCode, @SymptomText, @Source, @Confirmed);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, symptom);
        symptom.Id = id;

        await conn.ExecuteAsync(@"
            UPDATE cases SET updated_at = NOW(), row_version = row_version + 1
            WHERE id = @CaseId;", new { symptom.CaseId });

        return id;
    }

    public async Task AddTagAsync(long caseId, string tagName, long? updatedBy, CancellationToken ct = default)
    {
        var normalized = (tagName ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) return;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var tagId = await conn.ExecuteScalarAsync<long?>(
            "SELECT id FROM tags WHERE name = @Name LIMIT 1;", new { Name = normalized });

        if (tagId == null)
        {
            tagId = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO tags (name) VALUES (@Name); SELECT LAST_INSERT_ID();", new { Name = normalized });
        }

        await conn.ExecuteAsync(
            "INSERT IGNORE INTO case_tags (case_id, tag_id) VALUES (@CaseId, @TagId);",
            new { CaseId = caseId, TagId = tagId!.Value });

        await conn.ExecuteAsync(@"
            UPDATE cases
            SET updated_at = NOW(), updated_by = @UpdatedBy, row_version = row_version + 1
            WHERE id = @CaseId;", new { CaseId = caseId, UpdatedBy = updatedBy });
    }

    public async Task RemoveTagAsync(long caseId, string tagName, CancellationToken ct = default)
    {
        var normalized = (tagName ?? string.Empty).Trim().ToLowerInvariant();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(@"
            DELETE ct FROM case_tags ct
            INNER JOIN tags t ON t.id = ct.tag_id
            WHERE ct.case_id = @CaseId AND t.name = @Name;",
            new { CaseId = caseId, Name = normalized });
    }

    private static async Task LoadCaseAssociationsAsync(System.Data.Common.DbConnection conn, Case @case)
    {
        const string symptomsSql = @"
            SELECT id AS Id, case_id AS CaseId, symptom_code AS SymptomCode, symptom_text AS SymptomText, source AS Source, confirmed AS Confirmed
            FROM case_symptoms
            WHERE case_id = @CaseId;";
        @case.Symptoms = (await conn.QueryAsync<CaseSymptom>(symptomsSql, new { CaseId = @case.Id })).ToList();

        const string componentsSql = @"
            SELECT case_id AS CaseId, component_id AS ComponentId, relation_type AS RelationType, confidence_label AS ConfidenceLabel
            FROM case_components
            WHERE case_id = @CaseId;";
        @case.AffectedComponents = (await conn.QueryAsync<CaseComponent>(componentsSql, new { CaseId = @case.Id })).ToList();

        const string evidencesSql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, evidence_type AS EvidenceType, description AS Description, attachment_id AS AttachmentId, created_by AS CreatedBy, created_at AS CreatedAt
            FROM case_evidences
            WHERE case_id = @CaseId;";
        @case.Evidences = (await conn.QueryAsync<CaseEvidence>(evidencesSql, new { CaseId = @case.Id })).ToList();

        const string iterationsSql = @"
            SELECT id AS Id, case_id AS CaseId, sequence_number AS SequenceNumber, opened_at AS OpenedAt,
                   opened_by AS OpenedBy, reason AS Reason, closed_at AS ClosedAt, status AS Status
            FROM case_iterations
            WHERE case_id = @CaseId
            ORDER BY sequence_number ASC;";
        @case.Iterations = (await conn.QueryAsync<CaseIteration>(iterationsSql, new { CaseId = @case.Id })).ToList();

        const string tagsSql = @"
            SELECT t.name
            FROM tags t
            INNER JOIN case_tags ct ON t.id = ct.tag_id
            WHERE ct.case_id = @CaseId
            ORDER BY t.name;";
        @case.Tags = (await conn.QueryAsync<string>(tagsSql, new { CaseId = @case.Id })).ToList();
    }

    public async Task<IReadOnlyList<CaseIteration>> GetIterationsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, sequence_number AS SequenceNumber, opened_at AS OpenedAt,
                   opened_by AS OpenedBy, reason AS Reason, closed_at AS ClosedAt, status AS Status
            FROM case_iterations
            WHERE case_id = @CaseId
            ORDER BY sequence_number ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<CaseIteration>(sql, new { CaseId = caseId });
        return list.ToList();
    }

    public async Task<CaseIteration?> GetCurrentIterationAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, sequence_number AS SequenceNumber, opened_at AS OpenedAt,
                   opened_by AS OpenedBy, reason AS Reason, closed_at AS ClosedAt, status AS Status
            FROM case_iterations
            WHERE case_id = @CaseId
            ORDER BY sequence_number DESC
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CaseIteration>(sql, new { CaseId = caseId });
    }

    public async Task<long> AddIterationAsync(CaseIteration iteration, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
            VALUES (@CaseId, @SequenceNumber, @OpenedAt, @OpenedBy, @Reason, @ClosedAt, @Status);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, iteration);
        iteration.Id = id;
        return id;
    }

    public async Task UpdateIterationStatusAsync(long iterationId, string status, DateTime? closedAt, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE case_iterations
            SET status = @Status,
                closed_at = @ClosedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = iterationId, Status = status, ClosedAt = closedAt });
    }

    public async Task UpdateCaseReopenStatusAsync(long caseId, string status, long reopenedBy, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE cases
            SET status = @Status,
                resolved_at = NULL,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy,
                row_version = row_version + 1
            WHERE id = @CaseId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            CaseId = caseId,
            Status = status,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = reopenedBy
        });
    }
}
