namespace TraceCore.Application.Services;

/// <summary>
/// Prompt 4, §21/§24/§25/§26 — sanitização OBRIGATÓRIA no backend antes de qualquer
/// query sair do TraceCore para um provedor externo. O modelo pode ajudar a formular
/// a busca, mas nunca é o único mecanismo de proteção — a limpeza real acontece aqui,
/// de forma determinística e testável.
/// </summary>
public interface IExternalResearchQuerySanitizer
{
    /// <summary>
    /// Remove dado sensível (credenciais, IP privado, PII, hostname interno) da query
    /// e devolve a versão mínima segura para envio externo (§26: mínima divulgação
    /// necessária). Nunca lança exceção — na pior hipótese, devolve uma query vazia.
    /// </summary>
    string Sanitize(string rawQuery);
}
