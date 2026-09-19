using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 13 (M12): configuração de provedor/modelo de IA por propósito.
/// A API key NUNCA é persistida no banco — mora em configuração de ambiente
/// (User Secrets em Development; env/secret store em Staging/Produção).
/// Somente uma configuração ativa por propósito (validado na aplicação).
/// </summary>
public class LlmProviderConfig
{
    public long Id { get; set; }
    public string Purpose { get; set; } = "Generation"; // Generation, Embedding
    public string ProviderCode { get; set; } = string.Empty; // Catálogo aberto: Anthropic, OpenAI
    public string ModelName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LlmProviderConfig() { }

    public LlmProviderConfig(
        string purpose,
        string providerCode,
        string modelName,
        bool isActive,
        long? createdBy = null)
    {
        Purpose = purpose?.Trim() ?? "Generation";
        ProviderCode = providerCode?.Trim() ?? throw new ArgumentException("ProviderCode é obrigatório.", nameof(providerCode));
        ModelName = modelName?.Trim() ?? throw new ArgumentException("ModelName é obrigatório.", nameof(modelName));
        IsActive = isActive;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}