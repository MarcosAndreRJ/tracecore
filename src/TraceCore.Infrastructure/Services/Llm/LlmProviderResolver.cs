using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services.Llm;

/// <summary>
/// Fase 17: Resolve o provedor ativo por propósito (Generation/Embedding) usando a nova arquitetura Provider/Protocol/Model.
/// Fail-fast explícito: sem configuração ativa ou sem credencial (quando necessária), lança erro claro.
///
/// Precedência de credencial (GetApiKeyAsync):
///   1. ISecretStore — chave "llm_apikey_{providerCode}" (configurada pela UI)
///   2. IConfiguration — "Llm:{providerCode}:ApiKey" (User Secrets / env — fallback)
///   3. null: fail-fast com mensagem clara (se AuthenticationType != None)
/// </summary>
public class LlmProviderResolver : ILlmProviderResolver
{
    private readonly ILlmModelConfigRepository _modelConfigRepository;
    private readonly ILlmProviderRepository _providerRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecretStore _secretStore;

    public LlmProviderResolver(
        ILlmModelConfigRepository modelConfigRepository,
        ILlmProviderRepository providerRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ISecretStore secretStore)
    {
        _modelConfigRepository = modelConfigRepository;
        _providerRepository = providerRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _secretStore = secretStore;
    }

    public async Task<ILlmProvider> ResolveGenerationProviderAsync(CancellationToken ct = default)
    {
        var modelConfig = await _modelConfigRepository.GetActiveByPurposeAsync("Generation", ct)
            ?? throw new InvalidOperationException(
                "Nenhuma configuração de modelo de geração ativa (llm_model_configs, purpose=Generation). " +
                "Configure um provedor e modelo na tela Configuracoes -> Inteligencia Artificial.");

        var provider = await _providerRepository.GetByIdAsync(modelConfig.ProviderId, ct)
            ?? throw new InvalidOperationException(
                $"Provedor ID {modelConfig.ProviderId} referenciado pela configuração não encontrado.");

        var apiKey = await GetApiKeyAsync(provider.Code, ct);
        if (provider.AuthenticationType != LlmAuthenticationTypes.None && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Provedor '{provider.Name}' ({provider.Code}) selecionado, mas sem API Key configurada. " +
                "Configure a credencial na tela Configuracoes -> Inteligencia Artificial.");
        }

