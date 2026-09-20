using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.ExternalResearch;

/// <summary>
/// Prompt 4 — provedor de pesquisa externa via HttpClient puro (mesmo padrão DEV-AI-003
/// dos provedores de LLM). Espera um endpoint REST simples: GET {BaseUrl}?q=...&count=...
/// devolvendo {"results":[{"title","url","snippet","publishedAt"}]} — contrato comum a
/// diversos provedores de busca (Tavily/Brave/SerpAPI-like); ajustar BaseUrl/mapeamento
/// se o provedor real usar um formato diferente, sem mudar a abstração
/// (IExternalResearchProvider) nem o restante do fluxo.
/// Nunca decide política (Disabled/OfficialOnly/AllowListed/OpenWeb) — isso é
/// responsabilidade exclusiva de IExternalResearchService (Application layer).
/// </summary>
public class HttpExternalResearchProvider : IExternalResearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly string? _apiKey;
    private readonly string _authType;
    private readonly IExternalUrlSafetyValidator _urlSafetyValidator;

    public HttpExternalResearchProvider(
        HttpClient httpClient,
        IExternalUrlSafetyValidator urlSafetyValidator,
        string? baseUrl,
        string? apiKey,
        string authType = "BearerApiKey",
        string providerCode = "ExternalResearch")
    {
        _httpClient = httpClient;
        _urlSafetyValidator = urlSafetyValidator;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _authType = string.IsNullOrWhiteSpace(authType) ? "BearerApiKey" : authType;
        ProviderCode = string.IsNullOrWhiteSpace(providerCode) ? "ExternalResearch" : providerCode;
    }

    public string ProviderCode { get; }

    // "Configurado" exige URL base — a API key pode ser opcional para provedores
    // sem autenticação (raro, mas o contrato não deve assumir credencial obrigatória).
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);

    public async Task<IReadOnlyList<ExternalSearchResult>> SearchAsync(string sanitizedQuery, int limit, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Provedor de pesquisa externa não configurado (ExternalResearch:BaseUrl ausente).");

        var url = $"{_baseUrl}?q={Uri.EscapeDataString(sanitizedQuery)}&count={limit}";
        var uri = new Uri(url);

        if (!await _urlSafetyValidator.IsSafeAsync(uri, ct))
        {
            throw new InvalidOperationException("O endpoint do provedor de pesquisa externa configurado não é um alvo seguro (bloqueado por proteção SSRF).");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddAuthentication(request);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Falha ao consultar o provedor de pesquisa externa ({ProviderCode}): {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return ParseResults(body, limit);
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return;

        switch (_authType)
        {
            case "HeaderApiKey":
                request.Headers.Add("x-api-key", _apiKey);
                break;
            case "BearerApiKey":
            default:
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                break;
        }
    }

    private static IReadOnlyList<ExternalSearchResult> ParseResults(string body, int limit)
    {
        var results = new List<ExternalSearchResult>();

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("results", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return results;

        var now = DateTime.UtcNow;

        foreach (var item in arr.EnumerateArray().Take(limit))
        {
            var titleProp = item.TryGetProperty("title", out var t) ? t.GetString() : null;
            var urlProp = item.TryGetProperty("url", out var u) ? u.GetString() : null;

            if (string.IsNullOrWhiteSpace(urlProp) || !Uri.TryCreate(urlProp, UriKind.Absolute, out var parsedUri))
                continue; // §75: nunca apresentar fonte sem URL real.

            var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() : null;

            DateTime? publishedAt = null;
            if (item.TryGetProperty("publishedAt", out var p) && p.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(p.GetString(), out var parsedDate))
            {
                publishedAt = parsedDate;
            }

            results.Add(new ExternalSearchResult(
                Title: string.IsNullOrWhiteSpace(titleProp) ? parsedUri.Host : titleProp!,
                Url: parsedUri.ToString(),
                Domain: parsedUri.Host,
                Snippet: snippet,
                PublishedAt: publishedAt,
                RetrievedAt: now));
        }

        return results;
    }
}
