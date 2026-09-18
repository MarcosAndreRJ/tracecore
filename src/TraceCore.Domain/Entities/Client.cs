using System;

namespace TraceCore.Domain.Entities;

public class Client
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? ExternalCrmId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Client() { }

    public Client(string name, string? code = null, string status = "Active", string? externalCrmId = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do cliente é obrigatório.", nameof(name));

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Status = status;
        ExternalCrmId = string.IsNullOrWhiteSpace(externalCrmId) ? null : externalCrmId.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
