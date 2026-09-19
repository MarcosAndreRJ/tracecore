using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Copilot;

[Authorize(Policy = "ia.usar")]
public class IndexModel : PageModel
{
    private readonly IRagService _ragService;

    public IndexModel(IRagService ragService)
    {
        _ragService = ragService;
    }

    [BindProperty(SupportsGet = true)]
    public string Question { get; set; } = string.Empty;

    [BindProperty]
    public long FeedbackInteractionId { get; set; }

    [BindProperty]
    public bool FeedbackUseful { get; set; }

    [BindProperty]
    public string? FeedbackComment { get; set; }

    [BindProperty]
    public long DraftInteractionId { get; set; }

    public RagAnswerDto? LastAnswer { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet(string? question = null)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            Question = question;
        }
    }

    public async Task<IActionResult> OnPostAskAsync()
    {
        if (string.IsNullOrWhiteSpace(Question))
        {
            ErrorMessage = "Digite uma pergunta para o copiloto.";
            return RedirectToPage();
        }

        try
        {
            LastAnswer = await _ragService.AskAsync(Question, GetCurrentUserId());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Não foi possível consultar o copiloto: {ex.Message}";
            return RedirectToPage();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostFeedbackAsync()
    {
        try
        {
            await _ragService.SubmitFeedbackAsync(
                new AiFeedbackCommand(FeedbackInteractionId, FeedbackUseful, FeedbackComment),
                GetCurrentUserId());
            SuccessMessage = "Feedback registrado. Ele é armazenado para melhoria futura e jamais altera conhecimento automaticamente (BR-086).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao registrar feedback: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateDraftAsync()
    {
        if (DraftInteractionId <= 0)
        {
            ErrorMessage = "Interação inválida para criação de rascunho.";
            return RedirectToPage();
        }

        if (!User.HasClaim("permission", "solucao.criar"))
        {
            ErrorMessage = "Você não tem permissão para criar rascunhos de conhecimento.";
            return RedirectToPage();
        }

        try
        {
            var knowledgeItemId = await _ragService.CreateDraftFromInteractionAsync(DraftInteractionId, GetCurrentUserId());
            return RedirectToPage("/Knowledge/Details", new { id = knowledgeItemId });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Não foi possível criar o rascunho: {ex.Message}";
            return RedirectToPage();
        }
    }

    private long? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : null;
    }
}