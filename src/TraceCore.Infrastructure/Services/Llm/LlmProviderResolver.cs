using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.Llm;

/// <summary>
/// Fase 13 (M12): resolve o provedor ativo por propósito (Generation/Embedding).
/// Fail-fast explícito (mesmo padrão de Persistence:Provider): sem provedor ativo
/// ou sem credencial configurada, lança erro claro em vez de mascarar o problema.
/// A definição de IServiceCollection para o HttpClient vem do AddHttpClient
/// registrado no AddInfrastructure (sem headers globais, cada request define o seu).
/// </summary>
public class LlmProviderResolver : ILlmProviderResolver
{
    private readonly ILlmProviderConfigRepository _configRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmProviderResolver(
        ILlmProviderConfigRepository configRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configRepository = configRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ILlmProvider> ResolveGenerationProviderAsync(CancellationToken ct = default)
    {
        var config = await _configRepository.GetActiveByPurposeAsync("Generation", ct)
            ?? throw new InvalidOperationException(
                "Nenhum provedor de geração ativo configurado (llm_provider_configs, purpose=Generation). " +
                "Ative um provedor na tela de Configurações.");

        var apiKey = GetApiKey(config.ProviderCode);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Provedor '{config.ProviderCode}' selecionado, mas sem API Key configurada " +
                $"(chave de ambiente 'Llm:{config.ProviderCode}:ApiKey'). " +
                "A abstração está pronta, mas o copiloto não será chamado sem credencial.");
        }

        return config.ProviderCode switch
        {
            "OpenAI" => new OpenAiLlmProvider(_httpClientFactory.CreateClient(), apiKey, config.ModelName),
            "Anthropic" => new AnthropicLlmProvider(_httpClientFactory.CreateClient(), apiKey, config.ModelName),
            _ => throw new InvalidOperationException(
                $"Provedor '{config.ProviderCode}' não é suportado pelo resolver. " +
                "Catálogo disponível: Anthropic, OpenAI.")
        };
    }

    public async Task<IEmbeddingProvider> ResolveEmbeddingProviderAsync(CancellationToken ct = default)
    {
        var config = await _configRepository.GetActiveByPurposeAsync("Embedding", ct)
            ?? throw new InvalidOperationException(
                "Nenhum provedor de embedding ativo configurado (llm_provider_configs, purpose=Embedding). " +
                "Ative um provedor na tela de Configurações.");

        var apiKey = GetApiKey(config.ProviderCode);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Provedor de embedding '{config.ProviderCode}' selecionado, mas sem API Key configurada " +
                $"(chave de ambiente 'Llm:{config.ProviderCode}:ApiKey'). " +
                "A abstração está pronta, mas embeddings não serão gerados sem credencial.");
        }

        return config.ProviderCode switch
        {
            "OpenAI" => new OpenAiEmbeddingProvider(_httpClientFactory.CreateClient(), apiKey, config.ModelName),
            "Anthropic" => new AnthropicEmbeddingProvider(_httpClientFactory.CreateClient(), apiKey, config.ModelName),
            _ => throw new InvalidOperationException(
                $"Provedor '{config.ProviderCode}' não é suportado pelo resolver. " +
                "Catálogo disponível: Anthropic, OpenAI.")
        };
    }

    public bool IsCredentialConfigured(string providerCode)
    {
        return !string.IsNullOrWhiteSpace(GetApiKey(providerCode));
    }

    private string? GetApiKey(string providerCode) => _configuration[$"Llm:{providerCode}:ApiKey"];
}