using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.Llm;

/// <summary>
/// Fase 13 (M12): provedor de embedding OpenAI via HttpClient puro (DEV-AI-003).
/// A API key vem da configuração de ambiente (User Secrets / env), nunca do banco.
/// </summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/embeddings";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiEmbeddingProvider(HttpClient httpClient, string apiKey, string modelName)
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? throw new ArgumentException("API Key da OpenAI é obrigatória.", nameof(apiKey));
        ModelName = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim() : "text-embedding-3-small";
    }

    public string ProviderCode => "OpenAI";
    public string ModelName { get; }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            model = ModelName,
            input = text
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao chamar o embedding OpenAI ({ModelName}): {(int)response.StatusCode} {response.ReasonPhrase} — {body}");
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
                    throw new InvalidOperationException($"O embedding OpenAI ({ModelName}) retornou vetor vazio.");
                }

                return vector;
            }
        }

        throw new InvalidOperationException($"O embedding OpenAI ({ModelName}) retornou resposta sem vetor válido.");
    }
}