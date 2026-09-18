using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Integrations;

[Authorize(Policy = "integracao.gerenciar")]
public class IndexModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly IDepartmentService _departmentService;

    public IndexModel(IIntegrationService integrationService, IDepartmentService departmentService)
    {
        _integrationService = integrationService;
        _departmentService = departmentService;
    }

    public IReadOnlyList<IntegrationDto> IntegrationsList { get; private set; } = [];
    public IReadOnlyList<DepartmentDto> DepartmentsList { get; private set; } = [];

    [BindProperty]
    public CreateIntegrationInput NewIntegration { get; set; } = new();

    [BindProperty]
    public RegisterRunInput RunInput { get; set; } = new();

    [BindProperty]
    public UpdateStatusInput StatusInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public record CreateIntegrationInput
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IntegrationType { get; set; } = "Other";
        public string? TargetSystemDescription { get; set; }
        public long? OwnerDepartmentId { get; set; }
        public string? ContractNotes { get; set; }
    }

    public record RegisterRunInput
    {
        public long IntegrationId { get; set; }
        public string Status { get; set; } = "Success";
        public DateTime? StartedAt { get; set; }
        public long? RecordsProcessed { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public record UpdateStatusInput
    {
        public long IntegrationId { get; set; }
        public string Status { get; set; } = "Configured";
    }

    public async Task OnGetAsync()
    {
        IntegrationsList = await _integrationService.GetIntegrationsAsync();
        DepartmentsList = await _departmentService.GetAllDepartmentsAsync();
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
                CreatedBy: GetCurrentUserId()));

            SuccessMessage = $"Integração '{NewIntegration.Name}' cadastrada no catálogo com status 'Configured' (nenhuma conexão ativa).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao cadastrar integração: {ex.Message}";
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
                RecordedBy: GetCurrentUserId()));

            SuccessMessage = "Execução manual registrada no histórico da integração (log, não conexão automática).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar execução: {ex.Message}";
        }

        return RedirectToPage();
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}