using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IDiagnosticRepository _diagnosticRepository;

    public IndexModel(
        ICaseRepository caseRepository,
        ICatalogRepository catalogRepository,
        IDiagnosticRepository diagnosticRepository)
    {
        _caseRepository = caseRepository;
        _catalogRepository = catalogRepository;
        _diagnosticRepository = diagnosticRepository;
    }

    public bool IsUserAuthenticated { get; private set; }

    // Métricas Reais do Cockpit Operacional
    public int OpenCasesCount { get; private set; }
    public int CriticalCasesCount { get; private set; }
    public int InInvestigationCount { get; private set; }
    public int AwaitingActionCount { get; private set; }

    // Casos Recentes em Aberto
    public List<RecentCaseItem> RecentCases { get; private set; } = new();

    // Sistemas com Maior Número de Ocorrências (dado real da Fase 2/3)
    public List<SystemOccurrenceItem> TopSystems { get; private set; } = new();

    // Dependências Técnicas Reais (Bloco 7.A.2)
    public IReadOnlyList<ComponentDependency> ActiveDependencies { get; private set; } = [];

    // Caso de referência para ação rápida de diagnóstico
    public long? NextDiagnosisCaseId { get; private set; }

    public record RecentCaseItem(
        long Id,
        ulong CaseNumber,
        string Title,
        string Severity,
        string Status,
        string SystemName,
        DateTime OpenedAt
    );

    public record SystemOccurrenceItem(
        string SystemName,
        int Count,
        string Status
    );

    public async Task<IActionResult> OnGetAsync()
    {
        IsUserAuthenticated = User.Identity?.IsAuthenticated == true;

        if (!IsUserAuthenticated)
        {
            return Page();
        }

        // Carregar casos (limite generoso para métricas do cockpit)
        var allCases = await _caseRepository.GetAllAsync(limit: 500);
        var products = await _catalogRepository.GetAllProductsAsync();
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);
        ActiveDependencies = await _catalogRepository.GetComponentDependenciesAsync();

        var openCases = allCases.Where(c => c.Status.Equals("Open", StringComparison.OrdinalIgnoreCase)).ToList();

        OpenCasesCount = openCases.Count;
        CriticalCasesCount = openCases.Count(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));

        // Calcular casos em investigação real (que possuem hipóteses ativas Proposed ou Supported)
        int inInvestigation = 0;
        int awaitingAction = 0;

        foreach (var c in openCases)
        {
            var hypotheses = await _diagnosticRepository.GetHypothesesByCaseIdAsync(c.Id);
            var hasActiveHypotheses = hypotheses.Any(h =>
                h.Status.Equals("Proposed", StringComparison.OrdinalIgnoreCase) ||
                h.Status.Equals("Supported", StringComparison.OrdinalIgnoreCase));

            if (hasActiveHypotheses)
            {
                inInvestigation++;
            }
            else
            {
                awaitingAction++;
            }
        }

        InInvestigationCount = inInvestigation;
        AwaitingActionCount = awaitingAction;

        // Caso mais recente com prioridade para diagnóstico rápido
        var targetCase = openCases
            .OrderByDescending(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(c => c.OpenedAt)
            .FirstOrDefault();

        NextDiagnosisCaseId = targetCase?.Id;

        // 5 Casos Mais Recentes
        RecentCases = openCases
            .OrderByDescending(c => c.OpenedAt)
            .Take(5)
            .Select(c => new RecentCaseItem(
                c.Id,
                c.CaseNumber,
                !string.IsNullOrWhiteSpace(c.NormalizedSummary) ? c.NormalizedSummary : (!string.IsNullOrWhiteSpace(c.ObservedBehavior) ? c.ObservedBehavior : "Caso sem descrição resumida"),
                c.Severity,
                c.Status,
                c.ProductId.HasValue && productDict.TryGetValue(c.ProductId.Value, out var pName) ? pName : "Não especificado",
                c.OpenedAt
            ))
            .ToList();

        // Sistemas com maior número de ocorrências (cases.product_id agrupado)
        TopSystems = allCases
            .Where(c => c.ProductId.HasValue && productDict.ContainsKey(c.ProductId.Value))
            .GroupBy(c => c.ProductId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(4)
            .Select(g => new SystemOccurrenceItem(
                productDict[g.Key],
                g.Count(),
                g.Any(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase) && c.Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
                    ? "Atenção Crítica"
                    : "Operacional"
            ))
            .ToList();

        return Page();
    }
}