        return CreateGenerationProvider(provider, modelConfig.ModelName, apiKey);
    }

    public async Task<IEmbeddingProvider> ResolveEmbeddingProviderAsync(CancellationToken ct = default)
    {
        var modelConfig = await _modelConfigRepository.GetActiveByPurposeAsync("Embedding", ct)
            ?? throw new InvalidOperationException(
                "Nenhuma configuração de modelo de embedding ativa (llm_model_configs, purpose=Embedding). " +
                "Configure um provedor e modelo na tela Configuracoes -> Inteligencia Artificial.");

        var provider = await _providerRepository.GetByIdAsync(modelConfig.ProviderId, ct)
            ?? throw new InvalidOperationException(
                $"Provedor ID {modelConfig.ProviderId} referenciado pela configuração não encontrado.");

        if (!provider.HasEmbeddingCapability)
        {
            throw new InvalidOperationException(
                $"Provedor '{provider.Name}' ({provider.Code}) não possui capacidade de Embedding. " +
                "Selecione um provedor com essa capacidade ou desative essa configuração.");
        }

        var apiKey = await GetApiKeyAsync(provider.Code, ct);
        if (provider.AuthenticationType != LlmAuthenticationTypes.None && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Provedor de embedding '{provider.Name}' ({provider.Code}) selecionado, mas sem API Key configurada. " +
                "Configure a credencial na tela Configuracoes -> Inteligencia Artificial.");
        }

        return CreateEmbeddingProvider(provider, modelConfig.ModelName, apiKey);
    }

    /// <summary>
    /// Testa conexão para uma configuração de modelo específica (llm_model_configs).
    /// Usado pela camada Application sem referenciar tipos concretos de provedor.
    /// </summary>
    public async Task<ConnectionTestResult> TestModelConfigConnectionAsync(long modelConfigId, CancellationToken ct = default)
    {
        var modelConfig = await _modelConfigRepository.GetByIdAsync(modelConfigId, ct)
            ?? throw new InvalidOperationException($"Configuração de modelo ID {modelConfigId} não encontrada.");

        var provider = await _providerRepository.GetByIdAsync(modelConfig.ProviderId, ct)
            ?? throw new InvalidOperationException($"Provedor ID {modelConfig.ProviderId} não encontrado.");

        if (provider.AuthenticationType != LlmAuthenticationTypes.None)
        {
            var hasCredential = await _secretStore.ExistsAsync(SecretKey(provider.Code), ct)
                || IsCredentialConfigured(provider.Code);
            if (!hasCredential)
                return new ConnectionTestResult(false,
                    $"Credencial ausente para provedor '{provider.Name}' ({provider.Code}). Configure a API Key primeiro.");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            if (modelConfig.Purpose.Equals("Generation", StringComparison.OrdinalIgnoreCase))
            {
                var genProvider = CreateGenerationProvider(provider, modelConfig.ModelName, await GetApiKeyAsync(provider.Code, cts.Token));
                var result = await genProvider.GenerateAsync(
                    new Domain.Services.LlmGenerationRequest(
                        SystemPrompt: "Responda apenas: OK",
                        UserPrompt: "Ping",
                        MaxTokens: 64),
                    cts.Token);
                return new ConnectionTestResult(true,
                    $"Conexão bem-sucedida com {provider.Name} ({provider.Code}) - modelo {modelConfig.ModelName}.");
            }
            else if (modelConfig.Purpose.Equals("Embedding", StringComparison.OrdinalIgnoreCase))
            {
                if (!provider.HasEmbeddingCapability)
                    return new ConnectionTestResult(false, $"Provedor '{provider.Name}' não possui capability de Embedding.");

                var embProvider = CreateEmbeddingProvider(provider, modelConfig.ModelName, await GetApiKeyAsync(provider.Code, cts.Token));
                var result = await embProvider.EmbedAsync("test", cts.Token);
                return new ConnectionTestResult(true,
                    $"Conexão bem-sucedida com {provider.Name} ({provider.Code}) - modelo {modelConfig.ModelName}, vetor de {result.Length} dimensões.");
            }
            else
            {
                return new ConnectionTestResult(false, $"Purpose '{modelConfig.Purpose}' inválido.");
            }
        }
        catch (OperationCanceledException)
        {
            return new ConnectionTestResult(false, "Timeout: o provedor não respondeu em 15 segundos.");
        }
        catch (InvalidOperationException ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
        catch (Exception)
        {
            return new ConnectionTestResult(false,
                $"Falha ao conectar com provedor '{provider.Name}'. Verifique a configuração e tente novamente.");
        }
    }

    /// <summary>
    /// Testa conexão legado (pelo providerCode e purpose) — compatibilidade.
    /// </summary>
    public async Task<ConnectionTestResult> TestLegacyConnectionAsync(string providerCode, string purpose, CancellationToken ct = default)
    {
        var hasCredential = await _secretStore.ExistsAsync(SecretKey(providerCode), ct)
            || IsCredentialConfigured(providerCode);

        if (!hasCredential)
            return new ConnectionTestResult(false,
                $"Credencial ausente para '{providerCode}'. Configure a API Key primeiro.");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            if (purpose.Equals("Generation", StringComparison.OrdinalIgnoreCase))
            {
                var provider = await ResolveGenerationProviderAsync(cts.Token);
                var result = await provider.GenerateAsync(
                    new Domain.Services.LlmGenerationRequest(
                        SystemPrompt: "Responda apenas: OK",
                        UserPrompt: "Ping",
                        MaxTokens: 64),
                    cts.Token);
                return new ConnectionTestResult(true,
                    $"Conexão bem-sucedida com {providerCode} ({provider.ModelName}).");
            }
            else if (purpose.Equals("Embedding", StringComparison.OrdinalIgnoreCase))
            {
                var provider = await ResolveEmbeddingProviderAsync(cts.Token);
                var result = await provider.EmbedAsync("test", cts.Token);
                return new ConnectionTestResult(true,
                    $"Conexão bem-sucedida com {providerCode} ({provider.ModelName}), " +
                    $"vetor de {result.Length} dimensões.");
            }
            else
            {
                return new ConnectionTestResult(false, $"Purpose '{purpose}' inválido. Use Generation ou Embedding.");
            }
        }
        catch (OperationCanceledException)
        {
            return new ConnectionTestResult(false, "Timeout: o provedor não respondeu em 15 segundos.");
        }
        catch (InvalidOperationException ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
        catch (Exception)
        {
            return new ConnectionTestResult(false,
                $"Falha ao conectar com '{providerCode}'. Verifique a credencial e tente novamente.");
        }
    }

    private ILlmProvider CreateGenerationProvider(LlmProvider provider, string modelName, string? apiKey)
    {
        return provider.Protocol switch
        {
            LlmProtocols.OpenAICompatible => new OpenAiCompatibleLlmProvider(
                _httpClientFactory.CreateClient(), provider.BaseUrl, modelName, apiKey, provider.AuthenticationType, provider.Code),
            LlmProtocols.AnthropicMessages => new AnthropicLlmProvider(
                _httpClientFactory.CreateClient(), provider.BaseUrl, modelName, apiKey ?? string.Empty, provider.Code),
            _ => throw new InvalidOperationException(
                $"Protocolo '{provider.Protocol}' não suportado para geração. Protocolos: {string.Join(", ", LlmProtocols.All)}")
        };
    }

    private IEmbeddingProvider CreateEmbeddingProvider(LlmProvider provider, string modelName, string? apiKey)
    {
        return provider.Protocol switch
        {
            LlmProtocols.OpenAICompatible => new OpenAiCompatibleEmbeddingProvider(
                _httpClientFactory.CreateClient(), provider.BaseUrl, modelName, apiKey, provider.AuthenticationType, provider.Code),
            LlmProtocols.AnthropicMessages => new AnthropicEmbeddingProvider(
                _httpClientFactory.CreateClient(), provider.BaseUrl, modelName, apiKey ?? string.Empty, provider.Code),
            _ => throw new InvalidOperationException(
                $"Protocolo '{provider.Protocol}' não suportado para embedding. Protocolos: {string.Join(", ", LlmProtocols.All)}")
        };
    }

    /// <summary>
    /// Verifica se há credencial configurada para o providerCode.
    /// Precedência: ISecretStore => IConfiguration.
    /// </summary>
    public bool IsCredentialConfigured(string providerCode)
    {
        var fromStore = _secretStore.ExistsAsync(SecretKey(providerCode)).GetAwaiter().GetResult();
        if (fromStore) return true;
        return !string.IsNullOrWhiteSpace(_configuration[$"Llm:{providerCode}:ApiKey"]);
    }

    /// <summary>
    /// Recupera a API Key com precedência: ISecretStore => IConfiguration.
    /// NUNCA loga ou expõe o valor retornado.
    /// </summary>
    private async Task<string?> GetApiKeyAsync(string providerCode, CancellationToken ct)
    {
        var fromStore = await _secretStore.GetSecretAsync(SecretKey(providerCode), ct);
        if (!string.IsNullOrWhiteSpace(fromStore))
            return fromStore;
        return _configuration[$"Llm:{providerCode}:ApiKey"];
    }

    private static string SecretKey(string providerCode) => $"llm_apikey_{providerCode}";
}
