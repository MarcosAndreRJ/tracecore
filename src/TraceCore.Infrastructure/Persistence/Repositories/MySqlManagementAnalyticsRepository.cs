using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlManagementAnalyticsRepository : IManagementAnalyticsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlManagementAnalyticsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OverviewRawMetrics> GetOverviewMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        var sql = $@"
            SELECT 
                COUNT(DISTINCT c.id) AS TotalCases,
                COUNT(DISTINCT CASE WHEN c.status IN ('Open', 'Reopened') THEN c.id END) AS OpenCases,
                COUNT(DISTINCT CASE WHEN c.status = 'Resolved' THEN c.id END) AS ResolvedCases,
                COUNT(DISTINCT CASE WHEN EXISTS (
                    SELECT 1 FROM case_relations cr 
                    WHERE (cr.source_case_id = c.id OR cr.target_case_id = c.id) 
                      AND cr.relation_type IN ('Recurrence', 'CommonCause')
                ) THEN c.id END) AS RecurrentCases,
                COUNT(DISTINCT CASE WHEN c.status = 'Resolved' AND NOT EXISTS (
                    SELECT 1 FROM case_resolutions r 
                    WHERE r.case_id = c.id AND r.root_cause_id IS NOT NULL AND r.root_cause_confirmed = 1
                ) THEN c.id END) AS UnconfirmedRootCauseCases,
                COUNT(DISTINCT CASE WHEN c.status = 'Resolved' AND NOT EXISTS (
                    SELECT 1 FROM knowledge_items ki 
                    WHERE ki.provenance_case_id = c.id
                ) THEN c.id END) AS UndocumentedKnowledgeCases
            FROM cases c
            {whereSql};";

        var row = await conn.QueryFirstOrDefaultAsync<OverviewRawMetrics>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return row ?? new OverviewRawMetrics();
    }

    public async Task<IReadOnlyList<double>> GetResolvedIterationDurationsMinutesAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        var sql = $@"
            SELECT TIMESTAMPDIFF(MINUTE, ci.opened_at, ci.closed_at) AS Duration
            FROM case_iterations ci
            JOIN cases c ON ci.case_id = c.id
            {whereSql}
              AND ci.status = 'Resolved'
              AND ci.closed_at IS NOT NULL
            ORDER BY Duration ASC;";

        var list = (await conn.QueryAsync<double>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<TimeEvolutionRawItem>> GetTimeEvolutionAsync(AnalyticsFilterCriteria criteria, string grouping = "day", CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        string dateExpr = grouping switch
        {
            "month" => "DATE_FORMAT(c.opened_at, '%Y-%m-01')",
            "week" => "STR_TO_DATE(CONCAT(YEARWEEK(c.opened_at, 3), ' Monday'), '%X%V %W')",
            _ => "DATE(c.opened_at)"
        };

        var sql = $@"
            SELECT 
                {dateExpr} AS DateBucket,
                COUNT(DISTINCT c.id) AS OpenedCount,
                COUNT(DISTINCT CASE WHEN c.status = 'Resolved' THEN c.id END) AS ResolvedCount
            FROM cases c
            {whereSql}
            GROUP BY {dateExpr}
            ORDER BY DateBucket ASC;";

        var list = (await conn.QueryAsync<TimeEvolutionRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<EntityCountRawItem>> GetTopProductsAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("Limit", limit);

        var sql = $@"
            SELECT 
                p.id AS Id,
                p.name AS Label,
                NULL AS SecondaryLabel,
                COUNT(DISTINCT c.id) AS Count
            FROM cases c
            JOIN products p ON c.product_id = p.id
            {whereSql}
            GROUP BY p.id, p.name
            ORDER BY Count DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<EntityCountRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<EntityCountRawItem>> GetTopComponentsAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("Limit", limit);

        var sql = $@"
            SELECT 
                comp.id AS Id,
                comp.name AS Label,
                comp.code AS SecondaryLabel,
                COUNT(DISTINCT cc.case_id) AS Count
            FROM case_components cc
            JOIN cases c ON cc.case_id = c.id
            JOIN components comp ON cc.component_id = comp.id
            {whereSql}
            GROUP BY comp.id, comp.name, comp.code
            ORDER BY Count DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<EntityCountRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<EntityCountRawItem>> GetTopRootCausesAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("Limit", limit);

        var sql = $@"
            SELECT 
                COALESCE(rc.id, 0) AS Id,
                COALESCE(rc.name, 'Sem causa raiz definida') AS Label,
                rc.code AS SecondaryLabel,
                COUNT(DISTINCT c.id) AS Count
            FROM cases c
            JOIN case_resolutions res ON c.id = res.case_id
            LEFT JOIN root_causes rc ON res.root_cause_id = rc.id
            {whereSql}
              AND c.status = 'Resolved'
            GROUP BY COALESCE(rc.id, 0), COALESCE(rc.name, 'Sem causa raiz definida'), rc.code
            ORDER BY Count DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<EntityCountRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<EntityCountRawItem>> GetTopRecurrencesAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("Limit", limit);

        var sql = $@"
            SELECT 
                NULL AS Id,
                cr.relation_type AS Label,
                'Relação identificada' AS SecondaryLabel,
                COUNT(DISTINCT cr.id) AS Count
            FROM case_relations cr
            JOIN cases c ON (cr.source_case_id = c.id OR cr.target_case_id = c.id)
            {whereSql}
              AND cr.relation_type IN ('Recurrence', 'CommonCause')
            GROUP BY cr.relation_type
            ORDER BY Count DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<EntityCountRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<AttentionCaseRawItem>> GetAttentionCasesAsync(AnalyticsFilterCriteria criteria, int limit = 10, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("Limit", limit);

        var sql = $@"
            SELECT 
                c.id AS Id,
                c.case_number AS CaseNumber,
                COALESCE(NULLIF(c.normalized_summary, ''), c.original_report) AS Title,
                c.status AS Status,
                c.severity AS Severity,
                c.opened_at AS OpenedAt,
                DATEDIFF(NOW(), c.opened_at) AS ActiveDays,
                (SELECT COUNT(*) FROM case_iterations ci WHERE ci.case_id = c.id) AS IterationCount,
                EXISTS (
                    SELECT 1 FROM case_resolutions r 
                    WHERE r.case_id = c.id AND r.root_cause_id IS NOT NULL AND r.root_cause_confirmed = 1
                ) AS HasRootCause,
                EXISTS (
                    SELECT 1 FROM knowledge_items ki 
                    WHERE ki.provenance_case_id = c.id
                ) AS HasKnowledge,
                CASE 
                    WHEN c.status IN ('Open', 'Reopened') AND c.severity IN ('Critical', 'High') AND DATEDIFF(NOW(), c.opened_at) >= 2 THEN 'Crítico/Alto pendente há vários dias'
                    WHEN (SELECT COUNT(*) FROM case_iterations ci WHERE ci.case_id = c.id) > 1 THEN 'Caso reaberto com múltiplas iterações'
                    WHEN EXISTS (
                        SELECT 1 FROM case_relations cr 
                        WHERE (cr.source_case_id = c.id OR cr.target_case_id = c.id) 
                          AND cr.relation_type IN ('Recurrence', 'CommonCause')
                    ) AND NOT EXISTS (
                        SELECT 1 FROM case_resolutions r 
                        WHERE r.case_id = c.id AND r.root_cause_id IS NOT NULL AND r.root_cause_confirmed = 1
                    ) THEN 'Incidente recorrente sem causa raiz confirmada'
                    WHEN c.status = 'Resolved' AND NOT EXISTS (
                        SELECT 1 FROM knowledge_items ki WHERE ki.provenance_case_id = c.id
                    ) THEN 'Resolvido sem artigo na Base de Conhecimento'
                    ELSE 'Acompanhamento prioritário'
                END AS AttentionReason
            FROM cases c
            {whereSql}
              AND (
                  (c.status IN ('Open', 'Reopened') AND c.severity IN ('Critical', 'High'))
                  OR (SELECT COUNT(*) FROM case_iterations ci WHERE ci.case_id = c.id) > 1
                  OR EXISTS (
                      SELECT 1 FROM case_relations cr 
                      WHERE (cr.source_case_id = c.id OR cr.target_case_id = c.id) 
                        AND cr.relation_type IN ('Recurrence', 'CommonCause')
                  )
                  OR (c.status = 'Resolved' AND NOT EXISTS (SELECT 1 FROM knowledge_items ki WHERE ki.provenance_case_id = c.id))
              )
            ORDER BY c.opened_at DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<AttentionCaseRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<DepartmentRawMetrics>> GetDepartmentMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        var sql = $@"
            SELECT 
                d.id AS DepartmentId,
                d.name AS DepartmentName,
                COUNT(DISTINCT CASE WHEN c.status IN ('Open', 'Reopened') THEN c.id END) AS ActiveCasesCount,
                COUNT(DISTINCT CASE WHEN c.status = 'Resolved' THEN c.id END) AS ResolvedCasesCount,
                COUNT(DISTINCT CASE WHEN (SELECT COUNT(*) FROM case_iterations ci WHERE ci.case_id = c.id) > 1 THEN c.id END) AS ReopenedCasesCount,
                COUNT(DISTINCT CASE WHEN EXISTS (
                    SELECT 1 FROM case_relations cr 
                    WHERE (cr.source_case_id = c.id OR cr.target_case_id = c.id) 
                      AND cr.relation_type IN ('Recurrence', 'CommonCause')
                ) THEN c.id END) AS RecurrentCasesCount,
                (
                    SELECT COUNT(*) FROM knowledge_items ki 
                    WHERE ki.owner_department_id = d.id
                ) AS KnowledgeCreatedCount,
                (
                    SELECT COUNT(*) FROM diagnostic_steps ds 
                    JOIN user_departments ud ON ds.performed_by = ud.user_id 
                    WHERE ud.department_id = d.id
                ) AS DiagnosticStepsCount
            FROM departments d
            LEFT JOIN cases c ON (c.current_department_id = d.id OR EXISTS (
                SELECT 1 FROM case_components cc 
                JOIN component_owners co ON cc.component_id = co.component_id 
                WHERE cc.case_id = c.id AND co.department_id = d.id
            ))
            {whereSql}
            GROUP BY d.id, d.name
            ORDER BY d.name ASC;";

        var list = (await conn.QueryAsync<DepartmentRawMetrics>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<double>> GetDepartmentIterationDurationsMinutesAsync(long departmentId, AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("DepId", departmentId);

        var sql = $@"
            SELECT TIMESTAMPDIFF(MINUTE, ci.opened_at, ci.closed_at) AS Duration
            FROM case_iterations ci
            JOIN cases c ON ci.case_id = c.id
            {whereSql}
              AND ci.status = 'Resolved'
              AND ci.closed_at IS NOT NULL
              AND (c.current_department_id = @DepId OR EXISTS (
                  SELECT 1 FROM case_components cc 
                  JOIN component_owners co ON cc.component_id = co.component_id 
                  WHERE cc.case_id = c.id AND co.department_id = @DepId
              ))
            ORDER BY Duration ASC;";

        var list = (await conn.QueryAsync<double>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<IReadOnlyList<UserRawMetrics>> GetUserMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        var sql = $@"
            SELECT 
                u.id AS UserId,
                u.name AS UserName,
                u.email AS UserEmail,
                COUNT(DISTINCT CASE WHEN c.created_by = u.id THEN c.id END) AS OpenedCasesCount,
                COUNT(DISTINCT CASE WHEN res.resolved_by = u.id THEN c.id END) AS ResolvedCasesCount,
                (SELECT COUNT(*) FROM diagnostic_steps ds WHERE ds.performed_by = u.id) AS DiagnosticStepsCount,
                (SELECT COUNT(*) FROM case_hypotheses ch WHERE ch.created_by = u.id) AS HypothesesCreatedCount,
                (SELECT COUNT(*) FROM case_evidences ce WHERE ce.created_by = u.id) AS EvidencesCreatedCount,
                (SELECT COUNT(*) FROM knowledge_items ki WHERE ki.created_by = u.id) AS KnowledgeAuthoredCount,
                (SELECT COUNT(*) FROM knowledge_usages ku WHERE ku.used_by = u.id) AS KnowledgeUsedCount,
                (
                    SELECT p.name 
                    FROM cases c2 
                    JOIN products p ON c2.product_id = p.id 
                    WHERE c2.current_owner_user_id = u.id OR c2.created_by = u.id 
                    GROUP BY p.name 
                    ORDER BY COUNT(*) DESC 
                    LIMIT 1
                ) AS TopProduct,
                (
                    SELECT comp.name 
                    FROM case_components cc 
                    JOIN components comp ON cc.component_id = comp.id 
                    JOIN cases c3 ON cc.case_id = c3.id 
                    WHERE c3.current_owner_user_id = u.id OR c3.created_by = u.id 
                    GROUP BY comp.name 
                    ORDER BY COUNT(*) DESC 
                    LIMIT 1
                ) AS TopComponent
            FROM users u
            LEFT JOIN cases c ON (c.current_owner_user_id = u.id OR c.created_by = u.id)
            LEFT JOIN case_resolutions res ON c.id = res.case_id AND res.resolved_by = u.id
            {whereSql}
              AND u.status = 'Active'
            GROUP BY u.id, u.name, u.email
            ORDER BY u.name ASC;";

        var list = (await conn.QueryAsync<UserRawMetrics>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<KnowledgeRawMetrics> GetKnowledgeMetricsAsync(AnalyticsFilterCriteria criteria, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT 
                COUNT(DISTINCT CASE WHEN ki.status = 'Published' THEN ki.id END) AS TotalPublished,
                COUNT(DISTINCT CASE WHEN ki.status = 'Deprecated' THEN ki.id END) AS TotalDeprecated,
                COUNT(DISTINCT CASE WHEN ki.status = 'Published' AND ki.last_reviewed_at IS NULL THEN ki.id END) AS NeverReviewedCount,
                COUNT(DISTINCT CASE WHEN ki.status = 'Published' AND ki.review_due_at IS NOT NULL AND ki.review_due_at < NOW() THEN ki.id END) AS ReviewOverdueCount,
                COUNT(DISTINCT ku.id) AS TotalUsagesCount,
                COUNT(DISTINCT CASE WHEN ku.outcome = 'Worked' THEN ku.id END) AS WorkedUsagesCount,
                COUNT(DISTINCT CASE WHEN ku.outcome = 'PartiallyWorked' THEN ku.id END) AS PartiallyWorkedUsagesCount,
                COUNT(DISTINCT CASE WHEN ku.outcome = 'DidNotWork' THEN ku.id END) AS DidNotWorkUsagesCount,
                COUNT(DISTINCT CASE WHEN ku.outcome = 'Inconclusive' THEN ku.id END) AS InconclusiveUsagesCount
            FROM knowledge_items ki
            LEFT JOIN knowledge_usages ku ON ki.id = ku.knowledge_item_id;";

        var row = await conn.QueryFirstOrDefaultAsync<KnowledgeRawMetrics>(new CommandDefinition(sql, cancellationToken: ct));
        return row ?? new KnowledgeRawMetrics();
    }

    public async Task<IReadOnlyList<EntityCountRawItem>> GetTopUsedKnowledgeAsync(AnalyticsFilterCriteria criteria, int limit = 5, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);

        var sql = @"
            SELECT 
                ki.id AS Id,
                ki.title AS Label,
                ki.knowledge_code AS SecondaryLabel,
                COUNT(ku.id) AS Count
            FROM knowledge_usages ku
            JOIN knowledge_items ki ON ku.knowledge_item_id = ki.id
            GROUP BY ki.id, ki.title, ki.knowledge_code
            ORDER BY Count DESC
            LIMIT @Limit;";

        var list = (await conn.QueryAsync<EntityCountRawItem>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        return list;
    }

    public async Task<(string VersionLabel, DateTime? ReleasedAt, int BeforeCount, int AfterCount)> GetTrendAfterVersionRawAsync(
        long productVersionId,
        long? rootCauseId,
        string? errorCode,
        long? componentId,
        int intervalDays,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sqlVer = "SELECT version_label AS VersionLabel, released_at AS ReleasedAt FROM product_versions WHERE id = @Id;";
        var ver = await conn.QuerySingleOrDefaultAsync<(string VersionLabel, DateTime? ReleasedAt)>(sqlVer, new { Id = productVersionId });

        if (string.IsNullOrEmpty(ver.VersionLabel) || !ver.ReleasedAt.HasValue)
        {
            return (ver.VersionLabel ?? string.Empty, ver.ReleasedAt, 0, 0);
        }

        var releasedAt = ver.ReleasedAt.Value;
        var startBefore = releasedAt.AddDays(-intervalDays);
        var endAfter = releasedAt.AddDays(intervalDays);

        var filterConditions = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("ProductVersionId", productVersionId);
        parameters.Add("ReleasedAt", releasedAt);
        parameters.Add("StartBefore", startBefore);
        parameters.Add("EndAfter", endAfter);

        if (rootCauseId.HasValue)
        {
            filterConditions.Add("EXISTS (SELECT 1 FROM case_resolutions cr WHERE cr.case_id = c.id AND cr.root_cause_id = @RootCauseId)");
            parameters.Add("RootCauseId", rootCauseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            filterConditions.Add("c.error_code = @ErrorCode");
            parameters.Add("ErrorCode", errorCode.Trim());
        }

        if (componentId.HasValue)
        {
            filterConditions.Add("EXISTS (SELECT 1 FROM case_components cc WHERE cc.case_id = c.id AND cc.component_id = @ComponentId)");
            parameters.Add("ComponentId", componentId.Value);
        }

        string extraWhere = filterConditions.Count > 0 ? " AND " + string.Join(" AND ", filterConditions) : string.Empty;

        string sqlCountBefore = $"SELECT COUNT(*) FROM cases c WHERE c.opened_at >= @StartBefore AND c.opened_at < @ReleasedAt{extraWhere};";
        string sqlCountAfter = $"SELECT COUNT(*) FROM cases c WHERE c.opened_at >= @ReleasedAt AND c.opened_at <= @EndAfter{extraWhere};";

        int before = await conn.ExecuteScalarAsync<int>(sqlCountBefore, parameters);
        int after = await conn.ExecuteScalarAsync<int>(sqlCountAfter, parameters);

        return (ver.VersionLabel, ver.ReleasedAt, before, after);
    }

    public async Task<(int TotalCases, IReadOnlyList<(long ComponentId, string ComponentName, int Count)> ComponentCounts)> GetComponentAssociationRawAsync(
        AnalyticsFilterCriteria criteria,
        long? rootCauseId,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var (whereSql, parameters) = BuildWhereClause(criteria, "c");

        var extraConditions = new List<string>();
        if (rootCauseId.HasValue)
        {
            extraConditions.Add("EXISTS (SELECT 1 FROM case_resolutions cr WHERE cr.case_id = c.id AND cr.root_cause_id = @RootCauseId)");
            parameters.Add("RootCauseId", rootCauseId.Value);
        }

        string fullWhere = whereSql;
        if (extraConditions.Count > 0)
        {
            fullWhere += " AND " + string.Join(" AND ", extraConditions);
        }

        string sqlTotal = $"SELECT COUNT(*) FROM cases c {fullWhere};";
        int total = await conn.ExecuteScalarAsync<int>(sqlTotal, parameters);

        if (total == 0)
        {
            return (0, Array.Empty<(long, string, int)>());
        }

        string sqlComponents = $@"
            SELECT 
                comp.id AS ComponentId,
                comp.name AS ComponentName,
                COUNT(DISTINCT c.id) AS Count
            FROM cases c
            INNER JOIN case_components cc ON cc.case_id = c.id
            INNER JOIN components comp ON comp.id = cc.component_id
            {fullWhere}
            GROUP BY comp.id, comp.name
            ORDER BY Count DESC;";

        var rows = await conn.QueryAsync<(long ComponentId, string ComponentName, int Count)>(sqlComponents, parameters);
        return (total, rows.ToList());
    }

    public async Task<(string SolutionTitle, string ScopeLabel, IReadOnlyList<double> DurationsWithMinutes, IReadOnlyList<double> DurationsWithoutMinutes)> GetSolutionEffectivenessRawAsync(
        long knowledgeItemId,
        AnalyticsFilterCriteria criteria,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sqlKi = @"
            SELECT 
                ki.title AS Title,
                d.name AS DepartmentName
            FROM knowledge_items ki
            LEFT JOIN departments d ON d.id = ki.owner_department_id
            WHERE ki.id = @Id;";

        var ki = await conn.QuerySingleOrDefaultAsync<(string Title, string? DepartmentName)>(sqlKi, new { Id = knowledgeItemId });
        if (string.IsNullOrEmpty(ki.Title))
        {
            return (string.Empty, "Escopo não encontrado", Array.Empty<double>(), Array.Empty<double>());
        }

        string scopeLabel = !string.IsNullOrEmpty(ki.DepartmentName) ? $"Departamento: {ki.DepartmentName}" : "Escopo Geral";

        var (whereSql, parameters) = BuildWhereClause(criteria, "c");
        parameters.Add("KnowledgeItemId", knowledgeItemId);

        string baseWhere = whereSql;

        string whereWith = baseWhere + " AND EXISTS (SELECT 1 FROM knowledge_usages ku WHERE ku.case_id = c.id AND ku.knowledge_item_id = @KnowledgeItemId)";
        string whereWithout = baseWhere + " AND NOT EXISTS (SELECT 1 FROM knowledge_usages ku WHERE ku.case_id = c.id AND ku.knowledge_item_id = @KnowledgeItemId)";

        string sqlWith = $@"
            SELECT TIMESTAMPDIFF(SECOND, ci.opened_at, ci.closed_at) / 60.0
            FROM case_iterations ci
            INNER JOIN cases c ON c.id = ci.case_id
            {whereWith}
              AND ci.closed_at IS NOT NULL;";

        string sqlWithout = $@"
            SELECT TIMESTAMPDIFF(SECOND, ci.opened_at, ci.closed_at) / 60.0
            FROM case_iterations ci
            INNER JOIN cases c ON c.id = ci.case_id
            {whereWithout}
              AND ci.closed_at IS NOT NULL;";

        var durationsWith = (await conn.QueryAsync<double>(sqlWith, parameters)).ToList();
        var durationsWithout = (await conn.QueryAsync<double>(sqlWithout, parameters)).ToList();

        return (ki.Title, scopeLabel, durationsWith, durationsWithout);
    }

    private static (string WhereSql, DynamicParameters Parameters) BuildWhereClause(AnalyticsFilterCriteria criteria, string tableAlias)
    {
        var parameters = new DynamicParameters();
        var conditions = new List<string>();

        if (criteria.FromUtc.HasValue)
        {
            conditions.Add($"{tableAlias}.opened_at >= @FromUtc");
            parameters.Add("FromUtc", criteria.FromUtc.Value);
        }

        if (criteria.ToUtc.HasValue)
        {
            conditions.Add($"{tableAlias}.opened_at <= @ToUtc");
            parameters.Add("ToUtc", criteria.ToUtc.Value);
        }

        if (criteria.ClientId.HasValue)
        {
            conditions.Add($"{tableAlias}.client_id = @ClientId");
            parameters.Add("ClientId", criteria.ClientId.Value);
        }

        if (criteria.ClientUnitId.HasValue)
        {
            conditions.Add($"{tableAlias}.client_unit_id = @ClientUnitId");
            parameters.Add("ClientUnitId", criteria.ClientUnitId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            conditions.Add($"{tableAlias}.product_id = @ProductId");
            parameters.Add("ProductId", criteria.ProductId.Value);
        }

        if (criteria.ProductVersionId.HasValue)
        {
            conditions.Add($"{tableAlias}.product_version_id = @ProductVersionId");
            parameters.Add("ProductVersionId", criteria.ProductVersionId.Value);
        }

        if (criteria.EnvironmentId.HasValue)
        {
            conditions.Add($"{tableAlias}.environment_id = @EnvironmentId");
            parameters.Add("EnvironmentId", criteria.EnvironmentId.Value);
        }

        if (criteria.ComponentId.HasValue)
        {
            conditions.Add($"EXISTS (SELECT 1 FROM case_components cc_filter WHERE cc_filter.case_id = {tableAlias}.id AND cc_filter.component_id = @ComponentId)");
            parameters.Add("ComponentId", criteria.ComponentId.Value);
        }

        if (criteria.DepartmentId.HasValue)
        {
            conditions.Add($"{tableAlias}.current_department_id = @DepartmentId");
            parameters.Add("DepartmentId", criteria.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Severity))
        {
            conditions.Add($"{tableAlias}.severity = @Severity");
            parameters.Add("Severity", criteria.Severity.Trim());
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            conditions.Add($"{tableAlias}.status = @Status");
            parameters.Add("Status", criteria.Status.Trim());
        }

        // Sempre inclui "WHERE 1=1" como base — vários call-sites deste arquivo
        // concatenam "{whereSql} AND algo_mais"; com whereSql vazio (nenhum filtro
        // ativo) isso gerava um "AND" sem "WHERE" antes (erro de sintaxe). Manter
        // sempre um WHERE válido resolve isso para todos os usos, sem precisar
        // ajustar cada query individualmente.
        string whereSql = "WHERE 1=1" + (conditions.Count > 0 ? " AND " + string.Join(" AND ", conditions) : string.Empty);
        return (whereSql, parameters);
    }
}
