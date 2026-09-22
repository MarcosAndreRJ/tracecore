namespace TraceCore.Web.Helpers;

// Mapeia o score determinístico de casos semelhantes (0-100, ver CaseRelationService)
// para uma faixa visual (borda colorida dos cards), do menos para o mais provável de
// ser o mesmo caso: cinza (baixíssima) -> verde (boa) -> amarelo (alta) -> vermelho (altíssima).
public static class SimilarityDisplay
{
    public static string GetBorderClass(double? score)
    {
        var s = score ?? 0;
        if (s >= 75) return "border-danger";
        if (s >= 50) return "border-warning";
        if (s >= 25) return "border-success";
        return "border-secondary";
    }

    public static string GetLikelihoodLabel(double? score)
    {
        var s = score ?? 0;
        if (s >= 75) return "Altíssima probabilidade";
        if (s >= 50) return "Alta probabilidade";
        if (s >= 25) return "Boa probabilidade";
        return "Baixíssima probabilidade";
    }
}
