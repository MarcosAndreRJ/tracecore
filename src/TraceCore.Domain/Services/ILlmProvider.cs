using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Definição de uma ferramenta disponível para o modelo chamar (JSON Schema).
/// </summary>
public record LlmToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema
);

/// <summary>
/// Uma chamada de ferramenta solicitada pelo modelo (nome + argumentos JSON).
/// </summary>
public record LlmToolCall(
    string Name,
    string ArgumentsJson
);

public record LlmGenerationRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens = 4096,
    IReadOnlyList<LlmToolDefinition>? Tools = null
);

public record LlmGenerationResult(
    string Text,
    long? TokensUsed = null,
    string? FinishReason = null,
    IReadOnlyList<LlmToolCall>? ToolCalls = null
);

/// <summary>
/// Fase 13 (M12): provedor de LLM (modelo generativo). A resposta alimenta o
/// copiloto RAG. Implementações concretas (Anthropic, OpenAI) vivem em
/// Infrastructure via HttpClient puro (DEV-AI-003), nunca SDK de terceiros.
/// </summary>
public interface ILlmProvider
{
    string ProviderCode { get; }
    string ModelName { get; }
    Task<LlmGenerationResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default);
}