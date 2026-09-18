using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlClientRepository : IClientRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlClientRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Client?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, code, name, status, external_crm_id AS ExternalCrmId, notes,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM clients 
            WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Client>(sql, new { Id = id });
    }

    public async Task<Client?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, code, name, status, external_crm_id AS ExternalCrmId, notes,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM clients 
            WHERE code = @Code;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Client>(sql, new { Code = code });
    }

    public async Task<Client?> GetByExternalCrmIdAsync(string externalCrmId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, code, name, status, external_crm_id AS ExternalCrmId, notes,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM clients 
            WHERE external_crm_id = @ExternalCrmId;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Client>(sql, new { ExternalCrmId = externalCrmId });
    }

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, code, name, status, external_crm_id AS ExternalCrmId, notes,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM clients 
            ORDER BY name ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Client>(sql);
        return list.ToList();
    }

    public async Task<long> AddAsync(Client client, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO clients (code, name, status, external_crm_id, notes, created_at)
            VALUES (@Code, @Name, @Status, @ExternalCrmId, @Notes, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            client.Code,
            client.Name,
            client.Status,
            client.ExternalCrmId,
            client.Notes,
            client.CreatedAt
        });
        client.Id = id;
        return id;
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE clients
            SET code = @Code,
                name = @Name,
                status = @Status,
                external_crm_id = @ExternalCrmId,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            client.Id,
            client.Code,
            client.Name,
            client.Status,
            client.ExternalCrmId,
            client.Notes,
            UpdatedAt = DateTime.UtcNow
        });
    }

    // =========================================================================
    // Unidades de Cliente
    // =========================================================================

    public async Task<IReadOnlyList<ClientUnit>> GetUnitsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, client_id AS ClientId, code, name, status, 
                   external_crm_id AS ExternalCrmId, created_at AS CreatedAt, 
                   updated_at AS UpdatedAt, created_by AS CreatedBy, updated_by AS UpdatedBy
            FROM client_units
            WHERE client_id = @ClientId
            ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ClientUnit>(sql, new { ClientId = clientId });
        return list.ToList();
    }

    public async Task<ClientUnit?> GetUnitByIdAsync(long unitId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, client_id AS ClientId, code, name, status, 
                   external_crm_id AS ExternalCrmId, created_at AS CreatedAt, 
                   updated_at AS UpdatedAt, created_by AS CreatedBy, updated_by AS UpdatedBy
            FROM client_units
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ClientUnit>(sql, new { Id = unitId });
    }

    public async Task<long> AddUnitAsync(ClientUnit unit, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO client_units (client_id, code, name, status, external_crm_id, created_at, created_by)
            VALUES (@ClientId, @Code, @Name, @Status, @ExternalCrmId, @CreatedAt, @CreatedBy);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            unit.ClientId,
            unit.Code,
            unit.Name,
            unit.Status,
            unit.ExternalCrmId,
            unit.CreatedAt,
            unit.CreatedBy
        });
        unit.Id = id;
        return id;
    }

    public async Task UpdateUnitAsync(ClientUnit unit, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE client_units
            SET code = @Code,
                name = @Name,
                status = @Status,
                external_crm_id = @ExternalCrmId,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            unit.Id,
            unit.Code,
            unit.Name,
            unit.Status,
            unit.ExternalCrmId,
            UpdatedAt = DateTime.UtcNow,
            unit.UpdatedBy
        });
    }

    // =========================================================================
    // Contextos Técnicos
    // =========================================================================

    public async Task<IReadOnlyList<ClientTechnicalContext>> GetTechnicalContextsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, client_id AS ClientId, client_unit_id AS ClientUnitId,
                   product_id AS ProductId, product_version_id AS ProductVersionId,
                   environment_id AS EnvironmentId, status,
                   effective_from AS EffectiveFrom, effective_to AS EffectiveTo,
                   created_at AS CreatedAt, updated_at AS UpdatedAt,
                   created_by AS CreatedBy, updated_by AS UpdatedBy
            FROM client_technical_contexts
            WHERE client_id = @ClientId
            ORDER BY effective_from DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ClientTechnicalContext>(sql, new { ClientId = clientId });
        return list.ToList();
    }

    public async Task<ClientTechnicalContext?> GetTechnicalContextByIdAsync(long contextId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, client_id AS ClientId, client_unit_id AS ClientUnitId,
                   product_id AS ProductId, product_version_id AS ProductVersionId,
                   environment_id AS EnvironmentId, status,
                   effective_from AS EffectiveFrom, effective_to AS EffectiveTo,
                   created_at AS CreatedAt, updated_at AS UpdatedAt,
                   created_by AS CreatedBy, updated_by AS UpdatedBy
            FROM client_technical_contexts
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ClientTechnicalContext>(sql, new { Id = contextId });
    }

    public async Task<long> AddTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO client_technical_contexts (
                client_id, client_unit_id, product_id, product_version_id, 
                environment_id, status, effective_from, effective_to, created_at, created_by
            )
            VALUES (
                @ClientId, @ClientUnitId, @ProductId, @ProductVersionId, 
                @EnvironmentId, @Status, @EffectiveFrom, @EffectiveTo, @CreatedAt, @CreatedBy
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            context.ClientId,
            context.ClientUnitId,
            context.ProductId,
            context.ProductVersionId,
            context.EnvironmentId,
            context.Status,
            context.EffectiveFrom,
            context.EffectiveTo,
            context.CreatedAt,
            context.CreatedBy
        });
        context.Id = id;
        return id;
    }

    public async Task UpdateTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE client_technical_contexts
            SET client_unit_id = @ClientUnitId,
                product_id = @ProductId,
                product_version_id = @ProductVersionId,
                environment_id = @EnvironmentId,
                status = @Status,
                effective_from = @EffectiveFrom,
                effective_to = @EffectiveTo,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            context.Id,
            context.ClientUnitId,
            context.ProductId,
            context.ProductVersionId,
            context.EnvironmentId,
            context.Status,
            context.EffectiveFrom,
            context.EffectiveTo,
            UpdatedAt = DateTime.UtcNow,
            context.UpdatedBy
        });
    }
}
