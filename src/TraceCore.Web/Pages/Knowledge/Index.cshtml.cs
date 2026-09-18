using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Knowledge;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(
        IKnowledgeService knowledgeService,
        ICatalogRepository catalogRepository,
        IAuthorizationService authorizationService)
    {
        _knowledgeService = knowledgeService;
        _catalogRepository = catalogRepository;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Provenance { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    // Lookups
    public IReadOnlyList<Product> Products { get; private set; } = [];

    // Permissões
    public bool CanCreate { get; private set; }

    // Métricas
    public int TotalArticles { get; private set; }
    public int PublishedCount { get; private set; }
    public int InReviewCount { get; private set; }
    public int StaleCount { get; private set; }
    public string OverallSuccessRate { get; private set; } = "Sem utilizações registradas";

    // Itens
    public IReadOnlyList<KnowledgeItemViewModel> Items { get; private set; } = [];

    // Inputs para novo rascunho de conhecimento
    [BindProperty]
    public string NewTitle { get; set; } = string.Empty;
    [BindProperty]
    public string NewSummary { get; set; } = string.Empty;
    [BindProperty]
    public string NewKnowledgeType { get; set; } = "Solution";
    [BindProperty]
    public long? NewProductId { get; set; }
    [BindProperty]
    public string? NewApplicableVersions { get; set; }
    [BindProperty]
    public string? NewValidationMethod { get; set; }
    [BindProperty]
    public string? NewContentMarkdown { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Products = await _catalogRepository.GetAllProductsAsync();

        var authCreate = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        CanCreate = authCreate.Succeeded;

        // Métricas globais reais da base de conhecimento
        var metrics = await _knowledgeService.GetKnowledgeDashboardMetricsAsync();
        TotalArticles = metrics.TotalCount;
        PublishedCount = metrics.PublishedCount;
        InReviewCount = metrics.InReviewCount;
        StaleCount = metrics.StaleCount;
        OverallSuccessRate = metrics.OverallSuccessRate;

        // Consulta filtrada no serviço de aplicação
        var searchDtos = await _knowledgeService.SearchKnowledgeAsync(
            search: Search,
            productId: ProductId,
            status: Status,
            provenance: Provenance,
            category: Category
        );

        Items = searchDtos.Select(dto => new KnowledgeItemViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Title = dto.Title,
            Summary = dto.Summary,
            ProductCode = dto.ProductCode,
            ProductName = dto.ProductName,
            Component = dto.Component,
            ApplicableVersions = dto.ApplicableVersions,
            Status = dto.Status,
            ProvenanceType = dto.ProvenanceType,
            ProvenanceRef = dto.ProvenanceRef,
            Author = dto.Author,
            Reviewer = dto.Reviewer,
            SuccessRate = dto.SuccessRate,
            SuccessSample = dto.SuccessSample,
            RiskLevel = dto.RiskLevel,
            HasRollbackPlan = dto.HasRollbackPlan,
            UpdatedAt = dto.UpdatedAt,
            Category = dto.Category,
            Tags = dto.Tags
        }).ToList();
    }

    public async Task<IActionResult> OnPostCreateDraftAsync()
    {
        var authCreate = await _authorizationService.AuthorizeAsync(User, "solucao.criar");
        if (!authCreate.Succeeded) return Forbid();

        long? userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        try
        {
            var applicabilities = new List<CreateKnowledgeApplicabilityInput>();
            if (NewProductId.HasValue)
            {
                applicabilities.Add(new CreateKnowledgeApplicabilityInput(
                    ProductId: NewProductId.Value,
                    Notes: NewApplicableVersions
                ));
            }

            var command = new CreateKnowledgeDraftCommand(
                Title: NewTitle,
                Summary: NewSummary,
                KnowledgeType: NewKnowledgeType,
                ProvenanceType: "Documentation",
                ContentMarkdown: NewContentMarkdown ?? NewSummary,
                ValidationMethod: NewValidationMethod,
                Applicabilities: applicabilities
            );

            var newId = await _knowledgeService.CreateKnowledgeDraftAsync(command, userId.Value);
            StatusMessage = "Rascunho de solução criado com sucesso (BR-040). Prossiga com o detalhamento técnico e submissão para revisão.";
            return RedirectToPage("/Knowledge/Details", new { id = newId });
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Regra de Negócio ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar rascunho: {ex.Message}";
        }

        return RedirectToPage();
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(idClaim, out var id)) return id;
        return null;
    }
}

public class KnowledgeItemViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string ApplicableVersions { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProvenanceType { get; set; } = string.Empty;
    public string? ProvenanceRef { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string SuccessRate { get; set; } = string.Empty;
    public string SuccessSample { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public bool HasRollbackPlan { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Category { get; set; } = "solutions";
    public List<string> Tags { get; set; } = [];
}
