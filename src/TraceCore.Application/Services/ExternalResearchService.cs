using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public class ExternalResearchService : IExternalResearchService
{
    private const int MaxResultsCap = 5;

    // §8: sem ProductTechnicalSource cadastrada com TrustLevel=Official, aplica um
    // critério claro e determinístico (nunca "o modelo acha que é oficial") — lista
    // curada de domínios de documentação oficial amplamente reconhecidos, os mesmos
    // exemplos citados nos Prompts 2/4.
    private static readonly HashSet<string> WellKnownOfficialDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "learn.microsoft.com", "docs.microsoft.com", "dev.mysql.com", "docs.oracle.com",
        "docs.docker.com", "kubernetes.io", "docs.aws.amazon.com", "cloud.google.com",
        "nodejs.org", "docs.python.org", "docs.github.com", "developer.mozilla.org"
    };

    private readonly IProductTechnicalContextService _technicalContextService;
    private readonly IExternalResearchQuerySanitizer _sanitizer;
    private readonly IExternalResearchProviderFactory _providerFactory;
    private readonly IAuditService _auditService;

    public ExternalResearchService(
        IProductTechnicalContextService technicalContextService,
        IExternalResearchQuerySanitizer sanitizer,
        IExternalResearchProviderFactory providerFactory,
        IAuditService auditService)
    {
        _technicalContextService = technicalContextService;
        _sanitizer = sanitizer;
        _providerFactory = providerFactory;
        _auditService = auditService;
    }

    public async Task<ExternalResearchOutcomeDto> SearchAsync(long productId, string rawQuery, int maxResults, long? userId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        maxResults = Math.Clamp(maxResults <= 0 ? MaxResultsCap : maxResults, 1, MaxResultsCap);

        var context = await _technicalContextService.GetInvestigationContextAsync(productId, null, ct);
        var policy = context?.ExternalResearchPolicy ?? "Disabled";

        var sanitizedQuery = _sanitizer.Sanitize(rawQuery);

        // §6: policy Disabled — o backend recusa mesmo que o LLM peça. Nenhuma chamada
        // externa acontece, independentemente de o provider estar configurado.
        if (string.Equals(policy, "Disabled", StringComparison.OrdinalIgnoreCase) || context == null)
        {
            return Rejected(policy, "Pesquisa externa não está habilitada para esta aplicação.", sanitizedQuery);
        }

        var provider = await _providerFactory.GetProviderAsync(ct);

        // §62: policy permite, mas não há provider configurado — warning não fatal,
        // nunca derruba a investigação inteira.
        if (!provider.IsConfigured)
        {
            return Rejected(policy, "Pesquisa externa está habilitada para esta aplicação, mas não há provedor configurado.", sanitizedQuery);
        }

        if (string.IsNullOrWhiteSpace(sanitizedQuery))
        {
            return Rejected(policy, "A consulta ficou vazia após a sanitização (dado sensível removido) — refine a busca.", sanitizedQuery);
        }

        IReadOnlyList<ExternalSearchResult> rawResults;
        try
        {
            rawResults = await provider.SearchAsync(sanitizedQuery, maxResults, ct);
        }
        catch (Exception ex)
        {
            // §63: timeout/erro do provider não derruba a investigação — vira rejeição
            // não fatal com mensagem sanitizada (nunca stack trace/headers/API key).
            return Rejected(policy, $"Falha ao consultar o provedor de pesquisa externa: {ex.Message}", sanitizedQuery);
        }

        var filtered = ApplyPolicyFilter(policy, context.TechnicalSources, context.AllowedDomains, rawResults);

        var resultDtos = filtered.Select(r => new ExternalResearchResultDto(
            Title: r.Title,
            Url: r.Url,
            Domain: r.Domain,
            Snippet: r.Snippet,
            TrustLevel: ClassifyTrustLevel(r.Domain, context.TechnicalSources),
            PublishedAt: r.PublishedAt,
            RetrievedAt: r.RetrievedAt
        )).ToList();

        sw.Stop();

        await _auditService.RecordAsync(
            action: "external_research.executed",
            entityType: "products",
            entityId: productId.ToString(),
            actorUserId: userId,
            after: new
            {
                ProductId = productId,
                Policy = policy,
                SanitizedQuery = sanitizedQuery,
                ResultCount = resultDtos.Count,
                DurationMs = sw.ElapsedMilliseconds
            },
            ct: ct);

        return new ExternalResearchOutcomeDto(
            Rejected: false,
            RejectionReason: null,
            PolicyApplied: policy,
            Results: resultDtos,
            SanitizedQuery: sanitizedQuery);
    }

    private static ExternalResearchOutcomeDto Rejected(string policy, string reason, string sanitizedQuery) =>
        new(Rejected: true, RejectionReason: reason, PolicyApplied: policy, Results: Array.Empty<ExternalResearchResultDto>(), SanitizedQuery: sanitizedQuery);

    private static IReadOnlyList<ExternalSearchResult> ApplyPolicyFilter(
        string policy,
        IReadOnlyList<ProductTechnicalSourceDto> technicalSources,
        IReadOnlyList<string> allowedDomains,
        IReadOnlyList<ExternalSearchResult> results)
    {
        if (string.Equals(policy, "OpenWeb", StringComparison.OrdinalIgnoreCase))
            return results;

        if (string.Equals(policy, "AllowListed", StringComparison.OrdinalIgnoreCase))
        {
            return results.Where(r => allowedDomains.Any(d => DomainMatches(r.Domain, d))).ToList();
        }

        // OfficialOnly (padrão mais restritivo além de Disabled)
        var officialDomains = technicalSources
            .Where(s => string.Equals(s.TrustLevel, "Official", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(s.Url))
            .Select(s => TryGetHost(s.Url!))
            .Where(h => h != null)
            .Cast<string>()
            .ToList();

        return results.Where(r =>
            officialDomains.Any(d => DomainMatches(r.Domain, d)) ||
            WellKnownOfficialDomains.Contains(r.Domain)
        ).ToList();
    }

    private static string ClassifyTrustLevel(string domain, IReadOnlyList<ProductTechnicalSourceDto> technicalSources)
    {
        var matchingSource = technicalSources.FirstOrDefault(s =>
            !string.IsNullOrWhiteSpace(s.Url) && DomainMatches(domain, TryGetHost(s.Url!) ?? string.Empty));

        if (matchingSource != null) return matchingSource.TrustLevel;
        if (WellKnownOfficialDomains.Contains(domain)) return "Official";
        return "Reference";
    }

    /// <summary>Domínio igual OU subdomínio do permitido — mesma regra usada na allowlist do Prompt 2.</summary>
    private static bool DomainMatches(string domain, string allowed)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(allowed)) return false;
        domain = domain.Trim().ToLowerInvariant();
        allowed = allowed.Trim().ToLowerInvariant();
        return domain == allowed || domain.EndsWith("." + allowed, StringComparison.Ordinal);
    }

    private static string? TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
}
