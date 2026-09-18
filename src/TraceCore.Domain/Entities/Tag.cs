using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Tag/palavra-chave livre para indexação e taxonomia orgânica de conhecimento.
/// </summary>
public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Tag() { }

    public Tag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da tag é obrigatório.", nameof(name));

        Name = name.Trim().ToLowerInvariant();
    }
}
