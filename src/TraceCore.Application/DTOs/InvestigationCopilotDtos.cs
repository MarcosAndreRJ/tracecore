using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

/// <summary>
/// Interpretação estruturada do relato do usuário (Prompt 3, §7). Extraída pelo LLM
/// numa chamada sem ferramentas — o backend nunca aceita IDs vindos do modelo
/// (§8): ClientText/ProductText são nomes em texto livre, resolvidos para IDs reais
/// só depois, no backend.
/// </summary>
public record InvestigationIntent(
    string? ClientText,
    string? ProductText,
    IReadOnlyList<string> Symptoms,
    IReadOnlyList<string> ErrorCodes,
    string? ComponentHint,
    string? VersionHint,
    string? EnvironmentHint
);

/// <summary>Resultado da resolução de nomes em texto livre para IDs reais do catálogo (§8/§9).</summary>
public record ResolvedEntity(long? Id, string? Name, bool Ambiguous, IReadOnlyList<string> Candidates);

public record RetrievedCaseDto(
    long CaseId,
    string CaseDisplay,
    string? ClientName,
    string? ProductName,
    string? VersionLabel,
    string Status,
    string Severity,
    string? Summary,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> Components,
    string? ResolutionSummary,
    string? RootCauseSummary,
    double? MatchScore,
    IReadOnlyList<string> MatchedFactors
);

public record RetrievedKnowledgeDto(
    long KnowledgeItemId,
    string KnowledgeCode,
    string Title,
    string Summary,
    string Status
);

/// <summary>Fonte externa (Prompt 4) efetivamente usada na resposta — sempre rotulada DOCUMENTAÇÃO EXTERNA.</summary>
public record RetrievedExternalSourceDto(
    string Title,
    string Url,
    string Domain,
    string TrustLevel
);

/// <summary>
/// Resposta estruturada do Copiloto investigativo (§93). Nunca uma única string —
/// quem consome (a página) decide como apresentar cada parte, com rótulo de origem
/// explícito (HISTÓRICO INTERNO / SOLUÇÃO VALIDADA / CONTEXTO TÉCNICO / HIPÓTESE DA IA).
/// </summary>
public record InvestigationCopilotAnswerDto(
    long? InteractionId,
    string Question,
    string Answer,
    IReadOnlyList<RetrievedCaseDto> RelatedCases,
    IReadOnlyList<RetrievedKnowledgeDto> RelatedKnowledge,
    IReadOnlyList<RetrievedExternalSourceDto> ExternalSources,
    bool UsedTechnicalContext,
    IReadOnlyList<string> RetrievalStrategies,
    IReadOnlyList<string> Warnings,
    int ToolCallCount,
    string? ProviderCode,
    string? ModelName,
    long? LatencyMs
);
