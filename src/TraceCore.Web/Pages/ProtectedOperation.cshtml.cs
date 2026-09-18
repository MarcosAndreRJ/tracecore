using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraceCore.Web.Pages;

[Authorize(Policy = "usuario.gerenciar")]
public class ProtectedOperationModel : PageModel
{
    public string StatusMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        StatusMessage = "Acesso autorizado com sucesso pelo servidor! Você possui a permissão 'usuario.gerenciar'.";
    }

    public IActionResult OnPostExecute()
    {
        return new JsonResult(new
        {
            success = true,
            message = "Operação administrativa protegida executada no servidor com sucesso!",
            executedBy = User.Identity?.Name,
            serverTimestampUtc = System.DateTime.UtcNow
        });
    }
}
