using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAppAuthService = TraceCore.Application.Services.IAuthenticationService;

namespace TraceCore.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IAppAuthService _authService;

    public LoginModel(IAppAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/usuarios");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(Email, Password, ip, userAgent);
        if (!result.IsSuccess || result.User == null)
        {
            ErrorMessage = result.ErrorMessage ?? "Credenciais inválidas.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.User.Name),
            new Claim(ClaimTypes.Email, result.User.Email),
            new Claim("status", result.User.Status.ToString())
        };

        if (result.SessionId.HasValue)
        {
            claims.Add(new Claim("session_id", result.SessionId.Value.ToString()));
        }

        // Adiciona cada capacidade/permissão granular como claim 'permission'
        foreach (var perm in result.Permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        return LocalRedirect(returnUrl ?? "/usuarios");
    }
}
