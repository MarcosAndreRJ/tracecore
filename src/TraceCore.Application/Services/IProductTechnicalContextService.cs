using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IProductTechnicalContextService
{
    Task<ProductTechnicalProfileDto?> GetTechnicalProfileAsync(long productId, CancellationToken ct = default);
    Task UpsertTechnicalProfileAsync(UpsertProductTechnicalProfileCommand command, long? currentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetProductTechnologiesAsync(long productId, CancellationToken ct = default);
    Task SetProductTechnologiesAsync(long productId, IReadOnlyList<string> technologyNames, long? currentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductTechnicalSourceDto>> GetTechnicalSourcesAsync(long productId, CancellationToken ct = default);
    Task<long> AddTechnicalSourceAsync(CreateProductTechnicalSourceCommand command, long? currentUserId, CancellationToken ct = default);
    Task UpdateTechnicalSourceAsync(UpdateProductTechnicalSourceCommand command, long? currentUserId, CancellationToken ct = default);
    Task DisableTechnicalSourceAsync(long sourceId, long? currentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductExternalResearchDomainDto>> GetAllowedDomainsAsync(long productId, CancellationToken ct = default);
    Task<long> AddAllowedDomainAsync(long productId, string domain, string? description, long? currentUserId, CancellationToken ct = default);
    Task RemoveAllowedDomainAsync(long domainId, long? currentUserId, CancellationToken ct = default);

    /// <summary>
    /// Monta o contexto consolidado de investigação de um produto (Prompt 2, §24/§25).
    /// Poucas queries no backend — quem consome (futuro Copiloto) não precisa conhecer
    /// as tabelas internas nem fazer chamadas separadas por sub-recurso.
    /// </summary>
    Task<ProductInvestigationContextDto?> GetInvestigationContextAsync(long productId, long? productVersionId, CancellationToken ct = default);
}
