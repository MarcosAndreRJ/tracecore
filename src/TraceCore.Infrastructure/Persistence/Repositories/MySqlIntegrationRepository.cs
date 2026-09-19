using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlIntegrationRepository : IIntegrationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlIntegrationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Integration>> GetAllIntegrationsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                i.id,
                i.code,
                i.name,
                i.integration_type AS IntegrationType,
                i.target_system_description AS TargetSystemDescription,
                i.status,
                i.owner_department_id AS OwnerDepartmentId,
                i.contract_notes AS ContractNotes,
                i.health_check_url AS HealthCheckUrl,
                i.health_check_method AS HealthCheckMethod,
                i.health_check_timeout_seconds AS HealthCheckTimeoutSeconds,
                i.health_check_expected_status_code AS HealthCheckExpectedStatusCode,
                i.created_by AS CreatedBy,
                i.created_at AS CreatedAt,
                i.updated_at AS UpdatedAt,
                d.name AS OwnerDepartmentName
            FROM integrations i
            LEFT JOIN departments d ON i.owner_department_id = d.id
            ORDER BY i.name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Integration>(sql);
        return list.ToList();
    }

    public async Task<Integration?> GetIntegrationByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                i.id,
                i.code,
                i.name,
                i.integration_type AS IntegrationType,
                i.target_system_description AS TargetSystemDescription,
                i.status,
                i.owner_department_id AS OwnerDepartmentId,
                i.contract_notes AS ContractNotes,
                i.health_check_url AS HealthCheckUrl,
                i.health_check_method AS HealthCheckMethod,
                i.health_check_timeout_seconds AS HealthCheckTimeoutSeconds,
                i.health_check_expected_status_code AS HealthCheckExpectedStatusCode,
                i.created_by AS CreatedBy,
                i.created_at AS CreatedAt,
                i.updated_at AS UpdatedAt,
                d.name AS OwnerDepartmentName
            FROM integrations i
            LEFT JOIN departments d ON i.owner_department_id = d.id
            WHERE i.id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Integration>(sql, new { Id = id });
    }

    public async Task<long> AddIntegrationAsync(Integration integration, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO integrations
                (code, name, integration_type, target_system_description, status, owner_department_id, contract_notes,
                 health_check_url, health_check_method, health_check_timeout_seconds, health_check_expected_status_code,
                 created_by, created_at)
            VALUES
                (@Code, @Name, @IntegrationType, @TargetSystemDescription, @Status, @OwnerDepartmentId, @ContractNotes,
                 @HealthCheckUrl, @HealthCheckMethod, @HealthCheckTimeoutSeconds, @HealthCheckExpectedStatusCode,
                 @CreatedBy, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, integration);
        integration.Id = id;
        return id;
    }

    public async Task UpdateIntegrationAsync(Integration integration, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE integrations
            SET code = @Code,
                name = @Name,
                integration_type = @IntegrationType,
                target_system_description = @TargetSystemDescription,
                status = @Status,
                owner_department_id = @OwnerDepartmentId,
                contract_notes = @ContractNotes,
                health_check_url = @HealthCheckUrl,
                health_check_method = @HealthCheckMethod,
                health_check_timeout_seconds = @HealthCheckTimeoutSeconds,
                health_check_expected_status_code = @HealthCheckExpectedStatusCode,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        integration.UpdatedAt = System.DateTime.UtcNow;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, integration);
    }

    public async Task<IReadOnlyList<IntegrationRun>> GetRunsByIntegrationIdAsync(long integrationId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id,
                integration_id AS IntegrationId,
                started_at AS StartedAt,
                finished_at AS FinishedAt,
                status,
                records_processed AS RecordsProcessed,
                error_message AS ErrorMessage,
                recorded_by AS RecordedBy,
                recorded_at AS RecordedAt,
                triggered_by AS TriggeredBy
            FROM integration_runs
            WHERE integration_id = @IntegrationId
            ORDER BY recorded_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<IntegrationRun>(sql, new { IntegrationId = integrationId });
        return list.ToList();
    }

    public async Task<long> AddRunAsync(IntegrationRun run, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO integration_runs
                (integration_id, started_at, finished_at, status, records_processed, error_message, recorded_by, recorded_at, triggered_by)
            VALUES
                (@IntegrationId, @StartedAt, @FinishedAt, @Status, @RecordsProcessed, @ErrorMessage, @RecordedBy, @RecordedAt, @TriggeredBy);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, run);
        run.Id = id;
        return id;
    }
}