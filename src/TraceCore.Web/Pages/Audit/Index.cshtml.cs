using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Web.Pages.Audit;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAuditService _auditService;
    private readonly IUserRepository _userRepository;

    public IndexModel(IAuditService auditService, IUserRepository userRepository)
    {
        _auditService = auditService;
        _userRepository = userRepository;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? ActorUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityIdFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AuditSearchResultDto SearchResult { get; private set; } = null!;
    public IReadOnlyList<User> Users { get; private set; } = Array.Empty<User>();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!User.HasClaim("permission", "auditoria.visualizar"))
        {
            return Forbid();
        }

        Users = await _userRepository.GetAllAsync(ct);

        var filter = new AuditFilterDto(
            FromDate: FromDate,
            ToDate: ToDate,
            ActorUserId: ActorUserId,
            Action: ActionFilter,
            EntityType: EntityTypeFilter,
            EntityId: EntityIdFilter,
            SearchTerm: SearchTerm,
            Page: PageNumber < 1 ? 1 : PageNumber,
            PageSize: 50
        );

        SearchResult = await _auditService.SearchAsync(filter, ct);

        return Page();
    }
}
