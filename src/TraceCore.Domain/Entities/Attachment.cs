using System;

namespace TraceCore.Domain.Entities;

public class Attachment
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public ulong SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Confidentiality { get; set; } = "Internal";
    public long UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Attachment() { }

    public Attachment(
        string entityType,
        long entityId,
        string fileName,
        string mimeType,
        ulong sizeBytes,
        string sha256,
        string storageKey,
        long uploadedBy,
        string confidentiality = "Internal")
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType é obrigatório.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName é obrigatório.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MimeType é obrigatório.", nameof(mimeType));
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Sha256 é obrigatório.", nameof(sha256));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("StorageKey é obrigatório.", nameof(storageKey));

        EntityType = entityType.Trim();
        EntityId = entityId;
        FileName = fileName.Trim();
        MimeType = mimeType.Trim();
        SizeBytes = sizeBytes;
        Sha256 = sha256.Trim();
        StorageKey = storageKey.Trim();
        UploadedBy = uploadedBy;
        Confidentiality = confidentiality;
        UploadedAt = DateTime.UtcNow;
    }
}
