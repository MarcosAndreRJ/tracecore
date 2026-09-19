using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 17 (refatorado): Provedor de IA cadastrado pelo administrador.
/// Representa uma instância administrativa (ex: "OpenAI Produção", "Ollama Local", "DeepSeek").
/// O protocolo define COMO o TraceCore conversa com o endpoint.
/// </summary>
public class LlmProvider
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;          // Ex: "OpenAI Produção", "Ollama Local"
    public string Code { get; set; } = string.Empty;          // Identificador único: "openai-prod", "ollama-local"
    public string Protocol { get; set; } = string.Empty;      // "OpenAICompatible", "AnthropicMessages"
    public string BaseUrl { get; set; } = string.Empty;       // Ex: "https://api.openai.com/v1", "http://localhost:11434/v1"
    public string AuthenticationType { get; set; } = "BearerApiKey"; // "None", "BearerApiKey", "HeaderApiKey"
    public bool HasGenerationCapability { get; set; }         // Suporta geração de texto
    public bool HasEmbeddingCapability { get; set; }          // Suporta embeddings
    public string Status { get; set; } = "Active";            // "Active", "Inactive"
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LlmProvider() { }

    public LlmProvider(
        string name,
        string code,
        string protocol,
        string baseUrl,
        string authenticationType,
        bool hasGenerationCapability,
        bool hasEmbeddingCapability,
        long? createdBy = null)
    {
        Name = name?.Trim() ?? throw new ArgumentException("Name é obrigatório.", nameof(name));
        Code = code?.Trim().ToLowerInvariant() ?? throw new ArgumentException("Code é obrigatório.", nameof(code));
        Protocol = protocol?.Trim() ?? throw new ArgumentException("Protocol é obrigatório.", nameof(protocol));
        BaseUrl = baseUrl?.Trim() ?? throw new ArgumentException("BaseUrl é obrigatório.", nameof(baseUrl));
        AuthenticationType = authenticationType?.Trim() ?? "BearerApiKey";
        HasGenerationCapability = hasGenerationCapability;
        HasEmbeddingCapability = hasEmbeddingCapability;
        Status = "Active";
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string protocol,
        string baseUrl,
        string authenticationType,
        bool hasGenerationCapability,
        bool hasEmbeddingCapability,
        string status,
        long? updatedBy = null)
    {
        Name = name?.Trim() ?? throw new ArgumentException("Name é obrigatório.", nameof(name));
        Protocol = protocol?.Trim() ?? throw new ArgumentException("Protocol é obrigatório.", nameof(protocol));
        BaseUrl = baseUrl?.Trim() ?? throw new ArgumentException("BaseUrl é obrigatório.", nameof(baseUrl));
        AuthenticationType = authenticationType?.Trim() ?? "BearerApiKey";
        HasGenerationCapability = hasGenerationCapability;
        HasEmbeddingCapability = hasEmbeddingCapability;
        Status = status?.Trim() ?? "Active";
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Fase 17 (refatorado): Configuração de uso de modelo por propósito (Generation/Embedding).
/// Aponta para um provedor cadastrado e define qual modelo utilizar.
/// Substitui a antiga LlmProviderConfig (que misturava provedor com configuração de uso).
/// </summary>
public class LlmModelConfig
{
    public long Id { get; set; }
    public string Purpose { get; set; } = "Generation"; // "Generation", "Embedding"
    public long ProviderId { get; set; }                // FK para llm_providers
    public string ModelName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LlmModelConfig() { }

    public LlmModelConfig(
        string purpose,
        long providerId,
        string modelName,
        bool isActive,
        long? createdBy = null)
    {
        Purpose = purpose?.Trim() ?? "Generation";
        ProviderId = providerId;
        ModelName = modelName?.Trim() ?? throw new ArgumentException("ModelName é obrigatório.", nameof(modelName));
        IsActive = isActive;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Constantes para protocolos suportados.
/// </summary>
public static class LlmProtocols
{
    public const string OpenAICompatible = "OpenAICompatible";
    public const string AnthropicMessages = "AnthropicMessages";
    
    public static readonly IReadOnlyList<string> All = new[] { OpenAICompatible, AnthropicMessages };
}

/// <summary>
/// Constantes para tipos de autenticação.
/// </summary>
public static class LlmAuthenticationTypes
{
    public const string None = "None";
    public const string BearerApiKey = "BearerApiKey";
    public const string HeaderApiKey = "HeaderApiKey";
    
    public static readonly IReadOnlyList<string> All = new[] { None, BearerApiKey, HeaderApiKey };
}