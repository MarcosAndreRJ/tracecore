using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlCaseResolutionRepository : ICaseResolutionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCaseResolutionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddResolutionAsync(CaseResolution resolution, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_resolutions (
                case_id, case_iteration_id, resolution_summary, validation_summary, root_cause_id,
                root_cause_confirmed, responsible_department_id, resolution_type,
                recurrence_risk, recurrence_notes, preventive_actions,
                effort_minutes, resolved_by, resolved_at
            ) VALUES (
                @CaseId, @CaseIterationId, @ResolutionSummary, @ValidationSummary, @RootCauseId,
                @RootCauseConfirmed, @ResponsibleDepartmentId, @ResolutionType,
                @RecurrenceRisk, @RecurrenceNotes, @PreventiveActions,
                @EffortMinutes, @ResolvedBy, @ResolvedAt
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, resolution);
        resolution.Id = id;
        return id;
    }

    public async Task<CaseResolution?> GetByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                case_id AS CaseId,
                case_iteration_id AS CaseIterationId,
                resolution_summary AS ResolutionSummary,
                validation_summary AS ValidationSummary,
                root_cause_id AS RootCauseId,
                root_cause_confirmed AS RootCauseConfirmed,
                responsible_department_id AS ResponsibleDepartmentId,
                resolution_type AS ResolutionType,
                recurrence_risk AS RecurrenceRisk,
                recurrence_notes AS RecurrenceNotes,
                preventive_actions AS PreventiveActions,
                effort_minutes AS EffortMinutes,
                resolved_by AS ResolvedBy,
                resolved_at AS ResolvedAt
            FROM case_resolutions
            WHERE case_id = @CaseId
            ORDER BY id DESC
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CaseResolution>(sql, new { CaseId = caseId });
    }

    public async Task<CaseResolution?> GetByIterationIdAsync(long iterationId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                case_id AS CaseId,
                case_iteration_id AS CaseIterationId,
                resolution_summary AS ResolutionSummary,
                validation_summary AS ValidationSummary,
                root_cause_id AS RootCauseId,
                root_cause_confirmed AS RootCauseConfirmed,
                responsible_department_id AS ResponsibleDepartmentId,
                resolution_type AS ResolutionType,
                recurrence_risk AS RecurrenceRisk,
                recurrence_notes AS RecurrenceNotes,
                preventive_actions AS PreventiveActions,
                effort_minutes AS EffortMinutes,
                resolved_by AS ResolvedBy,
                resolved_at AS ResolvedAt
            FROM case_resolutions
            WHERE case_iteration_id = @IterationId
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CaseResolution>(sql, new { IterationId = iterationId });
    }

    public async Task<IReadOnlyList<CaseResolution>> GetAllResolutionsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                case_id AS CaseId,
                case_iteration_id AS CaseIterationId,
                resolution_summary AS ResolutionSummary,
                validation_summary AS ValidationSummary,
                root_cause_id AS RootCauseId,
                root_cause_confirmed AS RootCauseConfirmed,
                responsible_department_id AS ResponsibleDepartmentId,
                resolution_type AS ResolutionType,
                recurrence_risk AS RecurrenceRisk,
                recurrence_notes AS RecurrenceNotes,
                preventive_actions AS PreventiveActions,
                effort_minutes AS EffortMinutes,
                resolved_by AS ResolvedBy,
                resolved_at AS ResolvedAt
            FROM case_resolutions
            WHERE case_id = @CaseId
            ORDER BY id ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<CaseResolution>(sql, new { CaseId = caseId });
        return list.ToList();
    }

    public async Task<CaseResolution?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id AS Id,
                case_id AS CaseId,
                case_iteration_id AS CaseIterationId,
                resolution_summary AS ResolutionSummary,
                validation_summary AS ValidationSummary,
                root_cause_id AS RootCauseId,
                root_cause_confirmed AS RootCauseConfirmed,
                responsible_department_id AS ResponsibleDepartmentId,
                resolution_type AS ResolutionType,
                recurrence_risk AS RecurrenceRisk,
                recurrence_notes AS RecurrenceNotes,
                preventive_actions AS PreventiveActions,
                effort_minutes AS EffortMinutes,
                resolved_by AS ResolvedBy,
                resolved_at AS ResolvedAt
            FROM case_resolutions
            WHERE id = @Id
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CaseResolution>(sql, new { Id = id });
    }

    public async Task<long> AddRootCauseAsync(RootCause rootCause, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO root_causes (code, name, category, description, created_at)
            VALUES (@Code, @Name, @Category, @Description, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, rootCause);
        rootCause.Id = id;
        return id;
    }

    public async Task<RootCause?> GetRootCauseByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, code AS Code, name AS Name, category AS Category, description AS Description, created_at AS CreatedAt
            FROM root_causes
            WHERE id = @Id
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<RootCause>(sql, new { Id = id });
    }

    public async Task<RootCause?> GetRootCauseByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, code AS Code, name AS Name, category AS Category, description AS Description, created_at AS CreatedAt
            FROM root_causes
            WHERE code = @Code
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<RootCause>(sql, new { Code = code });
    }

    public async Task<IReadOnlyList<RootCause>> GetAllRootCausesAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, code AS Code, name AS Name, category AS Category, description AS Description, created_at AS CreatedAt
            FROM root_causes
            ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<RootCause>(sql);
        return list.ToList();
    }
}
