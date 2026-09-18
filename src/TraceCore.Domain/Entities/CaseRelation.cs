using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TraceCore.Domain.Entities;

public enum CaseRelationType
{
    Similar,
    Duplicate,
    Recurrence,
    CommonCause,
    Dependency,
    Reference
}

public class CaseRelation
{
    public long Id { get; set; }
    public long SourceCaseId { get; set; }
    public long TargetCaseId { get; set; }
    public string RelationType { get; set; } = CaseRelationType.Similar.ToString();
    public double? SimilarityScore { get; set; }
    public string? MatchedFactorsJson { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propriedade calculada de fatores
    public List<string> MatchedFactors
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MatchedFactorsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(MatchedFactorsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            MatchedFactorsJson = value != null ? JsonSerializer.Serialize(value) : null;
        }
    }

    public CaseRelation() { }

    public CaseRelation(
        long sourceCaseId,
        long targetCaseId,
        CaseRelationType relationType,
        double? similarityScore = null,
        IEnumerable<string>? matchedFactors = null,
        long? createdBy = null)
    {
        if (sourceCaseId <= 0)
            throw new ArgumentException("SourceCaseId deve ser maior que zero.", nameof(sourceCaseId));

        if (targetCaseId <= 0)
            throw new ArgumentException("TargetCaseId deve ser maior que zero.", nameof(targetCaseId));

        if (sourceCaseId == targetCaseId)
            throw new InvalidOperationException("Um caso não pode ser relacionado consigo mesmo (autorreferência bloqueada).");

        SourceCaseId = sourceCaseId;
        TargetCaseId = targetCaseId;
        RelationType = relationType.ToString();
        SimilarityScore = similarityScore;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;

        if (matchedFactors != null)
        {
            MatchedFactors = new List<string>(matchedFactors);
        }
    }
}
