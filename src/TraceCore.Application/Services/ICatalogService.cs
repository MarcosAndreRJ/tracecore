using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Application.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(long id, CancellationToken ct = default);
    Task<long> CreateProductAsync(string name, string? code, string? description, bool isExternal, CancellationToken ct = default);
    Task UpdateProductAsync(long id, string name, string? code, string? description, string status, bool isExternal, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default);
    Task<long> CreateVersionAsync(long productId, string versionLabel, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default);
    Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default);
    Task<long> CreateComponentAsync(string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, CancellationToken ct = default);
    Task UpdateComponentAsync(long id, string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, string status, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default);
    Task<long> AddComponentDependencyAsync(long sourceComponentId, long targetComponentId, string dependencyType, string criticality, string? description, CancellationToken ct = default);
    Task<bool> DeleteComponentDependencyAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default);
    Task<long> AddComponentOwnerAsync(long componentId, long departmentId, string ownershipRole, CancellationToken ct = default);
    Task<bool> DeleteComponentOwnerAsync(long id, CancellationToken ct = default);
}
