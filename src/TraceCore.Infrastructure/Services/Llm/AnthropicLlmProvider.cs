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
/// Fase 13 (M12): provedor generativo Anthropic (Claude) via HttpClient puro (DEV-AI-003).
/// A API key vem da configuração de ambiente (User Secrets / env), nunca do banco.
/// Fase 14: suporte a tool calling (Anthropic Messages API tool_use/tool_result).
/// </summary>
public class AnthropicLlmProvider : ILlmProvider
{
    private const string BaseUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AnthropicLlmProvider(HttpClient httpClient, string apiKey, string modelName)
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? throw new ArgumentException("API Key da Anthropic é obrigatória.", nameof(apiKey));
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "claude-sonnet-4-5-20250929";
    }

    public string ProviderCode => "Anthropic";
    public string ModelName { get; }

    public async Task<LlmGenerationResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
    {
        var payload = BuildPayload(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao chamar a Anthropic ({ModelName}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        return ParseResponse(body);
    }

    private object BuildPayload(LlmGenerationRequest request)
    {
        var messages = new List<object>
        {
            new { role = "user", content = request.UserPrompt }
        };

        var payload = new
        {
            model = ModelName,
            max_tokens = request.MaxTokens,
            system = request.SystemPrompt,
            messages = messages,
            tools = request.Tools?.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = JsonDocument.Parse(t.ParametersJsonSchema).RootElement
            }).ToArray()
        };

        return payload;
    }

    private LlmGenerationResult ParseResponse(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        string text = string.Empty;
        var toolCalls = new List<LlmToolCall>();

        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type))
                {
                    var typeStr = type.GetString();
                    if (typeStr == "text" && block.TryGetProperty("text", out var textProp))
                    {
                        text += textProp.GetString();
                    }
                    else if (typeStr == "tool_use" &&
                             block.TryGetProperty("name", out var nameProp) &&
                             block.TryGetProperty("input", out var inputProp))
                    {
                        toolCalls.Add(new LlmToolCall(
                            nameProp.GetString() ?? string.Empty,
                            JsonSerializer.Serialize(inputProp)
                        ));
                    }
                }
            }
        }

        long? tokensUsed = null;
        if (root.TryGetProperty("usage", out var usage))
        {
            long input = 0, output = 0;
            if (usage.TryGetProperty("input_tokens", out var inTok)) input = inTok.GetInt64();
            if (usage.TryGetProperty("output_tokens", out var outTok)) output = outTok.GetInt64();
            tokensUsed = input + output;
        }

        string? finishReason = null;
        if (root.TryGetProperty("stop_reason", out var sr))
        {
            finishReason = sr.GetString();
        }

        if (string.IsNullOrWhiteSpace(text) && toolCalls.Count == 0)
        {
            throw new InvalidOperationException($"A Anthropic ({ModelName}) retornou resposta vazia.");
        }

        return new LlmGenerationResult(
            text.Trim(),
            tokensUsed,
            finishReason,
            toolCalls.Count > 0 ? toolCalls : null
        );
    }
}