using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditService _auditService;

    public CatalogService(
        ICatalogRepository catalogRepository,
        IDepartmentRepository departmentRepository,
        IAuditService auditService)
    {
        _catalogRepository = catalogRepository;
        _departmentRepository = departmentRepository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        return await _catalogRepository.GetAllProductsAsync(ct);
    }

    public async Task<Product?> GetProductByIdAsync(long id, CancellationToken ct = default)
    {
        return await _catalogRepository.GetProductByIdAsync(id, ct);
    }

    public async Task<long> CreateProductAsync(string name, string? code, string? description, bool isExternal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));

        var product = new Product(name, code, description, "Active", isExternal);
        var id = await _catalogRepository.AddProductAsync(product, ct);
        return id;
    }

    public async Task UpdateProductAsync(long id, string name, string? code, string? description, string status, bool isExternal, CancellationToken ct = default)
    {
        var product = await _catalogRepository.GetProductByIdAsync(id, ct);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {id} não encontrado.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));

        product.Name = name.Trim();
        product.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        product.Description = description?.Trim();
        product.Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();
        product.IsExternal = isExternal;

        await _catalogRepository.UpdateProductAsync(product, ct);
    }

    public async Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default)
    {
        return await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
    }

    public async Task<long> CreateVersionAsync(long productId, string versionLabel, CancellationToken ct = default)
    {
        var product = await _catalogRepository.GetProductByIdAsync(productId, ct);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        var version = new ProductVersion(productId, versionLabel);
        return await _catalogRepository.AddProductVersionAsync(version, ct);
    }

    public async Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetAllComponentsAsync(productId, ct);
    }

    public async Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentByIdAsync(id, ct);
    }

    public async Task<long> CreateComponentAsync(string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do componente é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(componentType))
            throw new ArgumentException("O tipo de componente é obrigatório.", nameof(componentType));

        if (ownerDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(ownerDepartmentId.Value, ct);
            if (dept == null)
                throw new ArgumentException($"Departamento com ID {ownerDepartmentId.Value} não existe.", nameof(ownerDepartmentId));
        }

        var component = new ComponentEntity(name, componentType, productId, code, description, ownerDepartmentId);
        return await _catalogRepository.AddComponentAsync(component, ct);
    }

    public async Task UpdateComponentAsync(long id, string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, string status, CancellationToken ct = default)
    {
        var component = await _catalogRepository.GetComponentByIdAsync(id, ct);
        if (component == null)
            throw new KeyNotFoundException($"Componente com ID {id} não encontrado.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do componente é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(componentType))
            throw new ArgumentException("O tipo de componente é obrigatório.", nameof(componentType));

        if (ownerDepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(ownerDepartmentId.Value, ct);
            if (dept == null)
                throw new ArgumentException($"Departamento com ID {ownerDepartmentId.Value} não existe.", nameof(ownerDepartmentId));
        }

        component.Name = name.Trim();
        component.ComponentType = componentType.Trim();
        component.ProductId = productId;
        component.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        component.Description = description?.Trim();
        component.OwnerDepartmentId = ownerDepartmentId;
        component.Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();

        await _catalogRepository.UpdateComponentAsync(component, ct);
    }

    public async Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentDependenciesAsync(componentId, ct);
    }

    public async Task<long> AddComponentDependencyAsync(long sourceComponentId, long targetComponentId, string dependencyType, string criticality, string? description, CancellationToken ct = default)
    {
        if (sourceComponentId == targetComponentId)
            throw new InvalidOperationException("Um componente não pode depender de si mesmo.");

        var source = await _catalogRepository.GetComponentByIdAsync(sourceComponentId, ct);
        if (source == null)
            throw new KeyNotFoundException($"Componente de origem com ID {sourceComponentId} não encontrado.");

        var target = await _catalogRepository.GetComponentByIdAsync(targetComponentId, ct);
        if (target == null)
            throw new KeyNotFoundException($"Componente de destino com ID {targetComponentId} não encontrado.");

        var dependency = new ComponentDependency(sourceComponentId, targetComponentId, dependencyType, criticality, description);
        return await _catalogRepository.AddComponentDependencyAsync(dependency, ct);
    }

    public async Task<bool> DeleteComponentDependencyAsync(long id, CancellationToken ct = default)
    {
        return await _catalogRepository.DeleteComponentDependencyAsync(id, ct);
    }

    public async Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentOwnersAsync(componentId, ct);
    }

    public async Task<long> AddComponentOwnerAsync(long componentId, long departmentId, string ownershipRole, CancellationToken ct = default)
    {
        var component = await _catalogRepository.GetComponentByIdAsync(componentId, ct);
        if (component == null)
            throw new KeyNotFoundException($"Componente com ID {componentId} não encontrado.");

        var dept = await _departmentRepository.GetByIdAsync(departmentId, ct);
        if (dept == null)
            throw new KeyNotFoundException($"Departamento com ID {departmentId} não encontrado.");

        var owner = new ComponentOwner(componentId, departmentId, ownershipRole);
        return await _catalogRepository.AddComponentOwnerAsync(owner, ct);
    }

    public async Task<bool> DeleteComponentOwnerAsync(long id, CancellationToken ct = default)
    {
        return await _catalogRepository.DeleteComponentOwnerAsync(id, ct);
    }
}
