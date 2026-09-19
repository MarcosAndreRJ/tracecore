using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Settings;

[Authorize(Policy = "configuracao.gerenciar")]
public class IndexModel : PageModel
{
    private readonly ILlmConfigurationService _configurationService;

    public IndexModel(ILlmConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public LlmModelConfigDto? GenerationActive { get; private set; }
    public LlmModelConfigDto? EmbeddingActive { get; private set; }
    public int TotalProviders { get; private set; }

    public async Task OnGetAsync()
    {
        GenerationActive = await _configurationService.GetActiveModelConfigAsync("Generation");
        EmbeddingActive = await _configurationService.GetActiveModelConfigAsync("Embedding");
        var providers = await _configurationService.GetProvidersAsync();
        TotalProviders = providers.Count;
    }
}
