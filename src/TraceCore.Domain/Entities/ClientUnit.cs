using System;

namespace TraceCore.Domain.Entities;

public class ClientUnit
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? ExternalCrmId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }

    public ClientUnit() { }

    public ClientUnit(long clientId, string code, string name, string status = "Active", string? externalCrmId = null, long? createdBy = null)
    {
        if (clientId <= 0)
            throw new ArgumentException("ClientId inválido.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código da unidade é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da unidade é obrigatório.", nameof(name));

        ClientId = clientId;
        Code = code.Trim();
        Name = name.Trim();
        Status = status;
        ExternalCrmId = string.IsNullOrWhiteSpace(externalCrmId) ? null : externalCrmId.Trim();
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}
