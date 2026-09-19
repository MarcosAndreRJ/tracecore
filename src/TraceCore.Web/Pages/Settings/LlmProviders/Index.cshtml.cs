using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Settings.LlmProviders;

[Authorize(Policy = "configuracao.gerenciar")]
public class IndexModel : PageModel
{
    private readonly ILlmConfigurationService _configurationService;
    private readonly IEmbeddingIndexingService _embeddingIndexingService;

    public IndexModel(
        ILlmConfigurationService configurationService,
        IEmbeddingIndexingService embeddingIndexingService)
    {
        _configurationService = configurationService;
        _embeddingIndexingService = embeddingIndexingService;
    }

    public IReadOnlyList<LlmProviderConfigDto> Configs { get; private set; } = [];

    [BindProperty]
    public long ActivateConfigId { get; set; }

    [BindProperty]
    public UpdateModelInput ModelInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Configs = await _configurationService.GetConfigsAsync();
    }

    public async Task<IActionResult> OnPostActivateAsync()
    {
        if (ActivateConfigId <= 0)
        {
            ErrorMessage = "Configuração inválida.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.ActivateAsync(ActivateConfigId, GetCurrentUserId());
            SuccessMessage = "Provedor ativado. A credencial (API Key) continua em configuração de ambiente — nunca no banco.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao ativar provedor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateModelAsync()
    {
        if (ModelInput.ConfigId <= 0 || string.IsNullOrWhiteSpace(ModelInput.ModelName))
        {
            ErrorMessage = "Configuração e nome do modelo devem ser informados.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.UpdateModelAsync(ModelInput.ConfigId, ModelInput.ModelName, GetCurrentUserId());
            SuccessMessage = "Modelo atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar modelo: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostIndexEmbeddingsAsync()
    {
        try
        {
            var result = await _embeddingIndexingService.IndexReadyContentAsync();
            SuccessMessage = result.ProviderConfigured
                ? $"Indexação de embeddings concluída — modelo '{result.ModelName}', {result.ProcessedCount} vetorizado(s), {result.FailedCount} falha(s). {(string.IsNullOrEmpty(result.Message) ? string.Empty : "<br/>" + result.Message)}"
                : "Nenhum provedor de embedding configurado.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao indexar embeddings: {ex.Message}";
        }

        return RedirectToPage();
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }

    public record UpdateModelInput
    {
        public long ConfigId { get; set; }
        public string ModelName { get; set; } = string.Empty;
    }
}