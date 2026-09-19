using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record RagSourceDto(
    long SearchableContentEntryId,
    string SourceType,
    string Title,
    string Url,
    int Rank,
    double SimilarityScore);

public record RagAnswerDto(
    long? InteractionId,
    string Question,
    string Answer,
    string? ProviderCode,
    string? ModelName,
    long? TokensUsed,
    long LatencyMs,
    IReadOnlyList<RagSourceDto> Sources,
    bool UsedContext,
    string? Notice,
    IReadOnlyList<ToolCallResult>? ToolResults = null);

public record AiFeedbackCommand(long InteractionId, bool Useful, string? Comment);

public record CreateAiDraftCommand(long InteractionId);

public record EmbeddingIndexingResultDto(
    int ProcessedCount,
    int FailedCount,
    string? ModelName,
    bool ProviderConfigured,
    string? Message);