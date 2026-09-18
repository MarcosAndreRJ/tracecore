using System;

namespace TraceCore.Domain.Entities;

public class Product
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsExternal { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Product() { }

    public Product(string name, string? code = null, string? description = null, string status = "Active", bool isExternal = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do produto é obrigatório.", nameof(name));

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Description = description;
        Status = status;
        IsExternal = isExternal;
        CreatedAt = DateTime.UtcNow;
    }
}

public class ProductVersion
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public DateTime? ReleasedAt { get; set; }
    public DateTime? EndOfSupportAt { get; set; }
    public string Status { get; set; } = "Active";

    public ProductVersion() { }

    public ProductVersion(long productId, string versionLabel, string status = "Active")
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));
        if (string.IsNullOrWhiteSpace(versionLabel))
            throw new ArgumentException("Rótulo de versão é obrigatório.", nameof(versionLabel));

        ProductId = productId;
        VersionLabel = versionLabel.Trim();
        Status = status;
    }
}

public class EnvironmentEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnvironmentType { get; set; } = string.Empty;

    public EnvironmentEntity() { }

    public EnvironmentEntity(string name, string environmentType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do ambiente é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(environmentType))
            throw new ArgumentException("Tipo de ambiente é obrigatório.", nameof(environmentType));

        Name = name.Trim();
        EnvironmentType = environmentType.Trim();
    }
}

public class ComponentEntity
{
    public long Id { get; set; }
    public long? ProductId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? OwnerDepartmentId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ComponentEntity() { }

    public ComponentEntity(string name, string componentType, long? productId = null, string? code = null, string? description = null, long? ownerDepartmentId = null, string status = "Active")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do componente é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(componentType))
            throw new ArgumentException("Tipo do componente é obrigatório.", nameof(componentType));

        Name = name.Trim();
        ComponentType = componentType.Trim();
        ProductId = productId;
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Description = description;
        OwnerDepartmentId = ownerDepartmentId;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }
}

public class ComponentDependency
{
    public long Id { get; set; }
    public long SourceComponentId { get; set; }
    public long TargetComponentId { get; set; }
    public string DependencyType { get; set; } = "Synchronous"; // Synchronous, Asynchronous, Database, Messaging, External
    public string Criticality { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propriedades auxiliares para exibição (não mapeadas ou preenchidas por query)
    public string? SourceComponentName { get; set; }
    public string? TargetComponentName { get; set; }

    public ComponentDependency() { }

    public ComponentDependency(
        long sourceComponentId,
        long targetComponentId,
        string dependencyType,
        string criticality,
        string? description = null,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        if (sourceComponentId <= 0)
            throw new ArgumentException("SourceComponentId inválido.", nameof(sourceComponentId));
        if (targetComponentId <= 0)
            throw new ArgumentException("TargetComponentId inválido.", nameof(targetComponentId));
        if (sourceComponentId == targetComponentId)
            throw new InvalidOperationException("Um componente não pode ter dependência para si mesmo.");
        if (string.IsNullOrWhiteSpace(dependencyType))
            throw new ArgumentException("DependencyType é obrigatório.", nameof(dependencyType));
        if (string.IsNullOrWhiteSpace(criticality))
            throw new ArgumentException("Criticality é obrigatória.", nameof(criticality));

        SourceComponentId = sourceComponentId;
        TargetComponentId = targetComponentId;
        DependencyType = dependencyType.Trim();
        Criticality = criticality.Trim();
        Description = description?.Trim();
        ValidFrom = validFrom;
        ValidTo = validTo;
        CreatedAt = DateTime.UtcNow;
    }
}

public class ComponentOwner
{
    public long Id { get; set; }
    public long ComponentId { get; set; }
    public long DepartmentId { get; set; }
    public string OwnershipRole { get; set; } = "Primary"; // Primary, Secondary, Escalation
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propriedades auxiliares para exibição
    public string? ComponentName { get; set; }
    public string? DepartmentName { get; set; }

    public ComponentOwner() { }

    public ComponentOwner(
        long componentId,
        long departmentId,
        string ownershipRole = "Primary",
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        if (componentId <= 0)
            throw new ArgumentException("ComponentId inválido.", nameof(componentId));
        if (departmentId <= 0)
            throw new ArgumentException("DepartmentId inválido.", nameof(departmentId));
        if (string.IsNullOrWhiteSpace(ownershipRole))
            throw new ArgumentException("OwnershipRole é obrigatório.", nameof(ownershipRole));

        var role = ownershipRole.Trim();
        if (role != "Primary" && role != "Secondary" && role != "Escalation")
            throw new ArgumentException("OwnershipRole deve ser 'Primary', 'Secondary' ou 'Escalation'.", nameof(ownershipRole));

        ComponentId = componentId;
        DepartmentId = departmentId;
        OwnershipRole = role;
        ValidFrom = validFrom;
        ValidTo = validTo;
        CreatedAt = DateTime.UtcNow;
    }
}
