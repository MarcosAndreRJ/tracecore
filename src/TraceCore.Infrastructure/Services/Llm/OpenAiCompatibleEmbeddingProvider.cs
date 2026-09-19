using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.Llm;

/// <summary>
/// Fase 17: Adapter genérico para provedores de embedding compatíveis com OpenAI Embeddings API.
/// Permite usar OpenAI, Ollama, etc. para embeddings.
/// </summary>
public class OpenAiCompatibleEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _authType;

    public OpenAiCompatibleEmbeddingProvider(
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
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "text-embedding-3-small";
        ProviderCode = !string.IsNullOrWhiteSpace(providerCode) ? providerCode : "OpenAICompatible";
    }

    public string ProviderCode { get; }
    public string ModelName { get; }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            model = ModelName,
            input = text
        };

        var url = _baseUrl.EndsWith("/embeddings", StringComparison.OrdinalIgnoreCase)
            ? _baseUrl
            : $"{_baseUrl}/embeddings";

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
                $"Falha ao chamar embedding OpenAI-compatible ({ModelName} @ {_baseUrl}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            var first = data.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("embedding", out var embedding))
            {
                var vector = new float[embedding.GetArrayLength()];
                int i = 0;
                foreach (var item in embedding.EnumerateArray())
                {
                    vector[i++] = item.GetSingle();
                }

                if (vector.Length == 0)
                {
                    throw new InvalidOperationException($"O embedding OpenAI-compatible ({ModelName} @ {_baseUrl}) retornou vetor vazio.");
                }

                return vector;
            }
        }

        throw new InvalidOperationException($"O embedding OpenAI-compatible ({ModelName} @ {_baseUrl}) retornou resposta sem vetor válido.");
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
}