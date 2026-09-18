using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraceCore.Web.Pages;

[Authorize]
public class PlaceholderModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Module { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Module))
        {
            Module = "Funcionalidade";
        }
    }
}
