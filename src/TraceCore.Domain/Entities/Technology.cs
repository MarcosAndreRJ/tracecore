using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Catálogo de tecnologia corporativa associável a itens de conhecimento.
/// </summary>
public class Technology
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Technology() { }

    public Technology(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da tecnologia é obrigatório.", nameof(name));

        Name = name.Trim();
    }
}
