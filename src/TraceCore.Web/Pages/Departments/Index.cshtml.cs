using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Departments;

[Authorize(Policy = "usuario.gerenciar")]
public class IndexModel : PageModel
{
    private readonly IDepartmentService _departmentService;

    public IndexModel(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public IReadOnlyList<DepartmentDto> Departments { get; set; } = Array.Empty<DepartmentDto>();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Departments = await _departmentService.GetAllDepartmentsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string description)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _departmentService.CreateDepartmentAsync(name, description, currentUserId);
            SuccessMessage = $"Departamento '{name}' criado com sucesso.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Violação de regra ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar departamento: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(long id, string name, string description, string status)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _departmentService.UpdateDepartmentAsync(id, name, description, status, currentUserId);
            SuccessMessage = $"Departamento '{name}' atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar departamento: {ex.Message}";
        }

        return RedirectToPage();
    }

    private long? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : null;
    }
}
