using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlDiagnosticFlowRepository : IDiagnosticFlowRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlDiagnosticFlowRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<DiagnosticFlow>> GetAllFlowsAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        string sql = @"
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                description AS Description,
                entry_keywords AS EntryKeywords,
                status AS Status,
                created_by AS CreatedBy,
                created_at AS CreatedAt
            FROM diagnostic_flows " +
            (activeOnly ? "WHERE status = 'Active' " : "") +
            "ORDER BY name ASC;";

        var rows = await conn.QueryAsync<DiagnosticFlow>(sql);
        return rows.ToList();
    }

    public async Task<DiagnosticFlow?> GetFlowByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                description AS Description,
                entry_keywords AS EntryKeywords,
                status AS Status,
                created_by AS CreatedBy,
                created_at AS CreatedAt
            FROM diagnostic_flows
            WHERE id = @Id;";

        var flow = await conn.QuerySingleOrDefaultAsync<DiagnosticFlow>(sql, new { Id = id });
        if (flow == null) return null;

        flow.CandidateHypotheses = (await GetHypothesesByFlowIdAsync(flow.Id, ct)).ToList();
        flow.Checks = (await GetChecksByFlowIdAsync(flow.Id, ct)).ToList();

        return flow;
    }

    public async Task<DiagnosticFlow?> GetFlowByCodeAsync(string code, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                description AS Description,
                entry_keywords AS EntryKeywords,
                status AS Status,
                created_by AS CreatedBy,
                created_at AS CreatedAt
            FROM diagnostic_flows
            WHERE code = @Code;";

        var flow = await conn.QuerySingleOrDefaultAsync<DiagnosticFlow>(sql, new { Code = code });
        if (flow == null) return null;

        flow.CandidateHypotheses = (await GetHypothesesByFlowIdAsync(flow.Id, ct)).ToList();
        flow.Checks = (await GetChecksByFlowIdAsync(flow.Id, ct)).ToList();

        return flow;
    }

    public async Task<long> AddFlowAsync(DiagnosticFlow flow, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO diagnostic_flows (code, name, description, entry_keywords, status, created_by, created_at)
            VALUES (@Code, @Name, @Description, @EntryKeywords, @Status, @CreatedBy, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        long id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            flow.Code,
            flow.Name,
            flow.Description,
            flow.EntryKeywords,
            flow.Status,
            flow.CreatedBy,
            flow.CreatedAt
        });
        flow.Id = id;
        return id;
    }

    public async Task UpdateFlowAsync(DiagnosticFlow flow, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            UPDATE diagnostic_flows
            SET name = @Name,
                description = @Description,
                entry_keywords = @EntryKeywords,
                status = @Status
            WHERE id = @Id;";

        await conn.ExecuteAsync(sql, new
        {
            flow.Id,
            flow.Name,
            flow.Description,
            flow.EntryKeywords,
            flow.Status
        });
    }

    public async Task<bool> DeleteFlowAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "DELETE FROM diagnostic_flows WHERE id = @Id;";
        int affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<IReadOnlyList<DiagnosticFlowHypothesis>> GetHypothesesByFlowIdAsync(long flowId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                id AS Id,
                flow_id AS FlowId,
                title AS Title,
                description AS Description,
                associated_component_id AS AssociatedComponentId
            FROM diagnostic_flow_hypotheses
            WHERE flow_id = @FlowId
            ORDER BY id ASC;";

        var rows = await conn.QueryAsync<DiagnosticFlowHypothesis>(sql, new { FlowId = flowId });
        return rows.ToList();
    }

    public async Task<long> AddHypothesisAsync(DiagnosticFlowHypothesis hypothesis, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description, associated_component_id)
            VALUES (@FlowId, @Title, @Description, @AssociatedComponentId);
            SELECT LAST_INSERT_ID();";

        long id = await conn.ExecuteScalarAsync<long>(sql, hypothesis);
        hypothesis.Id = id;
        return id;
    }

    public async Task<bool> DeleteHypothesisAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "DELETE FROM diagnostic_flow_hypotheses WHERE id = @Id;";
        int affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<IReadOnlyList<DiagnosticCheck>> GetChecksByFlowIdAsync(long flowId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sqlChecks = @"
            SELECT 
                id AS Id,
                flow_id AS FlowId,
                code AS Code,
                title AS Title,
                question_text AS QuestionText,
                check_type AS CheckType,
                cost AS Cost,
                risk_level AS RiskLevel,
                skip_condition_field AS SkipConditionField,
                created_at AS CreatedAt
            FROM diagnostic_checks
            WHERE flow_id = @FlowId
            ORDER BY id ASC;";

        var checks = (await conn.QueryAsync<DiagnosticCheck>(sqlChecks, new { FlowId = flowId })).ToList();
        if (checks.Count == 0) return checks;

        var checkIds = checks.Select(c => c.Id).ToList();

        const string sqlOptions = @"
            SELECT 
                id AS Id,
                check_id AS CheckId,
                option_text AS OptionText,
                order_no AS OrderNo
            FROM diagnostic_check_options
            WHERE check_id IN @CheckIds
            ORDER BY order_no ASC;";

        var options = (await conn.QueryAsync<DiagnosticCheckOption>(sqlOptions, new { CheckIds = checkIds })).ToList();
        var optIds = options.Select(o => o.Id).ToList();

        List<DiagnosticCheckImpact> impacts = [];
        if (optIds.Count > 0)
        {
            const string sqlImpacts = @"
                SELECT 
                    id AS Id,
                    check_option_id AS CheckOptionId,
                    flow_hypothesis_id AS FlowHypothesisId,
                    impact_type AS ImpactType,
                    weight AS Weight
                FROM diagnostic_check_impacts
                WHERE check_option_id IN @OptionIds;";

            impacts = (await conn.QueryAsync<DiagnosticCheckImpact>(sqlImpacts, new { OptionIds = optIds })).ToList();
        }

        var impactsLookup = impacts.ToLookup(i => i.CheckOptionId);
        foreach (var opt in options)
        {
            opt.Impacts = impactsLookup[opt.Id].ToList();
        }

        var optionsLookup = options.ToLookup(o => o.CheckId);
        foreach (var check in checks)
        {
            check.Options = optionsLookup[check.Id].ToList();
        }

        return checks;
    }

    public async Task<DiagnosticCheck?> GetCheckByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                id AS Id,
                flow_id AS FlowId,
                code AS Code,
                title AS Title,
                question_text AS QuestionText,
                check_type AS CheckType,
                cost AS Cost,
                risk_level AS RiskLevel,
                skip_condition_field AS SkipConditionField,
                created_at AS CreatedAt
            FROM diagnostic_checks
            WHERE id = @Id;";

        var check = await conn.QuerySingleOrDefaultAsync<DiagnosticCheck>(sql, new { Id = id });
        if (check == null) return null;

        const string sqlOptions = @"
            SELECT 
                id AS Id,
                check_id AS CheckId,
                option_text AS OptionText,
                order_no AS OrderNo
            FROM diagnostic_check_options
            WHERE check_id = @CheckId
            ORDER BY order_no ASC;";

        var options = (await conn.QueryAsync<DiagnosticCheckOption>(sqlOptions, new { CheckId = check.Id })).ToList();
        var optIds = options.Select(o => o.Id).ToList();

        if (optIds.Count > 0)
        {
            const string sqlImpacts = @"
                SELECT 
                    id AS Id,
                    check_option_id AS CheckOptionId,
                    flow_hypothesis_id AS FlowHypothesisId,
                    impact_type AS ImpactType,
                    weight AS Weight
                FROM diagnostic_check_impacts
                WHERE check_option_id IN @OptionIds;";

            var impacts = (await conn.QueryAsync<DiagnosticCheckImpact>(sqlImpacts, new { OptionIds = optIds })).ToList();
            var impactsLookup = impacts.ToLookup(i => i.CheckOptionId);
            foreach (var opt in options)
            {
                opt.Impacts = impactsLookup[opt.Id].ToList();
            }
        }

        check.Options = options;
        return check;
    }

    public async Task<long> AddCheckAsync(DiagnosticCheck check, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, skip_condition_field, created_at)
            VALUES (@FlowId, @Code, @Title, @QuestionText, @CheckType, @Cost, @RiskLevel, @SkipConditionField, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        long id = await conn.ExecuteScalarAsync<long>(sql, check);
        check.Id = id;
        return id;
    }

    public async Task<bool> DeleteCheckAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "DELETE FROM diagnostic_checks WHERE id = @Id;";
        int affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<long> AddCheckOptionAsync(DiagnosticCheckOption option, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
            VALUES (@CheckId, @OptionText, @OrderNo);
            SELECT LAST_INSERT_ID();";

        long id = await conn.ExecuteScalarAsync<long>(sql, option);
        option.Id = id;
        return id;
    }

    public async Task<long> AddCheckImpactAsync(DiagnosticCheckImpact impact, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
            VALUES (@CheckOptionId, @FlowHypothesisId, @ImpactType, @Weight);
            SELECT LAST_INSERT_ID();";

        long id = await conn.ExecuteScalarAsync<long>(sql, impact);
        impact.Id = id;
        return id;
    }
}
