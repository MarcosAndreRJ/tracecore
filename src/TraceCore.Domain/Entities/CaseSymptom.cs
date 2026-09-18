using System;

namespace TraceCore.Domain.Entities;

public class CaseSymptom
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public string? SymptomCode { get; set; }
    public string SymptomText { get; set; } = string.Empty;
    public string Source { get; set; } = "Human";
    public bool Confirmed { get; set; } = true;

    public CaseSymptom() { }

    public CaseSymptom(long caseId, string symptomText, string? symptomCode = null, string source = "Human", bool confirmed = true)
    {
        if (string.IsNullOrWhiteSpace(symptomText))
            throw new ArgumentException("Texto do sintoma é obrigatório.", nameof(symptomText));

        CaseId = caseId;
        SymptomText = symptomText.Trim();
        SymptomCode = string.IsNullOrWhiteSpace(symptomCode) ? null : symptomCode.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? "Human" : source.Trim();
        Confirmed = confirmed;
    }
}
