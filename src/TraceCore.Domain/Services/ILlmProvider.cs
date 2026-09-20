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
/// Id é o identificador que o provedor atribui à chamada (ex.: Anthropic tool_use.id,
/// OpenAI tool_calls[].id) — necessário para montar o turno de resultado (tool_result/
/// role:"tool") de volta ao provedor em uma conversa multi-turno (Prompt 3).
/// </summary>
public record LlmToolCall(
    string Name,
    string ArgumentsJson,
    string? Id = null
);

/// <summary>
/// Um turno já ocorrido na conversa, usado para orquestração multi-turno de
/// tool-calling (Prompt 3 — Copiloto investigativo): o serviço chamador gera uma
/// resposta, executa as tool calls solicitadas e volta a chamar GenerateAsync
/// passando os turnos anteriores em PriorTurns, para que o modelo possa decidir a
/// próxima ação com base nos resultados reais — sem isso, cada chamada seria uma
/// conversa nova e o modelo não veria o resultado da ferramenta anterior.
/// Role: "assistant" (o modelo pediu tool calls, com ou sem texto) ou "tool"
/// (resultado de uma tool call específica, identificada por ToolCallId).
/// </summary>
public record LlmConversationTurn(
    string Role,
    string? Text = null,
    IReadOnlyList<LlmToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? ToolResultJson = null,
    bool ToolResultIsError = false
);

public record LlmGenerationRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens = 4096,
    IReadOnlyList<LlmToolDefinition>? Tools = null,
    IReadOnlyList<LlmConversationTurn>? PriorTurns = null
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