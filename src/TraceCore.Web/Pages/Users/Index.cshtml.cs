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
using TraceCore.Domain.Enums;

namespace TraceCore.Web.Pages.Users;

[Authorize(Policy = "usuario.gerenciar")]
public class IndexModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IDepartmentService _departmentService;
    private readonly IRoleService _roleService;

    public IndexModel(
        IUserService userService,
        IDepartmentService departmentService,
        IRoleService roleService)
    {
        _userService = userService;
        _departmentService = departmentService;
        _roleService = roleService;
    }

    public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
    public IReadOnlyList<DepartmentDto> AvailableDepartments { get; set; } = Array.Empty<DepartmentDto>();
    public IReadOnlyList<RoleDto> AvailableRoles { get; set; } = Array.Empty<RoleDto>();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateUserAsync(
        string name,
        string email,
        string password,
        List<long>? departmentIds,
        List<long>? roleIds)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var req = new CreateUserRequest(
                name,
                email,
                password,
                departmentIds ?? new List<long>(),
                roleIds ?? new List<long>()
            );

            await _userService.CreateUserAsync(req, currentUserId);
            SuccessMessage = $"Usuário '{name}' criado com sucesso e vinculado às estruturas organizacionais.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Violação de regra ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar usuário: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(long userId, string newStatus)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (Enum.TryParse<UserStatus>(newStatus, true, out var status))
            {
                await _userService.UpdateUserStatusAsync(userId, status, currentUserId);
                SuccessMessage = $"Status do usuário alterado para {status} com sucesso (BR-005: histórico preservado).";
            }
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Violação de regra ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao alterar status: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateDepartmentsAsync(long userId, List<long>? departmentIds)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _userService.AssignDepartmentsAsync(userId, departmentIds ?? new List<long>(), currentUserId);
            SuccessMessage = "Departamentos do usuário atualizados com sucesso (BR-002: N:N).";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Violação de regra ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar departamentos: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateRolesAsync(long userId, List<long>? roleIds)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _userService.AssignRolesAsync(userId, roleIds ?? new List<long>(), currentUserId);
            SuccessMessage = "Papéis e permissões do usuário atualizados com sucesso.";
        }
        catch (BusinessRuleValidationException ex)
        {
            ErrorMessage = $"Violação de regra ({ex.RuleId}): {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar papéis: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Users = await _userService.GetAllUsersAsync();
        AvailableDepartments = await _departmentService.GetAllDepartmentsAsync();
        AvailableRoles = await _roleService.GetAllRolesAsync();
    }

    private long? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : null;
    }
}
