using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;

namespace TraceCore.Web.Pages.Cases;

[Authorize(Policy = "caso.criar")]
public class CreateModel : PageModel
{
    private readonly ICaseService _caseService;
    private readonly IDepartmentService _departmentService;
    private readonly IUserService _userService;
    private readonly ICatalogService _catalogService;
    private readonly ICaseRelationService _caseRelationService;
    private readonly IVersionManagementService _versionManagementService;

    public CreateModel(
        ICaseService caseService,
        IDepartmentService departmentService,
        IUserService userService,
        ICatalogService catalogService,
        ICaseRelationService caseRelationService,
        IVersionManagementService versionManagementService)
    {
        _caseService = caseService;
        _departmentService = departmentService;
        _userService = userService;
        _catalogService = catalogService;
        _caseRelationService = caseRelationService;
        _versionManagementService = versionManagementService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<ClientDto> Clients { get; set; } = new List<ClientDto>();
    public IReadOnlyList<ProductDto> Products { get; set; } = new List<ProductDto>();
    public IReadOnlyList<EnvironmentDto> Environments { get; set; } = new List<EnvironmentDto>();
    public IReadOnlyList<ComponentDto> AvailableComponents { get; set; } = new List<ComponentDto>();
    public IReadOnlyList<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
    public IReadOnlyList<UserDto> UsersList { get; set; } = new List<UserDto>();

    // Mapa Sistema -> Versões (id/rótulo), embutido como JSON para alimentar o select
    // dependente de Versão via JS, sem round-trip ao servidor a cada troca de Sistema.
    public string ProductVersionsJson { get; private set; } = "{}";

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "O relato original da ocorrência é obrigatório.")]
        [Display(Name = "Relato Original / Descrição")]
        public string OriginalReport { get; set; } = string.Empty;

        // BR-021: título/resumo opcional, editável depois nos Detalhes do caso.
        // Quando vazio, a UI usa o relato original truncado como título (comportamento já existente).
        [Display(Name = "Título Resumido")]
        public string? TitleSummary { get; set; }

        [Display(Name = "Prioridade / Severidade")]
        public string Severity { get; set; } = "Medium";

        [Display(Name = "Nível de Impacto")]
        public string? ImpactLevel { get; set; }

        [Display(Name = "Cliente")]
        public long? ClientId { get; set; }

        [Display(Name = "Sistema / Produto")]
        public long? ProductId { get; set; }

        [Display(Name = "Versão")]
        public long? ProductVersionId { get; set; }

        [Display(Name = "Ambiente")]
        public long? EnvironmentId { get; set; }

        // BR-023: Suporte a múltiplos componentes/módulos afetados
        [Display(Name = "Módulos / Componentes Afetados")]
        public List<long> SelectedComponentIds { get; set; } = new();

        [Display(Name = "Código do Erro")]
        public string? ErrorCode { get; set; }

        [Display(Name = "Mensagem de Erro Técnica")]
        public string? ErrorErrorMessage { get; set; }

        // 1→N: cada sintoma observado é uma linha própria (o backend já suportava lista;
        // só a UI achatava para um único campo).
        [Display(Name = "Sintomas Observados")]
        public List<string> Symptoms { get; set; } = new();

        // 1→N: idem para evidências textuais iniciais.
        [Display(Name = "Evidências / Observações Iniciais")]
        public List<string> Evidences { get; set; } = new();

        // Tags manuais opcionais, já na abertura — peso menor no motor de casos
        // semelhantes (ver CaseRelationService), mas ajudam a "amarrar" candidatos.
        [Display(Name = "Tags")]
        public List<string> Tags { get; set; } = new();

        [Display(Name = "Anexos (Log, Screenshot ou Arquivo de Apoio)")]
        public List<IFormFile> AttachmentFiles { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    // Pré-visualização de casos semelhantes durante o preenchimento (BR-048 / motor
    // determinístico já usado em Detalhes do Caso — ver ICaseRelationService). Chamado via
    // fetch() com debounce conforme o formulário é preenchido; nada aqui é persistido.
    public async Task<PartialViewResult> OnGetPreviewSimilarCasesAsync(
        string? reportText,
        long? clientId,
        long? productId,
        long? productVersionId,
        string? errorCode,
        List<long>? componentIds,
        List<string>? symptoms)
    {
        var input = new CaseSimilarityDraftInput(
            ReportText: reportText,
            ClientId: clientId,
            ProductId: productId,
            ProductVersionId: productVersionId,
            ErrorCode: errorCode,
            ComponentIds: componentIds,
            Symptoms: symptoms
        );

        var results = await _caseRelationService.PreviewSimilarCasesAsync(input);
        return Partial("~/Pages/Shared/Partials/_SimilarCasePreviewList.cshtml", results);
    }

    // Auto-preenchimento da versão ativa do cliente com base no contexto técnico ativo (Fase 4)
    public async Task<JsonResult> OnGetClientCurrentVersionAsync(long clientId, long productId)
    {
        var dto = await _versionManagementService.GetActiveVersionForClientAsync(clientId, productId, null);
        return new JsonResult(dto);
    }

    // Pré-visualização de possíveis correções lançadas em versões posteriores à versão selecionada (Fase 4)
    public async Task<PartialViewResult> OnGetPreviewPossibleFixesAsync(
        long? productId,
        long? productVersionId,
        string? title,
        string? description,
        long? componentId,
        string? errorCode)
    {
        if (!productId.HasValue || !productVersionId.HasValue)
        {
            return Partial("~/Pages/Shared/Partials/_PossibleFixSuggestionList.cshtml", Array.Empty<PossibleFixSuggestionDto>());
        }

        var results = await _versionManagementService.SuggestPossibleFixesAsync(
            productId: productId.Value,
            currentProductVersionId: productVersionId.Value,
            title: title,
            description: description,
            componentId: componentId,
            errorCode: errorCode
        );

        return Partial("~/Pages/Shared/Partials/_PossibleFixSuggestionList.cshtml", results);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            long? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var attachmentsList = new List<AttachmentInputDto>();
            foreach (var file in Input.AttachmentFiles)
            {
                if (file == null || file.Length == 0) continue;
                attachmentsList.Add(new AttachmentInputDto(
                    FileName: file.FileName,
                    MimeType: file.ContentType,
                    ContentStream: file.OpenReadStream(),
                    Confidentiality: "Internal"
                ));
            }

            var symptomsList = Input.Symptoms
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            var evidencesList = Input.Evidences
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => new CaseEvidenceInputDto("Other", e.Trim()))
                .ToList();

            var command = new OpenCaseCommand(
                OriginalReport: Input.OriginalReport,
                NormalizedSummary: Input.TitleSummary,
                Severity: Input.Severity,
                ImpactLevel: Input.ImpactLevel,
                ClientId: Input.ClientId,
                ProductId: Input.ProductId,
                ProductVersionId: Input.ProductVersionId,
                EnvironmentId: Input.EnvironmentId,
                ComponentIds: Input.SelectedComponentIds,
                ErrorCode: Input.ErrorCode,
                ErrorMessage: Input.ErrorErrorMessage,
                Symptoms: symptomsList,
                Evidences: evidencesList,
                Attachments: attachmentsList,
                Tags: Input.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList()
            );

            var createdCase = await _caseService.OpenCaseAsync(command, currentUserId);

            // A similaridade "oficial" (persistida) é computada automaticamente na
            // primeira visita aos Detalhes do caso recém-criado (GetCaseRelationsOverviewAsync
            // já revalida casos Open/Reopened/Investigating) — não precisa ser repetida aqui.
            return RedirectToPage("/Cases/Details", new { id = createdCase.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        Clients = await _caseService.GetClientsAsync();
        Products = await _caseService.GetProductsAsync();
        Environments = await _caseService.GetEnvironmentsAsync();
        AvailableComponents = await _caseService.GetComponentsAsync();
        Departments = await _departmentService.GetAllDepartmentsAsync();
        UsersList = await _userService.GetAllUsersAsync();

        var versionsByProduct = new Dictionary<string, List<VersionOption>>();
        foreach (var p in Products)
        {
            var versions = await _catalogService.GetVersionsByProductIdAsync(p.Id);
            versionsByProduct[p.Id.ToString()] = versions
                .OrderByDescending(v => v.ReleasedAt ?? DateTime.MinValue)
                .Select(v => new VersionOption(v.Id, v.VersionLabel))
                .ToList();
        }

        ProductVersionsJson = JsonSerializer.Serialize(versionsByProduct);
    }

    private record VersionOption(long Id, string Label);
}
