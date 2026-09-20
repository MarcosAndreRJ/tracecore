using System;
using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record ExternalResearchResultDto(
    string Title,
    string Url,
    string Domain,
    string? Snippet,
    string TrustLevel,
    DateTime? PublishedAt,
    DateTime RetrievedAt
);

/// <summary>
/// Resultado de uma tentativa de pesquisa externa (Prompt 4). Nunca lança exceção
/// para "provider não configurado" ou "policy não permite" — expõe isso em
/// <see cref="Rejected"/>/<see cref="RejectionReason"/> para o chamador decidir como
/// comunicar, mantendo o restante da investigação funcionando (§62/§92/§94).
/// </summary>
public record ExternalResearchOutcomeDto(
    bool Rejected,
    string? RejectionReason,
    string PolicyApplied,
    IReadOnlyList<ExternalResearchResultDto> Results,
    string SanitizedQuery
);
