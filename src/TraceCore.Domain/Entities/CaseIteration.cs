using System;

namespace TraceCore.Domain.Entities;

public class CaseIteration
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public long OpenedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = "Open"; // Open, Resolved

    public CaseIteration() { }

    public CaseIteration(long caseId, int sequenceNumber, long openedBy, string? reason = null, DateTime? openedAt = null, string status = "Open")
    {
        if (caseId < 0)
            throw new ArgumentException("CaseId inválido.", nameof(caseId));
        if (sequenceNumber <= 0)
            throw new ArgumentException("SequenceNumber inválido.", nameof(sequenceNumber));
        if (openedBy <= 0)
            throw new ArgumentException("OpenedBy inválido.", nameof(openedBy));

        CaseId = caseId;
        SequenceNumber = sequenceNumber;
        OpenedBy = openedBy;
        Reason = reason?.Trim();
        OpenedAt = openedAt ?? DateTime.UtcNow;
        Status = string.IsNullOrWhiteSpace(status) ? "Open" : status.Trim();
    }
}
