using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Services;

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

    public IReadOnlyList<LlmProviderDto> Providers { get; private set; } = [];
    public IReadOnlyList<LlmModelConfigDto> ModelConfigs { get; private set; } = [];
    public LlmModelConfigDto? GenerationActive { get; private set; }
    public LlmModelConfigDto? EmbeddingActive { get; private set; }
    public IReadOnlyList<string> SupportedProtocols { get; private set; } = [];
    public IReadOnlyList<string> SupportedAuthenticationTypes { get; private set; } = [];
    public IReadOnlyList<LlmModelEntry> GenerationSuggestions { get; private set; } = [];
    public IReadOnlyList<LlmModelEntry> EmbeddingSuggestions { get; private set; } = [];

    [BindProperty] public ProviderFormInput ProviderForm { get; set; } = new();
    [BindProperty] public ModelUsageInput GenUsageInput { get; set; } = new();
    [BindProperty] public ModelUsageInput EmbUsageInput { get; set; } = new();
    [BindProperty] public CredentialInput CredInput { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveProviderAsync()
    {
        if (string.IsNullOrWhiteSpace(ProviderForm.Name) ||
            string.IsNullOrWhiteSpace(ProviderForm.Code) ||
            string.IsNullOrWhiteSpace(ProviderForm.Protocol) ||
            string.IsNullOrWhiteSpace(ProviderForm.BaseUrl))
        {
            ErrorMessage = "Nome, Código, Protocolo e Base URL são obrigatórios.";
            await LoadDataAsync();
            return Page();
        }

        if (!ProviderForm.HasGenerationCapability && !ProviderForm.HasEmbeddingCapability)
        {
            ErrorMessage = "Selecione pelo menos uma capacidade (Geração ou Embedding).";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var cmd = new UpsertLlmProviderCommand(
                Id: ProviderForm.Id > 0 ? ProviderForm.Id : null,
                Name: ProviderForm.Name.Trim(),
                Code: ProviderForm.Code.Trim().ToLowerInvariant(),
                Protocol: ProviderForm.Protocol.Trim(),
                BaseUrl: ProviderForm.BaseUrl.Trim(),
                AuthenticationType: ProviderForm.AuthenticationType?.Trim() ?? "BearerApiKey",
                HasGenerationCapability: ProviderForm.HasGenerationCapability,
                HasEmbeddingCapability: ProviderForm.HasEmbeddingCapability,
                Status: ProviderForm.Status?.Trim() ?? "Active",
                NewApiKey: !string.IsNullOrWhiteSpace(ProviderForm.ApiKey) ? ProviderForm.ApiKey.Trim() : null,
                UpdatedBy: GetCurrentUserId());

            await _configurationService.UpsertProviderAsync(cmd);
            SuccessMessage = $"Provedor '{ProviderForm.Name}' salvo com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar provedor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveGenerationModelAsync()
    {
        if (GenUsageInput.ProviderId <= 0 || string.IsNullOrWhiteSpace(GenUsageInput.ModelName))
        {
            ErrorMessage = "Selecione o provedor e informe o modelo de geração.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
                Id: null,
                Purpose: "Generation",
                ProviderId: GenUsageInput.ProviderId,
                ModelName: GenUsageInput.ModelName.Trim(),
                IsActive: true,
                UpdatedBy: GetCurrentUserId()));

            SuccessMessage = "Modelo de geração atualizado e ativado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao configurar modelo de geração: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveEmbeddingModelAsync()
    {
        if (EmbUsageInput.ProviderId <= 0 || string.IsNullOrWhiteSpace(EmbUsageInput.ModelName))
        {
            ErrorMessage = "Selecione o provedor e informe o modelo de embedding.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
                Id: null,
                Purpose: "Embedding",
                ProviderId: EmbUsageInput.ProviderId,
                ModelName: EmbUsageInput.ModelName.Trim(),
                IsActive: true,
                UpdatedBy: GetCurrentUserId()));

            SuccessMessage = "Modelo de embedding atualizado e ativado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao configurar modelo de embedding: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveCredentialAsync()
    {
        if (string.IsNullOrWhiteSpace(CredInput.ProviderCode) || string.IsNullOrWhiteSpace(CredInput.ApiKey))
        {
            ErrorMessage = "Código do provedor e API Key são obrigatórios.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.SaveCredentialAsync(
                CredInput.ProviderCode.Trim().ToLowerInvariant(),
                CredInput.ApiKey.Trim(),
                GetCurrentUserId());

            SuccessMessage = $"Credencial do provedor '{CredInput.ProviderCode}' salva com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar credencial: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCredentialAsync()
    {
        if (string.IsNullOrWhiteSpace(CredInput.ProviderCode))
        {
            ErrorMessage = "Código do provedor é obrigatório.";
            return RedirectToPage();
        }

        try
        {
            await _configurationService.DeleteCredentialAsync(
                CredInput.ProviderCode.Trim().ToLowerInvariant(),
                GetCurrentUserId());

            SuccessMessage = $"Credencial do provedor '{CredInput.ProviderCode}' removida.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao remover credencial: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestConnectionAsync(long? modelConfigId, string? providerCode, string? purpose)
    {
        try
        {
            ConnectionTestResult result;
            if (modelConfigId.HasValue && modelConfigId.Value > 0)
            {
                result = await _configurationService.TestModelConfigConnectionAsync(modelConfigId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(providerCode) && !string.IsNullOrWhiteSpace(purpose))
            {
                result = await _configurationService.TestConnectionAsync(providerCode, purpose);
            }
            else
            {
                return new JsonResult(new { success = false, message = "Identificador de configuração não informado." });
            }

            return new JsonResult(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Erro ao testar conexão: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnGetFetchModelsAsync(long providerId, string purpose)
    {
        try
        {
            if (providerId <= 0 || string.IsNullOrWhiteSpace(purpose))
            {
                return new JsonResult(new { success = false, message = "ProviderId e Purpose são obrigatórios." });
            }

            var models = await _configurationService.FetchModelsFromProviderAsync(providerId, purpose);
            var result = models.Select(m => new { modelId = m.ModelId, displayName = m.DisplayName, isDefault = m.IsDefault }).ToList();
            return new JsonResult(new { success = true, models = result });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Erro ao buscar modelos: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnPostIndexEmbeddingsAsync()
    {
        try
        {
            var result = await _embeddingIndexingService.IndexReadyContentAsync(limit: 200);
            SuccessMessage = $"Indexação concluída: {result.ProcessedCount} itens vetorizados, {result.FailedCount} falhas. {result.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Falha ao executar indexação: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Providers = await _configurationService.GetProvidersAsync();
        ModelConfigs = await _configurationService.GetModelConfigsAsync();

        GenerationActive = ModelConfigs.FirstOrDefault(m => m.Purpose.Equals("Generation", StringComparison.OrdinalIgnoreCase) && m.IsActive);
        EmbeddingActive = ModelConfigs.FirstOrDefault(m => m.Purpose.Equals("Embedding", StringComparison.OrdinalIgnoreCase) && m.IsActive);

        if (GenerationActive != null)
        {
            GenUsageInput.ProviderId = GenerationActive.ProviderId;
            GenUsageInput.ModelName = GenerationActive.ModelName;
            GenerationSuggestions = _configurationService.GetSupportedModels(GenerationActive.ProviderCode, "Generation");
        }

        if (EmbeddingActive != null)
        {
            EmbUsageInput.ProviderId = EmbeddingActive.ProviderId;
            EmbUsageInput.ModelName = EmbeddingActive.ModelName;
            EmbeddingSuggestions = _configurationService.GetSupportedModels(EmbeddingActive.ProviderCode, "Embedding");
        }

        SupportedProtocols = _configurationService.GetSupportedProtocols();
        SupportedAuthenticationTypes = _configurationService.GetSupportedAuthenticationTypes();
    }

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}

public class ProviderFormInput
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Protocol { get; set; } = "OpenAICompatible";
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = "BearerApiKey";
    public bool HasGenerationCapability { get; set; } = true;
    public bool HasEmbeddingCapability { get; set; } = false;
    public string Status { get; set; } = "Active";
    public string? ApiKey { get; set; }
}

public class ModelUsageInput
{
    public long ProviderId { get; set; }
    public string ModelName { get; set; } = string.Empty;
}

public class CredentialInput
{
    public string ProviderCode { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
