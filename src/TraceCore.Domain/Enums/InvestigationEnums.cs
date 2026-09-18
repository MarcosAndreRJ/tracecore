namespace TraceCore.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de uma hipótese de investigação (BR-024).
/// NOTA DE ARQUITETURA: O valor 'Confirmed' NÃO é utilizado aqui.
/// Hipótese é diferente de causa raiz: causa raiz confirmada só ocorre na Fase 5 ao criar
/// 'case_resolutions' com 'root_cause_id' apontando para uma hipótese com status 'Supported'
/// (ver BR-024, BR-028 e Seção 1 das Decisões de Modelagem).
/// </summary>
public enum HypothesisStatus
{
    /// <summary>
    /// Hipótese levantada, ainda em investigação ativa (estado inicial).
    /// </summary>
    Proposed = 1,

    /// <summary>
    /// Descartada por teste ou evidência objetiva.
    /// </summary>
    Discarded = 2,

    /// <summary>
    /// Evidência aponta para ela como origem provável (possível origem identificada), mas ainda não é causa raiz confirmada.
    /// </summary>
    Supported = 3
}

/// <summary>
/// Classifica o resultado da AÇÃO/teste em si (a execução produziu um sinal claro e utilizável ou não),
/// e não diretamente a conclusão sobre a hipótese (BR-026).
/// </summary>
public enum DiagnosticStepOutcome
{
    Worked = 1,
    PartiallyWorked = 2,
    DidNotWork = 3,
    NotApplicable = 4,
    Inconclusive = 5
}

/// <summary>
/// Status da sessão de diagnóstico do caso.
/// </summary>
public enum DiagnosticSessionStatus
{
    Open = 1,
    Closed = 2
}

/// <summary>
/// Tipos de passos na linha de diagnóstico (catálogo aberto).
/// </summary>
public static class DiagnosticStepTypes
{
    public const string Verification = "Verification";
    public const string Attempt = "Attempt";
    public const string Observation = "Observation";
    public const string GuidedQuestion = "GuidedQuestion";
    public const string RecommendationIgnored = "RecommendationIgnored";
}

/// <summary>
/// Tipo de relação N:N entre uma evidência factual e uma hipótese técnica (Bloco 7.A.4).
/// </summary>
public enum EvidenceRelationType
{
    Supports = 1,
    Contradicts = 2,
    Inconclusive = 3,
    Confirms = 4
}
