namespace TraceCore.Domain.Entities;

public class Permission
{
    public long Id { get; set; }
    // Convenção: português, 'dominio.acao' em lowercase (ex.: caso.visualizar, usuario.gerenciar)
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Permission() { }

    public Permission(string code, string description)
    {
        Code = code.Trim().ToLowerInvariant();
        Description = description;
    }
}
