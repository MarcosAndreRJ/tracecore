using System;
using System.Collections.Generic;

namespace TraceCore.Domain.Entities;

public class DiagnosticFlow
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntryKeywords { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DiagnosticFlowHypothesis> CandidateHypotheses { get; set; } = [];
    public List<DiagnosticCheck> Checks { get; set; } = [];

    public DiagnosticFlow() { }

    public DiagnosticFlow(
        string code,
        string name,
        string entryKeywords,
        string? description = null,
        string status = "Active",
        long? createdBy = null,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("O código do fluxo é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do fluxo é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(entryKeywords))
            throw new ArgumentException("As palavras-chave de entrada são obrigatórias.", nameof(entryKeywords));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        EntryKeywords = entryKeywords.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();
        CreatedBy = createdBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}

public class DiagnosticFlowHypothesis
{
    public long Id { get; set; }
    public long FlowId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? AssociatedComponentId { get; set; }

    public DiagnosticFlowHypothesis() { }

    public DiagnosticFlowHypothesis(long flowId, string title, string? description = null, long? associatedComponentId = null)
    {
        if (flowId <= 0)
            throw new ArgumentException("FlowId inválido.", nameof(flowId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título da hipótese é obrigatório.", nameof(title));

        FlowId = flowId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AssociatedComponentId = associatedComponentId;
    }
}

public class DiagnosticCheck
{
    public long Id { get; set; }
    public long FlowId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string CheckType { get; set; } = "Question"; // "Question", "AutomatedCheck" (reservado para M10)
    public int Cost { get; set; } = 1;
    public string RiskLevel { get; set; } = "Low"; // "Low", "Medium", "High"
    public string? SkipConditionField { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DiagnosticCheckOption> Options { get; set; } = [];

    public DiagnosticCheck() { }

    public DiagnosticCheck(
        long flowId,
        string code,
        string title,
        string questionText,
        string checkType = "Question",
        int cost = 1,
        string riskLevel = "Low",
        string? skipConditionField = null,
        DateTime? createdAt = null)
    {
        if (flowId <= 0)
            throw new ArgumentException("FlowId inválido.", nameof(flowId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código do check é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título do check é obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Texto da pergunta é obrigatório.", nameof(questionText));

        FlowId = flowId;
        Code = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        QuestionText = questionText.Trim();
        CheckType = string.IsNullOrWhiteSpace(checkType) ? "Question" : checkType.Trim();
        Cost = cost > 0 ? cost : 1;
        RiskLevel = string.IsNullOrWhiteSpace(riskLevel) ? "Low" : riskLevel.Trim();
        SkipConditionField = string.IsNullOrWhiteSpace(skipConditionField) ? null : skipConditionField.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}

public class DiagnosticCheckOption
{
    public long Id { get; set; }
    public long CheckId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OrderNo { get; set; } = 1;

    public List<DiagnosticCheckImpact> Impacts { get; set; } = [];

    public DiagnosticCheckOption() { }

    public DiagnosticCheckOption(long checkId, string optionText, int orderNo = 1)
    {
        if (checkId <= 0)
            throw new ArgumentException("CheckId inválido.", nameof(checkId));
        if (string.IsNullOrWhiteSpace(optionText))
            throw new ArgumentException("Texto da opção é obrigatório.", nameof(optionText));

        CheckId = checkId;
        OptionText = optionText.Trim();
        OrderNo = orderNo;
    }
}

public class DiagnosticCheckImpact
{
    public long Id { get; set; }
    public long CheckOptionId { get; set; }
    public long FlowHypothesisId { get; set; }
    public string ImpactType { get; set; } = "Favors"; // "Favors", "Discards"
    public decimal Weight { get; set; } = 1.0m;

    public DiagnosticCheckImpact() { }

    public DiagnosticCheckImpact(long checkOptionId, long flowHypothesisId, string impactType, decimal weight = 1.0m)
    {
        if (checkOptionId <= 0)
            throw new ArgumentException("CheckOptionId inválido.", nameof(checkOptionId));
        if (flowHypothesisId <= 0)
            throw new ArgumentException("FlowHypothesisId inválido.", nameof(flowHypothesisId));
        if (string.IsNullOrWhiteSpace(impactType))
            throw new ArgumentException("ImpactType é obrigatório.", nameof(impactType));

        CheckOptionId = checkOptionId;
        FlowHypothesisId = flowHypothesisId;
        ImpactType = impactType.Trim();
        Weight = weight > 0 ? weight : 1.0m;
    }
}
