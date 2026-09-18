using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
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

    public CreateModel(
        ICaseService caseService,
        IDepartmentService departmentService,
        IUserService userService)
    {
        _caseService = caseService;
        _departmentService = departmentService;
        _userService = userService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<ClientDto> Clients { get; set; } = new List<ClientDto>();
    public IReadOnlyList<ProductDto> Products { get; set; } = new List<ProductDto>();
    public IReadOnlyList<EnvironmentDto> Environments { get; set; } = new List<EnvironmentDto>();
    public IReadOnlyList<ComponentDto> AvailableComponents { get; set; } = new List<ComponentDto>();
    public IReadOnlyList<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
    public IReadOnlyList<UserDto> UsersList { get; set; } = new List<UserDto>();

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "O relato original da ocorrência é obrigatório.")]
        [Display(Name = "Relato Original / Descrição")]
        public string OriginalReport { get; set; } = string.Empty;

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

        [Display(Name = "Sintoma Observado")]
        public string? SymptomText { get; set; }

        [Display(Name = "Evidência / Observação Inicial")]
        public string? EvidenceDescription { get; set; }

        [Display(Name = "Anexo (Log, Screenshot ou Arquivo de Apoio)")]
        public IFormFile? AttachmentFile { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
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
            if (Input.AttachmentFile != null && Input.AttachmentFile.Length > 0)
            {
                var fileStream = Input.AttachmentFile.OpenReadStream();
                attachmentsList.Add(new AttachmentInputDto(
                    FileName: Input.AttachmentFile.FileName,
                    MimeType: Input.AttachmentFile.ContentType,
                    ContentStream: fileStream,
                    Confidentiality: "Internal"
                ));
            }

            var symptomsList = new List<string>();
            if (!string.IsNullOrWhiteSpace(Input.SymptomText))
            {
                symptomsList.Add(Input.SymptomText.Trim());
            }

            var evidencesList = new List<CaseEvidenceInputDto>();
            if (!string.IsNullOrWhiteSpace(Input.EvidenceDescription))
            {
                evidencesList.Add(new CaseEvidenceInputDto("Other", Input.EvidenceDescription.Trim()));
            }

            var command = new OpenCaseCommand(
                OriginalReport: Input.OriginalReport,
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
                Attachments: attachmentsList
            );

            var createdCase = await _caseService.OpenCaseAsync(command, currentUserId);

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
    }
}
