using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Fase 13 (M12): feedback do usuário sobre uma resposta do copiloto (BR-086).
/// Armazenado para melhoria futura — NUNCA altera conhecimento, regras ou fluxos automaticamente.
/// </summary>
public class AiInteractionFeedback
{
    public long Id { get; set; }
    public long AiInteractionId { get; set; }
    public long? UserId { get; set; }
    public bool Useful { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiInteractionFeedback() { }

    public AiInteractionFeedback(
        long aiInteractionId,
        long? userId,
        bool useful,
        string? comment)
    {
        AiInteractionId = aiInteractionId;
        UserId = userId;
        Useful = useful;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }
}