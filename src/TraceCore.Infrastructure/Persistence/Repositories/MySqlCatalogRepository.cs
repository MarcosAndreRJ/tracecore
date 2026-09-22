using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlCatalogRepository : ICatalogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCatalogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT id, code, name, description, status, is_external AS IsExternal, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products ORDER BY name ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<Product>(sql);
        return list.ToList();
    }

    public async Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default)
    {
        const string sql = "SELECT id, product_id AS ProductId, version_label AS VersionLabel, released_at AS ReleasedAt, end_of_support_at AS EndOfSupportAt, status FROM product_versions WHERE product_id = @ProductId ORDER BY version_label DESC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ProductVersion>(sql, new { ProductId = productId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<EnvironmentEntity>> GetAllEnvironmentsAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, environment_type AS EnvironmentType FROM environments ORDER BY name ASC;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<EnvironmentEntity>(sql);
        return list.ToList();
    }

    public async Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default)
    {
        var sql = "SELECT id, product_id AS ProductId, code, name, component_type AS ComponentType, description, owner_department_id AS OwnerDepartmentId, status, created_at AS CreatedAt, updated_at AS UpdatedAt FROM components";
        if (productId.HasValue)
        {
            sql += " WHERE product_id = @ProductId";
        }
        sql += " ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ComponentEntity>(sql, new { ProductId = productId });
        return list.ToList();
    }

    public async Task<Product?> GetProductByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, code, name, description, status, is_external AS IsExternal, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<ProductVersion?> GetProductVersionByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, product_id AS ProductId, version_label AS VersionLabel, released_at AS ReleasedAt, end_of_support_at AS EndOfSupportAt, status FROM product_versions WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ProductVersion>(sql, new { Id = id });
    }

    public async Task<EnvironmentEntity?> GetEnvironmentByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, name, environment_type AS EnvironmentType FROM environments WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<EnvironmentEntity>(sql, new { Id = id });
    }

    public async Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, product_id AS ProductId, code, name, component_type AS ComponentType, description, owner_department_id AS OwnerDepartmentId, status, created_at AS CreatedAt, updated_at AS UpdatedAt FROM components WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ComponentEntity>(sql, new { Id = id });
    }

    public async Task<long> AddProductAsync(Product product, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO products (code, name, description, status, is_external, created_at)
            VALUES (@Code, @Name, @Description, @Status, @IsExternal, @CreatedAt);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, product);
        product.Id = id;
        return id;
    }

    public async Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE products
            SET code = @Code,
                name = @Name,
                description = @Description,
                status = @Status,
                is_external = @IsExternal,
                updated_at = @UpdatedAt
            WHERE id = @Id;";
        product.UpdatedAt = System.DateTime.UtcNow;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, product);
    }

    public async Task<long> AddProductVersionAsync(ProductVersion version, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO product_versions (product_id, version_label, released_at, end_of_support_at, status)
            VALUES (@ProductId, @VersionLabel, @ReleasedAt, @EndOfSupportAt, @Status);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, version);
        version.Id = id;
        return id;
    }

    public async Task<long> AddEnvironmentAsync(EnvironmentEntity environment, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO environments (name, environment_type)
            VALUES (@Name, @EnvironmentType);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, environment);
        environment.Id = id;
        return id;
    }

    public async Task<long> AddComponentAsync(ComponentEntity component, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO components (product_id, code, name, component_type, description, owner_department_id, status, created_at)
            VALUES (@ProductId, @Code, @Name, @ComponentType, @Description, @OwnerDepartmentId, @Status, @CreatedAt);
            SELECT LAST_INSERT_ID();";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, component);
        component.Id = id;
        return id;
    }

    public async Task UpdateComponentAsync(ComponentEntity component, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE components
            SET product_id = @ProductId,
                code = @Code,
                name = @Name,
                component_type = @ComponentType,
                description = @Description,
                owner_department_id = @OwnerDepartmentId,
                status = @Status,
                updated_at = @UpdatedAt
            WHERE id = @Id;";
        component.UpdatedAt = System.DateTime.UtcNow;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, component);
    }

    public async Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                cd.id,
                cd.source_component_id AS SourceComponentId,
                cd.target_component_id AS TargetComponentId,
                cd.dependency_type AS DependencyType,
                cd.criticality AS Criticality,
                cd.description AS Description,
                cd.valid_from AS ValidFrom,
                cd.valid_to AS ValidTo,
                cd.created_at AS CreatedAt,
                sc.name AS SourceComponentName,
                tc.name AS TargetComponentName
            FROM component_dependencies cd
            INNER JOIN components sc ON cd.source_component_id = sc.id
            INNER JOIN components tc ON cd.target_component_id = tc.id";

        if (componentId.HasValue)
        {
            sql += " WHERE cd.source_component_id = @ComponentId OR cd.target_component_id = @ComponentId";
        }
        sql += " ORDER BY sc.name ASC, tc.name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ComponentDependency>(sql, new { ComponentId = componentId });
        return list.ToList();
    }

    public async Task<ComponentDependency?> GetComponentDependencyByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                cd.id,
                cd.source_component_id AS SourceComponentId,
                cd.target_component_id AS TargetComponentId,
                cd.dependency_type AS DependencyType,
                cd.criticality AS Criticality,
                cd.description AS Description,
                cd.valid_from AS ValidFrom,
                cd.valid_to AS ValidTo,
                cd.created_at AS CreatedAt,
                sc.name AS SourceComponentName,
                tc.name AS TargetComponentName
            FROM component_dependencies cd
            INNER JOIN components sc ON cd.source_component_id = sc.id
            INNER JOIN components tc ON cd.target_component_id = tc.id
            WHERE cd.id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ComponentDependency>(sql, new { Id = id });
    }

    public async Task<long> AddComponentDependencyAsync(ComponentDependency dependency, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO component_dependencies (source_component_id, target_component_id, dependency_type, criticality, description, valid_from, valid_to, created_at)
            VALUES (@SourceComponentId, @TargetComponentId, @DependencyType, @Criticality, @Description, @ValidFrom, @ValidTo, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, dependency);
        dependency.Id = id;
        return id;
    }

    public async Task<bool> DeleteComponentDependencyAsync(long id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM component_dependencies WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                co.id,
                co.component_id AS ComponentId,
                co.department_id AS DepartmentId,
                co.ownership_role AS OwnershipRole,
                co.valid_from AS ValidFrom,
                co.valid_to AS ValidTo,
                co.created_at AS CreatedAt,
                c.name AS ComponentName,
                d.name AS DepartmentName
            FROM component_owners co
            INNER JOIN components c ON co.component_id = c.id
            INNER JOIN departments d ON co.department_id = d.id";

        if (componentId.HasValue)
        {
            sql += " WHERE co.component_id = @ComponentId";
        }
        sql += " ORDER BY c.name ASC, co.ownership_role ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ComponentOwner>(sql, new { ComponentId = componentId });
        return list.ToList();
    }

    public async Task<long> AddComponentOwnerAsync(ComponentOwner owner, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO component_owners (component_id, department_id, ownership_role, valid_from, valid_to, created_at)
            VALUES (@ComponentId, @DepartmentId, @OwnershipRole, @ValidFrom, @ValidTo, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, owner);
        owner.Id = id;
        return id;
    }

    public async Task<bool> DeleteComponentOwnerAsync(long id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM component_owners WHERE id = @Id;";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<IReadOnlyList<ComponentType>> GetComponentTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id,
                code,
                name,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM component_types
            WHERE (@IncludeInactive = TRUE OR is_active = TRUE)
            ORDER BY name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<ComponentType>(sql, new { IncludeInactive = includeInactive });
        return list.ToList();
    }

    public async Task<ComponentType?> GetComponentTypeByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                id,
                code,
                name,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM component_types
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ComponentType>(sql, new { Id = id });
    }

    public async Task<long> AddComponentTypeAsync(ComponentType type, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO component_types (code, name, is_active, created_at)
            VALUES (@Code, @Name, @IsActive, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, type);
        type.Id = id;
        return id;
    }

    public async Task UpdateComponentTypeAsync(ComponentType type, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE component_types
            SET code = @Code,
                name = @Name,
                is_active = @IsActive
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, type);
    }
}

