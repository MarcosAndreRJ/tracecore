using System;
using System.Security.Cryptography;
using System.Text;

namespace TraceCore.Domain.Entities;

/// <summary>
/// BR-041, BR-042, BR-044, BR-045: Versão lógica do conteúdo do conhecimento.
/// Modificações em conteúdo já publicado criam obrigatoriamente nova versão.
/// </summary>
public class KnowledgeVersion
{
    public long Id { get; set; }
    public long KnowledgeItemId { get; set; }
    public int VersionNo { get; set; }
    public string ContentMarkdown { get; set; } = string.Empty;
    public string? ProblemDescription { get; set; }
    public string? RootCauseSummary { get; set; }
    public string? ValidationMethod { get; set; }
    public string? RiskWarning { get; set; }
    public string? RollbackPlan { get; set; }
    public string? ChangeSummary { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, InReview, Approved, Superseded, Deprecated
    public string ContentHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedBy { get; set; }

    public KnowledgeVersion() { }

    public KnowledgeVersion(
        long knowledgeItemId,
        int versionNo,
        string contentMarkdown,
        long createdBy,
        string? problemDescription = null,
        string? rootCauseSummary = null,
        string? validationMethod = null,
        string? riskWarning = null,
        string? rollbackPlan = null,
        string? changeSummary = null)
    {
        if (knowledgeItemId <= 0)
            throw new ArgumentException("KnowledgeItemId inválido.", nameof(knowledgeItemId));
        if (versionNo <= 0)
            throw new ArgumentException("VersionNo deve ser maior que zero.", nameof(versionNo));

        KnowledgeItemId = knowledgeItemId;
        VersionNo = versionNo;
        ContentMarkdown = contentMarkdown ?? string.Empty;
        CreatedBy = createdBy;
        ProblemDescription = problemDescription;
        RootCauseSummary = rootCauseSummary;
        ValidationMethod = validationMethod;
        RiskWarning = riskWarning;
        RollbackPlan = rollbackPlan;
        ChangeSummary = changeSummary;
        Status = "Draft";
        CreatedAt = DateTime.UtcNow;
        ContentHash = ComputeContentHash(ContentMarkdown);
    }

    public void Approve(long approvedBy, DateTime approvedAt)
    {
        Status = "Approved";
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    public void Supersede()
    {
        Status = "Superseded";
    }

    public void Deprecate()
    {
        Status = "Deprecated";
    }

    public static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
