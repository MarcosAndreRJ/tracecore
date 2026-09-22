using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Catálogo administrável de tipos de componente (Fase 01 — Ajuste do Ecossistema).
/// Substitui a lista fixa hoje embutida no formulário de componentes da UI por uma
/// lista mantida em banco. O valor armazenado em <see cref="ComponentEntity.ComponentType"/>
/// continua sendo uma string aberta — este catálogo alimenta as opções da UI e permite
/// inativar tipos, sem quebrar compatibilidade com valores já gravados.
/// </summary>
public class ComponentType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ComponentType() { }

    public ComponentType(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código do tipo de componente é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do tipo de componente é obrigatório.", nameof(name));

        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Catálogo administrável de tipos de integração (Fase 01 — Ajuste do Ecossistema).
/// Substitui a lista fixa hoje embutida no formulário de integrações da UI. O valor
/// armazenado em <see cref="Integration.IntegrationType"/> continua sendo uma string
/// aberta — este catálogo alimenta as opções da UI e permite inativar tipos.
/// </summary>
public class IntegrationType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IntegrationType() { }

    public IntegrationType(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código do tipo de integração é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do tipo de integração é obrigatório.", nameof(name));

        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}