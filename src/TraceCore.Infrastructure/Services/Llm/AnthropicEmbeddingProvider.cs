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
/// Fase 13 (M12): provedor de embedding Anthropic (Voyage) via HttpClient puro (DEV-AI-003).
/// A API key vem da configuração de ambiente (User Secrets / env), nunca do banco.
/// </summary>
public class AnthropicEmbeddingProvider : IEmbeddingProvider
{
    private const string BaseUrl = "https://api.anthropic.com/v1/embeddings";
    private const string ApiVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AnthropicEmbeddingProvider(HttpClient httpClient, string apiKey, string modelName)
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? throw new ArgumentException("API Key da Anthropic é obrigatória.", nameof(apiKey));
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "voyage-3";
    }

    public string ProviderCode => "Anthropic";
    public string ModelName { get; }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            model = ModelName,
            input = new[] { text }
        };

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
                $"Falha ao chamar o embedding Anthropic ({ModelName}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (root.TryGetProperty("embeddings", out var embeddings) && embeddings.ValueKind == JsonValueKind.Array)
        {
            var first = embeddings.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Array)
            {
                var vector = new float[first.GetArrayLength()];
                int i = 0;
                foreach (var item in first.EnumerateArray())
                {
                    vector[i++] = item.GetSingle();
                }

                if (vector.Length == 0)
                {
                    throw new InvalidOperationException($"O embedding Anthropic ({ModelName}) retornou vetor vazio.");
                }

                return vector;
            }
        }

        throw new InvalidOperationException($"O embedding Anthropic ({ModelName}) retornou resposta sem vetor válido.");
    }
}