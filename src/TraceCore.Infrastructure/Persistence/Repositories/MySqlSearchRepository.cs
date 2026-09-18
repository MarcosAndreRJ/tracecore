using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Infrastructure.Persistence;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlSearchRepository : ISearchRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlSearchRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateSessionAsync(SearchSession session, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO search_sessions (user_id, started_at, context_json)
            VALUES (@UserId, @StartedAt, @ContextJson);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, session);
    }

    public async Task<long> RecordQueryAsync(SearchQueryRecord queryRecord, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO search_queries (search_session_id, query_text, filters_json, result_count, duration_ms, executed_at)
            VALUES (@SearchSessionId, @QueryText, @FiltersJson, @ResultCount, @DurationMs, @ExecutedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, queryRecord);
    }

    public async Task<long> RecordInteractionAsync(SearchResultInteraction interaction, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO search_result_interactions (search_query_id, result_type, result_id, position, opened_at, feedback_useful)
            VALUES (@SearchQueryId, @ResultType, @ResultId, @Position, @OpenedAt, @FeedbackUseful);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(sql, interaction);
    }

    public async Task MarkInteractionOpenedAsync(long interactionId, DateTime openedAt, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE search_result_interactions
            SET opened_at = @OpenedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = interactionId, OpenedAt = openedAt });
    }

    public async Task RecordFeedbackAsync(long interactionId, bool useful, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE search_result_interactions
            SET feedback_useful = @Useful
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = interactionId, Useful = useful });
    }

    public async Task<IReadOnlyList<CaseSearchRawResult>> SearchCasesAsync(string query, SearchFilterCriteriaDb filters, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT 
                c.id AS Id,
                CAST(c.case_number AS CHAR) AS CaseNumber,
                c.normalized_summary AS NormalizedSummary,
                c.original_report AS OriginalReport,
                c.status AS Status,
                c.severity AS Severity,
                c.error_code AS ErrorCode,
                c.error_message AS ErrorMessage,
                c.product_id AS ProductId,
                p.name AS ProductName,
                c.product_version_id AS ProductVersionId,
                pv.version_label AS VersionName,
                cc.component_id AS ComponentId,
                cmp.name AS ComponentName,
                c.client_id AS ClientId,
                cl.name AS ClientName,
                c.client_unit_id AS ClientUnitId,
                cu.name AS ClientUnitName,
                c.current_department_id AS DepartmentId,
                d.name AS DepartmentName,
                c.opened_at AS OpenedAt,
                CASE 
                    WHEN @HasQuery = 0 THEN 50.0
                    WHEN c.case_number LIKE @LikeTerm THEN 90.0
                    WHEN c.error_code LIKE @LikeTerm THEN 80.0
                    WHEN c.normalized_summary LIKE @LikeTerm THEN 70.0
                    WHEN c.original_report LIKE @LikeTerm THEN 60.0
                    ELSE 40.0
                END AS TextScore
            FROM cases c
            LEFT JOIN products p ON p.id = c.product_id
            LEFT JOIN product_versions pv ON pv.id = c.product_version_id
            LEFT JOIN case_components cc ON cc.case_id = c.id
            LEFT JOIN components cmp ON cmp.id = cc.component_id
            LEFT JOIN clients cl ON cl.id = c.client_id
            LEFT JOIN client_units cu ON cu.id = c.client_unit_id
            LEFT JOIN departments d ON d.id = c.current_department_id
            WHERE 1=1 ";

        var parameters = new DynamicParameters();
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        parameters.Add("HasQuery", hasQuery ? 1 : 0);
        parameters.Add("LikeTerm", $"%{query}%");

        if (hasQuery)
        {
            sql += @" AND (
                c.case_number LIKE @LikeTerm 
                OR c.normalized_summary LIKE @LikeTerm 
                OR c.original_report LIKE @LikeTerm 
                OR c.error_code LIKE @LikeTerm
                OR c.error_message LIKE @LikeTerm
            )";
        }

        if (filters.ProductId.HasValue)
        {
            sql += " AND c.product_id = @ProductId";
            parameters.Add("ProductId", filters.ProductId.Value);
        }
        if (filters.ProductVersionId.HasValue)
        {
            sql += " AND c.product_version_id = @ProductVersionId";
            parameters.Add("ProductVersionId", filters.ProductVersionId.Value);
        }
        if (filters.ClientId.HasValue)
        {
            sql += " AND c.client_id = @ClientId";
            parameters.Add("ClientId", filters.ClientId.Value);
        }
        if (filters.ClientUnitId.HasValue)
        {
            sql += " AND c.client_unit_id = @ClientUnitId";
            parameters.Add("ClientUnitId", filters.ClientUnitId.Value);
        }
        if (filters.DepartmentId.HasValue)
        {
            sql += " AND c.current_department_id = @DepartmentId";
            parameters.Add("DepartmentId", filters.DepartmentId.Value);
        }
        if (filters.ComponentId.HasValue)
        {
            sql += " AND cc.component_id = @ComponentId";
            parameters.Add("ComponentId", filters.ComponentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.ErrorCode))
        {
            sql += " AND c.error_code = @ErrorCode";
            parameters.Add("ErrorCode", filters.ErrorCode);
        }
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            sql += " AND c.status = @Status";
            parameters.Add("Status", filters.Status);
        }
        if (filters.StartDate.HasValue)
        {
            sql += " AND c.opened_at >= @StartDate";
            parameters.Add("StartDate", filters.StartDate.Value);
        }
        if (filters.EndDate.HasValue)
        {
            sql += " AND c.opened_at <= @EndDate";
            parameters.Add("EndDate", filters.EndDate.Value);
        }

        sql += " ORDER BY TextScore DESC, c.opened_at DESC LIMIT @Limit;";
        parameters.Add("Limit", limit);

        var rows = await conn.QueryAsync<CaseSearchRawResult>(sql, parameters);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SolutionSearchRawResult>> SearchSolutionsAsync(string query, SearchFilterCriteriaDb filters, bool allowDrafts = false, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT 
                ki.id AS Id,
                ki.knowledge_code AS KnowledgeCode,
                ki.title AS Title,
                ki.summary AS Summary,
                ki.status AS Status,
                CONCAT('v', ki.current_version_no, '.0') AS Version,
                kv.problem_description AS ProblemDescription,
                kv.validation_method AS ValidationMethod,
                kv.risk_warning AS RiskWarning,
                kv.rollback_plan AS RollbackPlan,
                kv.content_markdown AS ContentMarkdown,
                ki.owner_department_id AS OwnerDepartmentId,
                ki.created_at AS CreatedAt,
                ki.published_at AS PublishedAt,
                ki.provenance_case_id AS SourceCaseId,
                (SELECT COUNT(1) FROM knowledge_usages ku WHERE ku.knowledge_item_id = ki.id) AS TotalUsages,
                (SELECT COUNT(1) FROM knowledge_usages ku WHERE ku.knowledge_item_id = ki.id AND ku.outcome IN ('Worked', 'PartiallyWorked')) AS SuccessfulUsages,
                CASE 
                    WHEN @HasQuery = 0 THEN 50.0
                    WHEN ki.knowledge_code LIKE @LikeTerm THEN 90.0
                    WHEN ki.title LIKE @LikeTerm THEN 75.0
                    WHEN ki.summary LIKE @LikeTerm THEN 65.0
                    WHEN kv.problem_description LIKE @LikeTerm THEN 55.0
                    ELSE 40.0
                END AS TextScore
            FROM knowledge_items ki
            LEFT JOIN knowledge_versions kv ON kv.knowledge_item_id = ki.id AND kv.version_no = ki.current_version_no
            WHERE 1=1 ";

        var parameters = new DynamicParameters();
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        parameters.Add("HasQuery", hasQuery ? 1 : 0);
        parameters.Add("LikeTerm", $"%{query}%");

        if (!allowDrafts)
        {
            sql += " AND ki.status = 'Published'";
        }
        else if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            sql += " AND ki.status = @Status";
            parameters.Add("Status", filters.Status);
        }
        else
        {
            sql += " AND ki.status != 'Deprecated'"; // Por padrão BR-049 exclui Deprecated das buscas gerais
        }

        if (hasQuery)
        {
            sql += @" AND (
                ki.knowledge_code LIKE @LikeTerm 
                OR ki.title LIKE @LikeTerm 
                OR ki.summary LIKE @LikeTerm 
                OR kv.problem_description LIKE @LikeTerm
                OR kv.content_markdown LIKE @LikeTerm
            )";
        }

        if (filters.DepartmentId.HasValue)
        {
            sql += " AND ki.owner_department_id = @DepartmentId";
            parameters.Add("DepartmentId", filters.DepartmentId.Value);
        }

        if (filters.TechnologyId.HasValue)
        {
            sql += @" AND EXISTS (
                SELECT 1 FROM knowledge_technologies kt
                WHERE kt.knowledge_item_id = ki.id AND kt.technology_id = @TechnologyId
            )";
            parameters.Add("TechnologyId", filters.TechnologyId.Value);
        }

        sql += " ORDER BY TextScore DESC, ki.updated_at DESC LIMIT @Limit;";
        parameters.Add("Limit", limit);

        var rows = (await conn.QueryAsync<dynamic>(sql, parameters)).ToList();
        var results = new List<SolutionSearchRawResult>();

        foreach (var r in rows)
        {
            long itemId = (long)r.Id;

            // Busca aplicabilidades do item
            var appSql = "SELECT product_id, component_id, applicability_type, product_version_id FROM knowledge_applicability WHERE knowledge_item_id = @ItemId;";
            var apps = (await conn.QueryAsync<dynamic>(appSql, new { ItemId = itemId })).ToList();

            var appEnvs = new List<string>();
            var appProds = new List<long>();
            var appComps = new List<long>();
            var negativeVersions = new List<long>();

            foreach (var a in apps)
            {
                if (a.product_id != null) appProds.Add((long)a.product_id);
                if (a.component_id != null) appComps.Add((long)a.component_id);
                if (string.Equals((string)a.applicability_type, "DoesNotApply", StringComparison.OrdinalIgnoreCase) && a.product_version_id != null)
                {
                    negativeVersions.Add((long)a.product_version_id);
                }
            }

            results.Add(new SolutionSearchRawResult
            {
                Id = itemId,
                KnowledgeCode = (string)r.KnowledgeCode,
                Title = (string)r.Title,
                Summary = (string)r.Summary,
                Status = (string)r.Status,
                Version = (string)r.Version,
                ProblemDescription = (string?)r.ProblemDescription,
                ValidationMethod = (string?)r.ValidationMethod,
                RiskWarning = (string?)r.RiskWarning,
                RollbackPlan = (string?)r.RollbackPlan,
                ContentMarkdown = (string?)r.ContentMarkdown,
                OwnerDepartmentId = (long?)r.OwnerDepartmentId,
                CreatedAt = (DateTime)r.CreatedAt,
                PublishedAt = (DateTime?)r.PublishedAt,
                SourceCaseId = (long?)r.SourceCaseId,
                TotalUsages = Convert.ToInt32(r.TotalUsages),
                SuccessfulUsages = Convert.ToInt32(r.SuccessfulUsages),
                TextScore = Convert.ToDouble(r.TextScore),
                ApplicableEnvironments = appEnvs,
                ApplicableProductIds = appProds,
                ApplicableComponentIds = appComps,
                NegativeProductVersionIds = negativeVersions
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<ProductSearchRawResult>> SearchProductsAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id AS Id, code AS Code, name AS Name, description AS Description,
                CASE
                    WHEN @Query = '' THEN 50.0
                    WHEN code = @Query THEN 100.0
                    WHEN name = @Query THEN 95.0
                    WHEN code LIKE @LikeTerm THEN 80.0
                    WHEN name LIKE @LikeTerm THEN 70.0
                    WHEN description LIKE @LikeTerm THEN 60.0
                    ELSE 50.0
                END AS TextScore
            FROM products
            WHERE @Query = '' OR code LIKE @LikeTerm OR name LIKE @LikeTerm OR description LIKE @LikeTerm
            ORDER BY TextScore DESC, name ASC
            LIMIT @Limit;";

        var rows = await conn.QueryAsync<ProductSearchRawResult>(sql, new
        {
            Query = query ?? string.Empty,
            LikeTerm = $"%{query}%",
            Limit = limit
        });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ComponentSearchRawResult>> SearchComponentsAsync(string query, long? productId = null, int limit = 20, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT c.id AS Id, COALESCE(c.product_id, 0) AS ProductId, COALESCE(p.name, 'Sistema') AS ProductName, c.name AS Name, c.description AS Description, c.component_type AS Technology,
                CASE
                    WHEN @Query = '' THEN 50.0
                    WHEN c.name = @Query THEN 95.0
                    WHEN c.name LIKE @LikeTerm THEN 80.0
                    WHEN c.component_type LIKE @LikeTerm THEN 70.0
                    WHEN c.description LIKE @LikeTerm THEN 60.0
                    ELSE 50.0
                END AS TextScore
            FROM components c
            INNER JOIN products p ON p.id = c.product_id
            WHERE (@Query = '' OR c.name LIKE @LikeTerm OR c.description LIKE @LikeTerm OR c.component_type LIKE @LikeTerm) ";

        var parameters = new DynamicParameters();
        parameters.Add("Query", query ?? string.Empty);
        parameters.Add("LikeTerm", $"%{query}%");
        parameters.Add("Limit", limit);

        if (productId.HasValue)
        {
            sql += " AND c.product_id = @ProductId";
            parameters.Add("ProductId", productId.Value);
        }

        sql += " ORDER BY TextScore DESC, c.name ASC LIMIT @Limit;";

        var rows = await conn.QueryAsync<ComponentSearchRawResult>(sql, parameters);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<RootCauseSearchRawResult>> SearchRootCausesAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id AS Id, name AS Name, category AS Category, description AS Description,
                CASE
                    WHEN @Query = '' THEN 50.0
                    WHEN name = @Query THEN 95.0
                    WHEN name LIKE @LikeTerm THEN 80.0
                    WHEN category LIKE @LikeTerm THEN 70.0
                    WHEN description LIKE @LikeTerm THEN 60.0
                    ELSE 50.0
                END AS TextScore
            FROM root_causes
            WHERE @Query = '' OR name LIKE @LikeTerm OR description LIKE @LikeTerm OR category LIKE @LikeTerm
            ORDER BY TextScore DESC, name ASC
            LIMIT @Limit;";

        var rows = await conn.QueryAsync<RootCauseSearchRawResult>(sql, new
        {
            Query = query ?? string.Empty,
            LikeTerm = $"%{query}%",
            Limit = limit
        });
        return rows.ToList();
    }
}
