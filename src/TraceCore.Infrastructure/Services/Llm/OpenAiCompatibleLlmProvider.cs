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
/// Fase 17: Adapter genérico para provedores compatíveis com OpenAI Chat Completions API.
/// Permite usar OpenAI, DeepSeek, OpenRouter, Groq, Mistral, Ollama, LM Studio, etc.
/// sem criar classes específicas para cada fornecedor.
/// </summary>
public class OpenAiCompatibleLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _authType;

    public OpenAiCompatibleLlmProvider(
        HttpClient httpClient,
        string baseUrl,
        string modelName,
        string? apiKey = null,
        string authenticationType = "BearerApiKey",
        string providerCode = "OpenAICompatible")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentException("BaseUrl é obrigatório.", nameof(baseUrl));
        _apiKey = apiKey ?? string.Empty;
        _authType = authenticationType ?? "BearerApiKey";
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "gpt-4o-mini";
        ProviderCode = !string.IsNullOrWhiteSpace(providerCode) ? providerCode : "OpenAICompatible";
    }

    public string ProviderCode { get; }
    public string ModelName { get; }

    public async Task<LlmGenerationResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt },
            new { role = "user", content = request.UserPrompt }
        };

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

        var url = _baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? _baseUrl
            : $"{_baseUrl}/chat/completions";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthentication(httpRequest);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao chamar provedor OpenAI-compatible ({ModelName} @ {_baseUrl}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        return ParseOpenAiResponse(body);
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        switch (_authType)
        {
            case "BearerApiKey":
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                break;
            case "HeaderApiKey":
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Add("x-api-key", _apiKey);
                break;
            case "None":
            default:
                break;
        }
    }

    private LlmGenerationResult ParseOpenAiResponse(string body)
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
                                toolCalls.Add(new LlmToolCall(
                                    nameProp.GetString() ?? string.Empty,
                                    argsProp.GetRawText()
                                ));
                            }
                        }
                    }
                }

                if (first.TryGetProperty("finish_reason", out var fr))
                {
                    // finish_reason pode ser "tool_calls", "stop", etc.
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
            throw new InvalidOperationException($"Provedor OpenAI-compatible ({ModelName} @ {_baseUrl}) retornou resposta vazia.");
        }

        return new LlmGenerationResult(
            text.Trim(),
            tokensUsed,
            finishReason,
            toolCalls.Count > 0 ? toolCalls : null
        );
    }
}