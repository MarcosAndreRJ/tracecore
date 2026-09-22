using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlKnowledgeRepository : IKnowledgeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlKnowledgeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<KnowledgeItem?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_code AS KnowledgeCode,
                knowledge_type AS KnowledgeType,
                title AS Title,
                summary AS Summary,
                status AS Status,
                confidentiality AS Confidentiality,
                owner_user_id AS OwnerUserId,
                owner_department_id AS OwnerDepartmentId,
                current_version_no AS CurrentVersionNo,
                provenance_type AS ProvenanceType,
                provenance_case_id AS ProvenanceCaseId,
                provenance_reference AS ProvenanceReference,
                source_case_iteration_id AS SourceCaseIterationId,
                review_due_at AS ReviewDueAt,
                last_reviewed_at AS LastReviewedAt,
                published_at AS PublishedAt,
                deprecated_at AS DeprecatedAt,
                replacement_knowledge_id AS ReplacementKnowledgeId,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                updated_at AS UpdatedAt
            FROM knowledge_items
            WHERE id = @Id
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<KnowledgeItem>(sql, new { Id = id });
    }

    public async Task<KnowledgeItem?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_code AS KnowledgeCode,
                knowledge_type AS KnowledgeType,
                title AS Title,
                summary AS Summary,
                status AS Status,
                confidentiality AS Confidentiality,
                owner_user_id AS OwnerUserId,
                owner_department_id AS OwnerDepartmentId,
                current_version_no AS CurrentVersionNo,
                provenance_type AS ProvenanceType,
                provenance_case_id AS ProvenanceCaseId,
                provenance_reference AS ProvenanceReference,
                source_case_iteration_id AS SourceCaseIterationId,
                review_due_at AS ReviewDueAt,
                last_reviewed_at AS LastReviewedAt,
                published_at AS PublishedAt,
                deprecated_at AS DeprecatedAt,
                replacement_knowledge_id AS ReplacementKnowledgeId,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                updated_at AS UpdatedAt
            FROM knowledge_items
            WHERE knowledge_code = @Code
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<KnowledgeItem>(sql, new { Code = code });
    }

    public async Task<IReadOnlyList<KnowledgeItem>> GetByProvenanceCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_code AS KnowledgeCode,
                knowledge_type AS KnowledgeType,
                title AS Title,
                summary AS Summary,
                status AS Status,
                confidentiality AS Confidentiality,
                owner_user_id AS OwnerUserId,
                owner_department_id AS OwnerDepartmentId,
                current_version_no AS CurrentVersionNo,
                provenance_type AS ProvenanceType,
                provenance_case_id AS ProvenanceCaseId,
                provenance_reference AS ProvenanceReference,
                source_case_iteration_id AS SourceCaseIterationId,
                review_due_at AS ReviewDueAt,
                last_reviewed_at AS LastReviewedAt,
                published_at AS PublishedAt,
                deprecated_at AS DeprecatedAt,
                replacement_knowledge_id AS ReplacementKnowledgeId,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                updated_at AS UpdatedAt
            FROM knowledge_items
            WHERE provenance_case_id = @CaseId
            ORDER BY created_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var items = await conn.QueryAsync<KnowledgeItem>(sql, new { CaseId = caseId });
        return items.ToList();
    }

    public async Task<long> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_items (
                knowledge_code, knowledge_type, title, summary, status, confidentiality,
                owner_user_id, owner_department_id, current_version_no,
                provenance_type, provenance_case_id, provenance_reference, source_case_iteration_id,
                review_due_at, last_reviewed_at, published_at, deprecated_at,
                replacement_knowledge_id, created_at, created_by, updated_at
            ) VALUES (
                @KnowledgeCode, @KnowledgeType, @Title, @Summary, @Status, @Confidentiality,
                @OwnerUserId, @OwnerDepartmentId, @CurrentVersionNo,
                @ProvenanceType, @ProvenanceCaseId, @ProvenanceReference, @SourceCaseIterationId,
                @ReviewDueAt, @LastReviewedAt, @PublishedAt, @DeprecatedAt,
                @ReplacementKnowledgeId, @CreatedAt, @CreatedBy, @UpdatedAt
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, item);
        item.Id = id;
        return id;
    }

    public async Task UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE knowledge_items SET
                title = @Title,
                summary = @Summary,
                status = @Status,
                confidentiality = @Confidentiality,
                owner_user_id = @OwnerUserId,
                owner_department_id = @OwnerDepartmentId,
                current_version_no = @CurrentVersionNo,
                source_case_iteration_id = @SourceCaseIterationId,
                review_due_at = @ReviewDueAt,
                last_reviewed_at = @LastReviewedAt,
                published_at = @PublishedAt,
                deprecated_at = @DeprecatedAt,
                replacement_knowledge_id = @ReplacementKnowledgeId,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, item);
    }

    public async Task<IReadOnlyList<KnowledgeItem>> SearchAsync(
        string? search = null,
        long? productId = null,
        string? status = null,
        string? provenance = null,
        string? category = null,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT DISTINCT
                ki.id AS Id,
                ki.knowledge_code AS KnowledgeCode,
                ki.knowledge_type AS KnowledgeType,
                ki.title AS Title,
                ki.summary AS Summary,
                ki.status AS Status,
                ki.confidentiality AS Confidentiality,
                ki.owner_user_id AS OwnerUserId,
                ki.owner_department_id AS OwnerDepartmentId,
                ki.current_version_no AS CurrentVersionNo,
                ki.provenance_type AS ProvenanceType,
                ki.provenance_case_id AS ProvenanceCaseId,
                ki.provenance_reference AS ProvenanceReference,
                ki.review_due_at AS ReviewDueAt,
                ki.last_reviewed_at AS LastReviewedAt,
                ki.published_at AS PublishedAt,
                ki.deprecated_at AS DeprecatedAt,
                ki.replacement_knowledge_id AS ReplacementKnowledgeId,
                ki.created_at AS CreatedAt,
                ki.created_by AS CreatedBy,
                ki.updated_at AS UpdatedAt
            FROM knowledge_items ki
            LEFT JOIN knowledge_applicability ka ON ki.id = ka.knowledge_item_id
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " AND (ki.title LIKE @Search OR ki.summary LIKE @Search OR ki.knowledge_code LIKE @Search)";
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        if (productId.HasValue && productId.Value > 0)
        {
            sql += " AND ka.product_id = @ProductId";
            parameters.Add("ProductId", productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            sql += " AND ki.status = @Status";
            parameters.Add("Status", status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(provenance) && !provenance.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            sql += " AND LOWER(ki.provenance_type) = @Provenance";
            parameters.Add("Provenance", provenance.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            sql += " AND LOWER(ki.knowledge_type) = @Category";
            parameters.Add("Category", category.Trim().ToLowerInvariant());
        }

        sql += " ORDER BY ki.updated_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeItem>(sql, parameters);
        return rows.ToList();
    }

    public async Task<KnowledgeVersion?> GetVersionByIdAsync(long versionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_item_id AS KnowledgeItemId,
                version_no AS VersionNo,
                content_markdown AS ContentMarkdown,
                problem_description AS ProblemDescription,
                root_cause_summary AS RootCauseSummary,
                validation_method AS ValidationMethod,
                risk_warning AS RiskWarning,
                rollback_plan AS RollbackPlan,
                change_summary AS ChangeSummary,
                status AS Status,
                content_hash AS ContentHash,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                approved_at AS ApprovedAt,
                approved_by AS ApprovedBy
            FROM knowledge_versions
            WHERE id = @VersionId
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<KnowledgeVersion>(sql, new { VersionId = versionId });
    }

    public async Task<KnowledgeVersion?> GetVersionByItemAndNumberAsync(long itemId, int versionNo, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_item_id AS KnowledgeItemId,
                version_no AS VersionNo,
                content_markdown AS ContentMarkdown,
                problem_description AS ProblemDescription,
                root_cause_summary AS RootCauseSummary,
                validation_method AS ValidationMethod,
                risk_warning AS RiskWarning,
                rollback_plan AS RollbackPlan,
                change_summary AS ChangeSummary,
                status AS Status,
                content_hash AS ContentHash,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                approved_at AS ApprovedAt,
                approved_by AS ApprovedBy
            FROM knowledge_versions
            WHERE knowledge_item_id = @ItemId AND version_no = @VersionNo
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<KnowledgeVersion>(sql, new { ItemId = itemId, VersionNo = versionNo });
    }

    public async Task<IReadOnlyList<KnowledgeVersion>> GetVersionsByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_item_id AS KnowledgeItemId,
                version_no AS VersionNo,
                content_markdown AS ContentMarkdown,
                problem_description AS ProblemDescription,
                root_cause_summary AS RootCauseSummary,
                validation_method AS ValidationMethod,
                risk_warning AS RiskWarning,
                rollback_plan AS RollbackPlan,
                change_summary AS ChangeSummary,
                status AS Status,
                content_hash AS ContentHash,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                approved_at AS ApprovedAt,
                approved_by AS ApprovedBy
            FROM knowledge_versions
            WHERE knowledge_item_id = @ItemId
            ORDER BY version_no DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeVersion>(sql, new { ItemId = itemId });
        return rows.ToList();
    }

    public async Task<long> CreateVersionAsync(KnowledgeVersion version, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_versions (
                knowledge_item_id, version_no, content_markdown, problem_description,
                root_cause_summary, validation_method, risk_warning, rollback_plan,
                change_summary, status, content_hash, created_at, created_by,
                approved_at, approved_by
            ) VALUES (
                @KnowledgeItemId, @VersionNo, @ContentMarkdown, @ProblemDescription,
                @RootCauseSummary, @ValidationMethod, @RiskWarning, @RollbackPlan,
                @ChangeSummary, @Status, @ContentHash, @CreatedAt, @CreatedBy,
                @ApprovedAt, @ApprovedBy
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, version);
        version.Id = id;
        return id;
    }

    public async Task UpdateVersionAsync(KnowledgeVersion version, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE knowledge_versions SET
                content_markdown = @ContentMarkdown,
                problem_description = @ProblemDescription,
                root_cause_summary = @RootCauseSummary,
                validation_method = @ValidationMethod,
                risk_warning = @RiskWarning,
                rollback_plan = @RollbackPlan,
                change_summary = @ChangeSummary,
                status = @Status,
                content_hash = @ContentHash,
                approved_at = @ApprovedAt,
                approved_by = @ApprovedBy
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, version);
    }

    public async Task<IReadOnlyList<KnowledgeApplicability>> GetApplicabilitiesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_item_id AS KnowledgeItemId,
                product_id AS ProductId,
                product_version_id AS ProductVersionId,
                component_id AS ComponentId,
                environment_id AS EnvironmentId,
                applicability_type AS ApplicabilityType,
                notes AS Notes
            FROM knowledge_applicability
            WHERE knowledge_item_id = @ItemId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeApplicability>(sql, new { ItemId = itemId });
        return rows.ToList();
    }

    public async Task<long> AddApplicabilityAsync(KnowledgeApplicability applicability, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_applicability (
                knowledge_item_id, product_id, product_version_id, component_id,
                environment_id, applicability_type, notes
            ) VALUES (
                @KnowledgeItemId, @ProductId, @ProductVersionId, @ComponentId,
                @EnvironmentId, @ApplicabilityType, @Notes
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, applicability);
        applicability.Id = id;
        return id;
    }

    public async Task RemoveApplicabilityAsync(long applicabilityId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM knowledge_applicability WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = applicabilityId });
    }

    public async Task<IReadOnlyList<KnowledgeStep>> GetStepsByVersionIdAsync(long versionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_version_id AS KnowledgeVersionId,
                sequence_no AS SequenceNo,
                step_type AS StepType,
                title AS Title,
                description AS Description,
                command AS Command,
                expected_output AS ExpectedOutput
            FROM knowledge_steps
            WHERE knowledge_version_id = @VersionId
            ORDER BY sequence_no ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeStep>(sql, new { VersionId = versionId });
        return rows.ToList();
    }

    public async Task AddStepsAsync(IEnumerable<KnowledgeStep> steps, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_steps (
                knowledge_version_id, sequence_no, step_type, title, description,
                command, expected_output
            ) VALUES (
                @KnowledgeVersionId, @SequenceNo, @StepType, @Title, @Description,
                @Command, @ExpectedOutput
            );";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, steps);
    }

    public async Task<IReadOnlyList<KnowledgeSymptom>> GetSymptomsByVersionIdAsync(long versionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_version_id AS KnowledgeVersionId,
                symptom_text AS SymptomText
            FROM knowledge_symptoms
            WHERE knowledge_version_id = @VersionId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeSymptom>(sql, new { VersionId = versionId });
        return rows.ToList();
    }

    public async Task AddSymptomsAsync(IEnumerable<KnowledgeSymptom> symptoms, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_symptoms (knowledge_version_id, symptom_text)
            VALUES (@KnowledgeVersionId, @SymptomText);";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, symptoms);
    }

    public async Task<IReadOnlyList<string>> GetTechnologiesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT t.name
            FROM technologies t
            INNER JOIN knowledge_technologies kt ON t.id = kt.technology_id
            WHERE kt.knowledge_item_id = @ItemId
            ORDER BY t.name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<string>(sql, new { ItemId = itemId });
        return rows.ToList();
    }

    public async Task SetTechnologiesAsync(long itemId, IEnumerable<string> technologyNames, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM knowledge_technologies WHERE knowledge_item_id = @ItemId;", new { ItemId = itemId });

        foreach (var tech in technologyNames.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()))
        {
            var techId = await conn.ExecuteScalarAsync<long?>(
                "SELECT id FROM technologies WHERE name = @Name LIMIT 1;", new { Name = tech });

            if (!techId.HasValue)
            {
                techId = await conn.ExecuteScalarAsync<long>(
                    "INSERT INTO technologies (name) VALUES (@Name); SELECT LAST_INSERT_ID();", new { Name = tech });
            }

            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO knowledge_technologies (knowledge_item_id, technology_id)
                VALUES (@ItemId, @TechId);", new { ItemId = itemId, TechId = techId.Value });
        }
    }

    public async Task<IReadOnlyList<string>> GetTagsByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT t.name
            FROM tags t
            INNER JOIN knowledge_tags kt ON t.id = kt.tag_id
            WHERE kt.knowledge_item_id = @ItemId
            ORDER BY t.name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<string>(sql, new { ItemId = itemId });
        return rows.ToList();
    }

    public async Task SetTagsAsync(long itemId, IEnumerable<string> tagNames, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM knowledge_tags WHERE knowledge_item_id = @ItemId;", new { ItemId = itemId });

        foreach (var tag in tagNames.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()))
        {
            var tagId = await conn.ExecuteScalarAsync<long?>(
                "SELECT id FROM tags WHERE name = @Name LIMIT 1;", new { Name = tag });

            if (!tagId.HasValue)
            {
                tagId = await conn.ExecuteScalarAsync<long>(
                    "INSERT INTO tags (name) VALUES (@Name); SELECT LAST_INSERT_ID();", new { Name = tag });
            }

            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO knowledge_tags (knowledge_item_id, tag_id)
                VALUES (@ItemId, @TagId);", new { ItemId = itemId, TagId = tagId.Value });
        }
    }

    public async Task<long> RecordUsageAsync(KnowledgeUsage usage, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO knowledge_usages (
                knowledge_item_id, knowledge_version_id, case_id, used_by, used_at,
                outcome, notes, context_match_json
            ) VALUES (
                @KnowledgeItemId, @KnowledgeVersionId, @CaseId, @UsedBy, @UsedAt,
                @Outcome, @Notes, @ContextMatchJson
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, usage);
        usage.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<KnowledgeUsage>> GetUsagesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                knowledge_item_id AS KnowledgeItemId,
                knowledge_version_id AS KnowledgeVersionId,
                case_id AS CaseId,
                used_by AS UsedBy,
                used_at AS UsedAt,
                outcome AS Outcome,
                notes AS Notes,
                context_match_json AS ContextMatchJson
            FROM knowledge_usages
            WHERE knowledge_item_id = @ItemId
            ORDER BY used_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<KnowledgeUsage>(sql, new { ItemId = itemId });
        return rows.ToList();
    }

    public async Task<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount)> GetDashboardCountsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                COUNT(*) AS TotalCount,
                SUM(CASE WHEN status = 'Published' THEN 1 ELSE 0 END) AS PublishedCount,
                SUM(CASE WHEN status = 'Review' THEN 1 ELSE 0 END) AS InReviewCount,
                SUM(CASE WHEN review_due_at IS NOT NULL AND review_due_at < NOW() THEN 1 ELSE 0 END) AS StaleCount
            FROM knowledge_items;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var res = await conn.QueryFirstOrDefaultAsync<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount)>(sql);
        return res;
    }
}
