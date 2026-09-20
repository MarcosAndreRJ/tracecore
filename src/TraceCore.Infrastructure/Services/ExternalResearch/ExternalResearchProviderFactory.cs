using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.ExternalResearch;

// IExternalResearchProviderFactory (contrato) vive em TraceCore.Domain.Services,
// mesmo padrão de ILlmProviderResolver — Application depende só de Domain,
// nunca de Infrastructure.

public class ExternalResearchProviderFactory : IExternalResearchProviderFactory
{
    private const string SecretKey = "external_research_apikey";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ISecretStore _secretStore;
    private readonly IExternalUrlSafetyValidator _urlSafetyValidator;

    public ExternalResearchProviderFactory(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ISecretStore secretStore,
        IExternalUrlSafetyValidator urlSafetyValidator)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _secretStore = secretStore;
        _urlSafetyValidator = urlSafetyValidator;
    }

    public async Task<IExternalResearchProvider> GetProviderAsync(CancellationToken ct = default)
    {
        var providerCode = _configuration["ExternalResearch:Provider"] ?? "ExternalResearch";
        var baseUrl = _configuration["ExternalResearch:BaseUrl"];
        var authType = _configuration["ExternalResearch:AuthType"] ?? "BearerApiKey";

        var apiKey = await _secretStore.GetSecretAsync(SecretKey, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["ExternalResearch:ApiKey"];
        }

        var httpClient = _httpClientFactory.CreateClient("ExternalResearch");

        return new HttpExternalResearchProvider(httpClient, _urlSafetyValidator, baseUrl, apiKey, authType, providerCode);
    }
}
