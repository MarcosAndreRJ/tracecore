using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 13 (M12): interação do usuário com o copiloto (pergunta + resposta oficial).
/// Base de BR-084 (citar fontes). Nunca é publicada automaticamente como conhecimento (BR-083).
/// </summary>
public class AiInteraction
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public string? ProviderCode { get; set; }
    public string? ModelName { get; set; }
    public long? TokensUsed { get; set; }
    public long? LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiInteraction() { }

    public AiInteraction(
        long? userId,
        string queryText,
        string responseText,
        string? providerCode,
        string? modelName,
        long? tokensUsed,
        long? latencyMs)
    {
        UserId = userId;
        QueryText = queryText ?? string.Empty;
        ResponseText = responseText ?? string.Empty;
        ProviderCode = providerCode;
        ModelName = modelName;
        TokensUsed = tokensUsed;
        LatencyMs = latencyMs;
        CreatedAt = DateTime.UtcNow;
    }
}