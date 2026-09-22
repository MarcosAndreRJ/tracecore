using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlProductTechnicalContextRepository : IProductTechnicalContextRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlProductTechnicalContextRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProductTechnicalProfile?> GetProfileByProductIdAsync(long productId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id, product_id AS ProductId,
                business_purpose AS BusinessPurpose,
                architecture_summary AS ArchitectureSummary,
                system_type AS SystemType,
                frontend_stack AS FrontendStack,
                backend_stack AS BackendStack,
                primary_database AS PrimaryDatabase,
                runtime_platform AS RuntimePlatform,
                hosting_model AS HostingModel,
                authentication_model AS AuthenticationModel,
                observability_stack AS ObservabilityStack,
                deployment_model AS DeploymentModel,
                vendor,
                support_notes AS SupportNotes,
                known_constraints AS KnownConstraints,
                investigation_notes AS InvestigationNotes,
                external_research_policy AS ExternalResearchPolicy,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                updated_at AS UpdatedAt,
                updated_by AS UpdatedBy
            FROM product_technical_profiles
            WHERE product_id = @ProductId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductTechnicalProfile>(sql, new { ProductId = productId });
    }

    public async Task<IReadOnlyList<ProductTechnicalProfile>> GetProfilesByProductIdsAsync(IReadOnlyList<long> productIds, CancellationToken ct = default)
    {
        if (productIds.Count == 0)
            return Array.Empty<ProductTechnicalProfile>();

        const string sql = @"
            SELECT
                id, product_id AS ProductId,
                business_purpose AS BusinessPurpose,
                architecture_summary AS ArchitectureSummary,
                system_type AS SystemType,
                frontend_stack AS FrontendStack,
                backend_stack AS BackendStack,
                primary_database AS PrimaryDatabase,
                runtime_platform AS RuntimePlatform,
                hosting_model AS HostingModel,
                authentication_model AS AuthenticationModel,
                observability_stack AS ObservabilityStack,
                deployment_model AS DeploymentModel,
                vendor,
                support_notes AS SupportNotes,
                known_constraints AS KnownConstraints,
                investigation_notes AS InvestigationNotes,
                external_research_policy AS ExternalResearchPolicy,
                created_at AS CreatedAt,
                created_by AS CreatedBy,
                updated_at AS UpdatedAt,
                updated_by AS UpdatedBy
            FROM product_technical_profiles
            WHERE product_id IN @ProductIds;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ProductTechnicalProfile>(sql, new { ProductIds = productIds });
        return rows.ToList();
    }

    public async Task UpsertProfileAsync(ProductTechnicalProfile profile, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_technical_profiles
                (product_id, business_purpose, architecture_summary, system_type, frontend_stack, backend_stack, primary_database,
                 runtime_platform, hosting_model, authentication_model, observability_stack, deployment_model, vendor,
                 support_notes, known_constraints, investigation_notes, external_research_policy,
                 created_at, created_by, updated_at, updated_by)
            VALUES
                (@ProductId, @BusinessPurpose, @ArchitectureSummary, @SystemType, @FrontendStack, @BackendStack, @PrimaryDatabase,
                 @RuntimePlatform, @HostingModel, @AuthenticationModel, @ObservabilityStack, @DeploymentModel, @Vendor,
                 @SupportNotes, @KnownConstraints, @InvestigationNotes, @ExternalResearchPolicy,
                 @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)
            ON DUPLICATE KEY UPDATE
                business_purpose = VALUES(business_purpose),
                architecture_summary = VALUES(architecture_summary),
                system_type = VALUES(system_type),
                frontend_stack = VALUES(frontend_stack),
                backend_stack = VALUES(backend_stack),
                primary_database = VALUES(primary_database),
                runtime_platform = VALUES(runtime_platform),
                hosting_model = VALUES(hosting_model),
                authentication_model = VALUES(authentication_model),
                observability_stack = VALUES(observability_stack),
                deployment_model = VALUES(deployment_model),
                vendor = VALUES(vendor),
                support_notes = VALUES(support_notes),
                known_constraints = VALUES(known_constraints),
                investigation_notes = VALUES(investigation_notes),
                external_research_policy = VALUES(external_research_policy),
                updated_at = VALUES(updated_at),
                updated_by = VALUES(updated_by);";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, profile);
    }

    public async Task<IReadOnlyList<string>> GetTechnologyNamesByProductIdAsync(long productId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT t.name
            FROM technologies t
            INNER JOIN product_technologies pt ON t.id = pt.technology_id
            WHERE pt.product_id = @ProductId
            ORDER BY t.name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<string>(sql, new { ProductId = productId });
        return rows.ToList();
    }

    public async Task SetProductTechnologiesAsync(long productId, IEnumerable<string> technologyNames, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM product_technologies WHERE product_id = @ProductId;", new { ProductId = productId });

        foreach (var tech in technologyNames.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var techId = await conn.ExecuteScalarAsync<long?>(
                "SELECT id FROM technologies WHERE name = @Name LIMIT 1;", new { Name = tech });

            if (!techId.HasValue)
            {
                techId = await conn.ExecuteScalarAsync<long>(
                    "INSERT INTO technologies (name) VALUES (@Name); SELECT LAST_INSERT_ID();", new { Name = tech });
            }

            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO product_technologies (product_id, technology_id, created_at)
                VALUES (@ProductId, @TechId, @CreatedAt);",
                new { ProductId = productId, TechId = techId.Value, CreatedAt = DateTime.UtcNow });
        }
    }

    public async Task<IReadOnlyList<ProductTechnicalSource>> GetSourcesByProductIdAsync(long productId, bool includeInactive = false, CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                id, product_id AS ProductId, name, source_type AS SourceType, url, description,
                trust_level AS TrustLevel, is_active AS IsActive,
                created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_technical_sources
            WHERE product_id = @ProductId";

        if (!includeInactive)
            sql += " AND is_active = 1";

        sql += " ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ProductTechnicalSource>(sql, new { ProductId = productId });
        return rows.ToList();
    }

    public async Task<ProductTechnicalSource?> GetSourceByIdAsync(long sourceId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id, product_id AS ProductId, name, source_type AS SourceType, url, description,
                trust_level AS TrustLevel, is_active AS IsActive,
                created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM product_technical_sources
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductTechnicalSource>(sql, new { Id = sourceId });
    }

    public async Task<long> AddSourceAsync(ProductTechnicalSource source, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_technical_sources
                (product_id, name, source_type, url, description, trust_level, is_active, created_at, created_by)
            VALUES
                (@ProductId, @Name, @SourceType, @Url, @Description, @TrustLevel, @IsActive, @CreatedAt, @CreatedBy);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, source);
        source.Id = id;
        return id;
    }

    public async Task UpdateSourceAsync(ProductTechnicalSource source, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE product_technical_sources
            SET name = @Name,
                source_type = @SourceType,
                url = @Url,
                description = @Description,
                trust_level = @TrustLevel,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";

        source.UpdatedAt = DateTime.UtcNow;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, source);
    }

    public async Task SetSourceActiveAsync(long sourceId, bool isActive, long? updatedBy, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE product_technical_sources
            SET is_active = @IsActive, updated_at = @UpdatedAt, updated_by = @UpdatedBy
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = sourceId, IsActive = isActive, UpdatedAt = DateTime.UtcNow, UpdatedBy = updatedBy });
    }

    public async Task<IReadOnlyList<ProductExternalResearchDomain>> GetAllowedDomainsByProductIdAsync(long productId, bool includeInactive = false, CancellationToken ct = default)
    {
        var sql = @"
            SELECT id, product_id AS ProductId, domain, description, is_active AS IsActive, created_at AS CreatedAt, created_by AS CreatedBy
            FROM product_external_research_domains
            WHERE product_id = @ProductId";

        if (!includeInactive)
            sql += " AND is_active = 1";

        sql += " ORDER BY domain ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ProductExternalResearchDomain>(sql, new { ProductId = productId });
        return rows.ToList();
    }

    public async Task<long> AddAllowedDomainAsync(ProductExternalResearchDomain domain, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_external_research_domains (product_id, domain, description, is_active, created_at, created_by)
            VALUES (@ProductId, @Domain, @Description, @IsActive, @CreatedAt, @CreatedBy);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, domain);
        domain.Id = id;
        return id;
    }

    public async Task<bool> RemoveAllowedDomainAsync(long domainId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var affected = await conn.ExecuteAsync("DELETE FROM product_external_research_domains WHERE id = @Id;", new { Id = domainId });
        return affected > 0;
    }
}
