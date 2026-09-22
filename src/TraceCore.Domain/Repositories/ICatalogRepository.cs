using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<EnvironmentEntity>> GetAllEnvironmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default);

    Task<Product?> GetProductByIdAsync(long id, CancellationToken ct = default);
    Task<ProductVersion?> GetProductVersionByIdAsync(long id, CancellationToken ct = default);
    Task<EnvironmentEntity?> GetEnvironmentByIdAsync(long id, CancellationToken ct = default);
    Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default);

    Task<long> AddProductAsync(Product product, CancellationToken ct = default);
    Task<long> AddProductVersionAsync(ProductVersion version, CancellationToken ct = default);
    Task<long> AddEnvironmentAsync(EnvironmentEntity environment, CancellationToken ct = default);
    Task<long> AddComponentAsync(ComponentEntity component, CancellationToken ct = default);

    Task UpdateProductAsync(Product product, CancellationToken ct = default);
    Task UpdateComponentAsync(ComponentEntity component, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default);
    Task<ComponentDependency?> GetComponentDependencyByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddComponentDependencyAsync(ComponentDependency dependency, CancellationToken ct = default);
    Task<bool> DeleteComponentDependencyAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default);
    Task<long> AddComponentOwnerAsync(ComponentOwner owner, CancellationToken ct = default);
    Task<bool> DeleteComponentOwnerAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<ComponentType>> GetComponentTypesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ComponentType?> GetComponentTypeByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddComponentTypeAsync(ComponentType type, CancellationToken ct = default);
    Task UpdateComponentTypeAsync(ComponentType type, CancellationToken ct = default);
}

