using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Catálogo corporativo de causas raízes reutilizável entre casos (M04 / M08).
/// </summary>
public class RootCause
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RootCause() { }

    public RootCause(string name, string? code = null, string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da causa raiz é obrigatório.", nameof(name));

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
