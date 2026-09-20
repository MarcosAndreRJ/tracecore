using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.Llm;

/// <summary>
/// Fase 13 (M12): provedor generativo OpenAI (chat/completions) via HttpClient puro (DEV-AI-003).
/// A API key vem da configuração de ambiente (User Secrets / env), nunca do banco.
/// Fase 14: suporte a tool calling (OpenAI function calling).
/// </summary>
public class OpenAiLlmProvider : ILlmProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiLlmProvider(HttpClient httpClient, string apiKey, string modelName)
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? throw new ArgumentException("API Key da OpenAI é obrigatória.", nameof(apiKey));
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "gpt-4o-mini";
    }

    public string ProviderCode => "OpenAI";
    public string ModelName { get; }

    public async Task<LlmGenerationResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
    {
        var payload = BuildPayload(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao chamar a OpenAI ({ModelName}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        return ParseResponse(body);
    }

    private object BuildPayload(LlmGenerationRequest request)
    {
        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt },
            new { role = "user", content = request.UserPrompt }
        };

        if (request.PriorTurns != null)
        {
            foreach (var turn in request.PriorTurns)
            {
                if (string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new
                    {
                        role = "assistant",
                        content = turn.Text,
                        tool_calls = turn.ToolCalls?.Select(c => new
                        {
                            id = c.Id,
                            type = "function",
                            function = new { name = c.Name, arguments = c.ArgumentsJson }
                        }).ToArray()
                    });
                }
                else if (string.Equals(turn.Role, "tool", StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new
                    {
                        role = "tool",
                        tool_call_id = turn.ToolCallId,
                        content = turn.ToolResultJson ?? string.Empty
                    });
                }
            }
        }

        var payload = new
        {
            model = ModelName,
            max_tokens = request.MaxTokens,
            messages = messages,
            tools = request.Tools?.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = JsonDocument.Parse(t.ParametersJsonSchema).RootElement
                }
            }).ToArray(),
            tool_choice = request.Tools != null && request.Tools.Count > 0 ? "auto" : "none"
        };

        return payload;
    }

    private LlmGenerationResult ParseResponse(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        string text = string.Empty;
        var toolCalls = new List<LlmToolCall>();

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            var first = choices.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object)
            {
                if (first.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content) && content.ValueKind != JsonValueKind.Null)
                    {
                        text = content.GetString() ?? string.Empty;
                    }

                    if (message.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tc in toolCallsProp.EnumerateArray())
                        {
                            if (tc.TryGetProperty("function", out var func) &&
                                func.TryGetProperty("name", out var nameProp) &&
                                func.TryGetProperty("arguments", out var argsProp))
                            {
                                string argumentsJson = argsProp.ValueKind == JsonValueKind.String
                                    ? (argsProp.GetString() ?? "{}")
                                    : argsProp.GetRawText();

                                string? toolCallId = tc.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                                toolCalls.Add(new LlmToolCall(
                                    nameProp.GetString() ?? string.Empty,
                                    argumentsJson,
                                    toolCallId
                                ));
                            }
                        }
                    }
                }

                if (first.TryGetProperty("finish_reason", out var fr))
                {
                    // finish_reason can be "tool_calls", "stop", etc.
                }
            }
        }

        long? tokensUsed = null;
        if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var total))
        {
            tokensUsed = total.GetInt64();
        }

        string? finishReason = null;
        if (root.TryGetProperty("choices", out var choices2) && choices2.ValueKind == JsonValueKind.Array)
        {
            var first = choices2.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("finish_reason", out var fr))
            {
                finishReason = fr.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(text) && toolCalls.Count == 0)
        {
            throw new InvalidOperationException($"A OpenAI ({ModelName}) retornou resposta vazia.");
        }

        return new LlmGenerationResult(
            text.Trim(),
            tokensUsed,
            finishReason,
            toolCalls.Count > 0 ? toolCalls : null
        );
    }
}