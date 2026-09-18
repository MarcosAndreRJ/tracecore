using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IAppAuthService = TraceCore.Application.Services.IAuthenticationService;

namespace TraceCore.Web.Pages;

public class LogoutModel : PageModel
{
    private readonly IAppAuthService _authService;

    public LogoutModel(IAppAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await PerformLogoutAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await PerformLogoutAsync();
    }

    private async Task<IActionResult> PerformLogoutAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdStr = User.FindFirstValue("session_id");
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (long.TryParse(userIdStr, out var userId))
        {
            long? sessionId = long.TryParse(sessionIdStr, out var sId) ? sId : null;
            await _authService.LogoutAsync(userId, sessionId, ip);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}
