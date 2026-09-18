using System;

namespace TraceCore.Domain.Entities;

public class ClientTechnicalContext
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public long? ClientUnitId { get; set; }
    public long ProductId { get; set; }
    public long? ProductVersionId { get; set; }
    public long? EnvironmentId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }

    public ClientTechnicalContext() { }

    public ClientTechnicalContext(
        long clientId,
        long productId,
        long? clientUnitId = null,
        long? productVersionId = null,
        long? environmentId = null,
        string status = "Active",
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null,
        long? createdBy = null)
    {
        if (clientId <= 0)
            throw new ArgumentException("ClientId inválido.", nameof(clientId));
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));

        ClientId = clientId;
        ProductId = productId;
        ClientUnitId = clientUnitId;
        ProductVersionId = productVersionId;
        EnvironmentId = environmentId;
        Status = status;
        EffectiveFrom = effectiveFrom ?? DateTime.UtcNow;
        EffectiveTo = effectiveTo;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}
