using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;

namespace TraceCore.Web.Pages.Integrations;

[Authorize(Policy = "integracao.gerenciar")]
public class IndexModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly IDepartmentService _departmentService;
    private readonly ICatalogService _catalogService;
    private readonly IIntegrationHealthCheckService _healthCheckService;

    public IndexModel(
        IIntegrationService integrationService,
        IDepartmentService departmentService,
        ICatalogService catalogService,
        IIntegrationHealthCheckService healthCheckService)
    {
        _integrationService = integrationService;
        _departmentService = departmentService;
        _catalogService = catalogService;
        _healthCheckService = healthCheckService;
    }

    public IReadOnlyList<IntegrationDto> IntegrationsList { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> DepartmentsList { get; private set; } = [];
    public IReadOnlyList<Product> ProductsList { get; private set; } = [];
    public IReadOnlyList<IntegrationType> IntegrationTypesList { get; private set; } = [];

    [BindProperty]
    public CreateIntegrationInput NewIntegration { get; set; } = new();

    [BindProperty]
    public EditIntegrationInput EditIntegration { get; set; } = new();

    [BindProperty]
    public RegisterRunInput RunInput { get; set; } = new();

    [BindProperty]
    public UpdateStatusInput StatusInput { get; set; } = new();

    [BindProperty]
    public ConfigureHealthCheckInput HealthCheckInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record CreateIntegrationInput
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IntegrationType { get; set; } = "Other";
        public long? ProductId { get; set; }
        public string? TargetSystemDescription { get; set; }
        public long? OwnerDepartmentId { get; set; }
        public string? ContractNotes { get; set; }
        public string? Responsibility { get; set; }
        public string? HostingLocation { get; set; }
        public string? Direction { get; set; }
    }

    public record EditIntegrationInput
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IntegrationType { get; set; } = string.Empty;
        public long? ProductId { get; set; }
        public string? TargetSystemDescription { get; set; }
        public long? OwnerDepartmentId { get; set; }
        public string? ContractNotes { get; set; }
        public string? Responsibility { get; set; }
        public string? HostingLocation { get; set; }
        public string? Direction { get; set; }
    }

    public record RegisterRunInput
    {
        public long IntegrationId { get; set; }
        public string Status { get; set; } = "Success";
        public DateTime? StartedAt { get; set; }
        public long? RecordsProcessed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TriggeredBy { get; set; } = "Manual";
    }

    public record UpdateStatusInput
    {
        public long IntegrationId { get; set; }
        public string Status { get; set; } = "Configured";
    }

    public record ConfigureHealthCheckInput
    {
        public long IntegrationId { get; set; }
        public string? HealthCheckUrl { get; set; }
        public string HealthCheckMethod { get; set; } = "GET";
        public int HealthCheckTimeoutSeconds { get; set; } = 10;
        public int HealthCheckExpectedStatusCode { get; set; } = 200;
    }

    public async Task OnGetAsync()
    {
        IntegrationsList = await _integrationService.GetIntegrationsAsync();
        DepartmentsList = await _departmentService.GetAllDepartmentsAsync();
        ProductsList = await _catalogService.GetAllProductsAsync();
        IntegrationTypesList = await _integrationService.GetIntegrationTypesAsync();
    }

    public async Task<IActionResult> OnPostCreateIntegrationAsync()
    {
        if (string.IsNullOrWhiteSpace(NewIntegration.Code) || string.IsNullOrWhiteSpace(NewIntegration.Name) || string.IsNullOrWhiteSpace(NewIntegration.IntegrationType))
        {
            ErrorMessage = "Código, Nome e Tipo da integração são obrigatórios.";
            return RedirectToPage();
        }

        try
        {
            var id = await _integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
                Code: NewIntegration.Code,
                Name: NewIntegration.Name,
                IntegrationType: NewIntegration.IntegrationType,
                TargetSystemDescription: NewIntegration.TargetSystemDescription,
                OwnerDepartmentId: NewIntegration.OwnerDepartmentId,
                ContractNotes: NewIntegration.ContractNotes,
                CreatedBy: GetCurrentUserId(),
                ProductId: NewIntegration.ProductId,
                Responsibility: NewIntegration.Responsibility,
                HostingLocation: NewIntegration.HostingLocation,
                Direction: NewIntegration.Direction));

            SuccessMessage = $"Integração '{NewIntegration.Name}' cadastrada no catálogo com status 'Configured' (nenhuma conexão ativa).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar integração: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateIntegrationAsync()
    {
        if (EditIntegration.Id <= 0 || string.IsNullOrWhiteSpace(EditIntegration.Code) || string.IsNullOrWhiteSpace(EditIntegration.Name) || string.IsNullOrWhiteSpace(EditIntegration.IntegrationType))
        {
            ErrorMessage = "Código, Nome e Tipo da integração são obrigatórios para edição.";
            return RedirectToPage();
        }

        try
        {
            await _integrationService.UpdateIntegrationAsync(new UpdateIntegrationCommand(
                Id: EditIntegration.Id,
                Code: EditIntegration.Code,
                Name: EditIntegration.Name,
                IntegrationType: EditIntegration.IntegrationType,
                ProductId: EditIntegration.ProductId,
                TargetSystemDescription: EditIntegration.TargetSystemDescription,
                OwnerDepartmentId: EditIntegration.OwnerDepartmentId,
                ContractNotes: EditIntegration.ContractNotes,
                Responsibility: EditIntegration.Responsibility,
                HostingLocation: EditIntegration.HostingLocation,
                Direction: EditIntegration.Direction), GetCurrentUserId());

            SuccessMessage = $"Integração '{EditIntegration.Name}' atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar integração: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnlinkIntegrationAsync(long integrationId)
    {
        if (integrationId <= 0)
        {
            ErrorMessage = "Integração inválida para desvincular.";
            return RedirectToPage();
        }

        try
        {
            await _integrationService.UnlinkIntegrationFromProductAsync(integrationId, GetCurrentUserId());
            SuccessMessage = "Integração desvinculada do sistema. Histórico de execuções mantido.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao desvincular integração: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        if (StatusInput.IntegrationId <= 0 || string.IsNullOrWhiteSpace(StatusInput.Status))
        {
            ErrorMessage = "Integração e novo status devem ser informados.";
            return RedirectToPage();
        }

        try
        {
            await _integrationService.UpdateIntegrationStatusAsync(StatusInput.IntegrationId, StatusInput.Status, GetCurrentUserId());
            SuccessMessage = "Status da integração atualizado manualmente com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar status: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegisterRunAsync()
    {
        if (RunInput.IntegrationId <= 0 || string.IsNullOrWhiteSpace(RunInput.Status))
        {
            ErrorMessage = "Integração e status da execução devem ser informados.";
            return RedirectToPage();
        }

        try
        {
            var runId = await _integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
                IntegrationId: RunInput.IntegrationId,
                Status: RunInput.Status,
                StartedAt: RunInput.StartedAt,
                RecordsProcessed: RunInput.RecordsProcessed,
                ErrorMessage: RunInput.ErrorMessage,
                RecordedBy: GetCurrentUserId(),
                TriggeredBy: RunInput.TriggeredBy ?? "Manual"));

            SuccessMessage = "Execução registrada no histórico da integração com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar execução: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConfigureHealthCheckAsync()
    {
        if (HealthCheckInput.IntegrationId <= 0)
        {
            ErrorMessage = "Integração inválida.";
            return RedirectToPage();
        }

        try
        {
            await _integrationService.ConfigureHealthCheckAsync(new ConfigureIntegrationHealthCheckCommand(
                IntegrationId: HealthCheckInput.IntegrationId,
                HealthCheckUrl: string.IsNullOrWhiteSpace(HealthCheckInput.HealthCheckUrl) ? null : HealthCheckInput.HealthCheckUrl.Trim(),
                HealthCheckMethod: HealthCheckInput.HealthCheckMethod,
                HealthCheckTimeoutSeconds: HealthCheckInput.HealthCheckTimeoutSeconds > 0 ? HealthCheckInput.HealthCheckTimeoutSeconds : 10,
                HealthCheckExpectedStatusCode: HealthCheckInput.HealthCheckExpectedStatusCode > 0 ? HealthCheckInput.HealthCheckExpectedStatusCode : 200
            ));

            SuccessMessage = "Configuração de Health-Check atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao configurar health-check: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestHealthCheckAsync(long integrationId)
    {
        if (integrationId <= 0)
        {
            ErrorMessage = "Integração inválida para teste de health-check.";
            return RedirectToPage();
        }

        try
        {
            var run = await _healthCheckService.ExecuteHealthCheckAsync(integrationId);
            if (run.Status == "Success")
            {
                SuccessMessage = $"Health-check bem-sucedido! Execução gravada no histórico como 'Automated' (Run #{run.Id}).";
            }
            else
            {
                ErrorMessage = $"Health-check falhou (princípio §26 - falha segura): {run.ErrorMessage}. Execução registrada como 'Failed'.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Exceção ao testar health-check: {ex.Message}";
        }

        return RedirectToPage();
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}