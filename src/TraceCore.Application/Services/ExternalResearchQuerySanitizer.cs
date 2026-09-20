using System.Text.RegularExpressions;

namespace TraceCore.Application.Services;

public class ExternalResearchQuerySanitizer : IExternalResearchQuerySanitizer
{
    private const string Redacted = "[REDACTED]";

    // Bearer tokens / Authorization headers coladas na query.
    private static readonly Regex BearerToken = new(@"Bearer\s+[A-Za-z0-9\-_\.=]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // api_key=..., apikey: ..., token=...
    private static readonly Regex ApiKeyOrToken = new(@"(api[_-]?key|token|secret)\s*[:=]\s*\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // password=..., pwd=...
    private static readonly Regex PasswordAssignment = new(@"(password|pwd)\s*=\s*\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Fragmentos de connection string: Server=...;User Id=...;Uid=...;Data Source=...
    private static readonly Regex ConnectionStringFragment = new(
        @"(server|data source|user id|uid|initial catalog|database)\s*=\s*[^;]+;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // IPs privados/loopback/link-local — nunca fazem sentido numa busca pública.
    private static readonly Regex PrivateIp = new(
        @"\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|127\.\d{1,3}\.\d{1,3}\.\d{1,3}|169\.254\.\d{1,3}\.\d{1,3})\b",
        RegexOptions.Compiled);

    // E-mails (dado pessoal desnecessário para a busca).
    private static readonly Regex Email = new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);

    // CPF / CNPJ
    private static readonly Regex Cpf = new(@"\d{3}\.\d{3}\.\d{3}-\d{2}", RegexOptions.Compiled);
    private static readonly Regex Cnpj = new(@"\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}", RegexOptions.Compiled);

    // Hostnames/domínios claramente internos (sufixos comuns de rede corporativa).
    private static readonly Regex InternalHostname = new(
        @"\b[a-z0-9][a-z0-9\-]*\.(local|internal|corp|lan|intranet)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Sanitize(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery)) return string.Empty;

        var sanitized = rawQuery;

        sanitized = BearerToken.Replace(sanitized, Redacted);
        sanitized = ApiKeyOrToken.Replace(sanitized, Redacted);
        sanitized = PasswordAssignment.Replace(sanitized, Redacted);
        sanitized = ConnectionStringFragment.Replace(sanitized, Redacted);
        sanitized = PrivateIp.Replace(sanitized, Redacted);
        sanitized = InternalHostname.Replace(sanitized, Redacted);
        sanitized = Email.Replace(sanitized, Redacted);
        sanitized = Cnpj.Replace(sanitized, Redacted);
        sanitized = Cpf.Replace(sanitized, Redacted);

        return sanitized.Trim();
    }
}
