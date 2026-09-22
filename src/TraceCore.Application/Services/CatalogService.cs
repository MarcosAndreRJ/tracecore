using System;
using System.Collections.Generic;
using System.Linq;
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

    public async Task<long> CreateProductAsync(string name, string? code, string? description, bool isExternal, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));

        var product = new Product(name, code, description, "Active", isExternal);
        var id = await _catalogRepository.AddProductAsync(product, ct);
        product.Id = id;

        await _auditService.RecordAsync(
            action: "product.create",
            entityType: "products",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { product.Id, product.Name, product.Code, product.Description, product.Status, product.IsExternal },
            ct: ct
        );

        return id;
    }

    public async Task UpdateProductAsync(long id, string name, string? code, string? description, string status, bool isExternal, long? currentUserId = null, CancellationToken ct = default)
    {
        var product = await _catalogRepository.GetProductByIdAsync(id, ct);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {id} não encontrado.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));

        var before = new { product.Id, product.Name, product.Code, product.Description, product.Status, product.IsExternal };

        product.Name = name.Trim();
        product.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        product.Description = description?.Trim();
        product.Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();
        product.IsExternal = isExternal;

        await _catalogRepository.UpdateProductAsync(product, ct);

        await _auditService.RecordAsync(
            action: "product.update",
            entityType: "products",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { product.Id, product.Name, product.Code, product.Description, product.Status, product.IsExternal },
            ct: ct
        );
    }

    public async Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default)
    {
        return await _catalogRepository.GetVersionsByProductIdAsync(productId, ct);
    }

    public async Task<long> CreateVersionAsync(long productId, string versionLabel, DateTime? releasedAt = null, long? currentUserId = null, CancellationToken ct = default)
    {
        var product = await _catalogRepository.GetProductByIdAsync(productId, ct);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");

        var version = new ProductVersion(productId, versionLabel) { ReleasedAt = releasedAt };
        var id = await _catalogRepository.AddProductVersionAsync(version, ct);

        await _auditService.RecordAsync(
            action: "product_version.create",
            entityType: "product_versions",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { Id = id, ProductId = productId, VersionLabel = versionLabel, ReleasedAt = releasedAt },
            ct: ct
        );

        return id;
    }

    public async Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetAllComponentsAsync(productId, ct);
    }

    public async Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentByIdAsync(id, ct);
    }

    public async Task<long> CreateComponentAsync(string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, long? currentUserId = null, CancellationToken ct = default)
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
        var id = await _catalogRepository.AddComponentAsync(component, ct);
        component.Id = id;

        await _auditService.RecordAsync(
            action: "component.create",
            entityType: "components",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { component.Id, component.Name, component.ComponentType, component.ProductId, component.Code, component.OwnerDepartmentId, component.Status },
            ct: ct
        );

        return id;
    }

    public async Task UpdateComponentAsync(long id, string name, string componentType, long? productId, string? code, string? description, long? ownerDepartmentId, string status, long? currentUserId = null, CancellationToken ct = default)
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

        var before = new { component.Id, component.Name, component.ComponentType, component.ProductId, component.Code, component.OwnerDepartmentId, component.Status };

        component.Name = name.Trim();
        component.ComponentType = componentType.Trim();
        component.ProductId = productId;
        component.Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        component.Description = description?.Trim();
        component.OwnerDepartmentId = ownerDepartmentId;
        component.Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();

        await _catalogRepository.UpdateComponentAsync(component, ct);

        await _auditService.RecordAsync(
            action: "component.update",
            entityType: "components",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { component.Id, component.Name, component.ComponentType, component.ProductId, component.Code, component.OwnerDepartmentId, component.Status },
            ct: ct
        );
    }

    public async Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentDependenciesAsync(componentId, ct);
    }

    public async Task<long> AddComponentDependencyAsync(long sourceComponentId, long targetComponentId, string dependencyType, string criticality, string? description, long? currentUserId = null, CancellationToken ct = default)
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
        var id = await _catalogRepository.AddComponentDependencyAsync(dependency, ct);

        await _auditService.RecordAsync(
            action: "component.dependency_create",
            entityType: "component_dependencies",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { Id = id, SourceComponentId = sourceComponentId, TargetComponentId = targetComponentId, DependencyType = dependencyType, Criticality = criticality },
            ct: ct
        );

        return id;
    }

    public async Task<bool> DeleteComponentDependencyAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var deleted = await _catalogRepository.DeleteComponentDependencyAsync(id, ct);
        if (deleted)
        {
            await _auditService.RecordAsync(
                action: "component.dependency_delete",
                entityType: "component_dependencies",
                entityId: id.ToString(),
                actorUserId: currentUserId,
                ct: ct
            );
        }
        return deleted;
    }

    public async Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentOwnersAsync(componentId, ct);
    }

    public async Task<long> AddComponentOwnerAsync(long componentId, long departmentId, string ownershipRole, long? currentUserId = null, CancellationToken ct = default)
    {
        var component = await _catalogRepository.GetComponentByIdAsync(componentId, ct);
        if (component == null)
            throw new KeyNotFoundException($"Componente com ID {componentId} não encontrado.");

        var dept = await _departmentRepository.GetByIdAsync(departmentId, ct);
        if (dept == null)
            throw new KeyNotFoundException($"Departamento com ID {departmentId} não encontrado.");

        var owner = new ComponentOwner(componentId, departmentId, ownershipRole);
        var id = await _catalogRepository.AddComponentOwnerAsync(owner, ct);

        await _auditService.RecordAsync(
            action: "component.owner_add",
            entityType: "component_owners",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { Id = id, ComponentId = componentId, DepartmentId = departmentId, OwnershipRole = ownershipRole },
            ct: ct
        );

        return id;
    }

    public async Task<bool> DeleteComponentOwnerAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var deleted = await _catalogRepository.DeleteComponentOwnerAsync(id, ct);
        if (deleted)
        {
            await _auditService.RecordAsync(
                action: "component.owner_delete",
                entityType: "component_owners",
                entityId: id.ToString(),
                actorUserId: currentUserId,
                ct: ct
            );
        }
        return deleted;
    }

    public async Task<IReadOnlyList<ComponentType>> GetComponentTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        return await _catalogRepository.GetComponentTypesAsync(includeInactive, ct);
    }

    public async Task<long> CreateComponentTypeAsync(string code, string name, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("O código do tipo de componente é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do tipo de componente é obrigatório.", nameof(name));

        var type = new ComponentType(code, name);
        var id = await _catalogRepository.AddComponentTypeAsync(type, ct);
        type.Id = id;

        await _auditService.RecordAsync(
            action: "component_type.create",
            entityType: "component_types",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { type.Id, type.Code, type.Name, type.IsActive },
            ct: ct
        );

        return id;
    }

    public async Task DeactivateComponentTypeAsync(long id, long? currentUserId = null, CancellationToken ct = default)
    {
        var type = await _catalogRepository.GetComponentTypeByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Tipo de componente com ID {id} não encontrado.");

        if (!type.IsActive)
            return;

        if (await IsComponentTypeInUseAsync(type, ct))
        {
            throw new InvalidOperationException(
                $"O tipo de componente '{type.Name}' não pode ser inativado porque já está em uso por componentes cadastrados.");
        }

        var before = new { type.Id, type.Code, type.Name, type.IsActive };
        type.IsActive = false;
        await _catalogRepository.UpdateComponentTypeAsync(type, ct);

        await _auditService.RecordAsync(
            action: "component_type.deactivate",
            entityType: "component_types",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { type.Id, type.Code, type.Name, type.IsActive },
            ct: ct
        );
    }

    private async Task<bool> IsComponentTypeInUseAsync(ComponentType type, CancellationToken ct)
    {
        var components = await _catalogRepository.GetAllComponentsAsync(ct: ct);
        return components.Any(c => string.Equals(c.ComponentType, type.Code, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(c.ComponentType, type.Name, StringComparison.OrdinalIgnoreCase));
    }
}
