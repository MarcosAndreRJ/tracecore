using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

/// <summary>
/// Repositório dedicado ao contexto técnico de investigação de um produto (Prompt 2).
/// Separado de ICatalogRepository para não sobrecarregar uma interface já com 16
/// métodos cobrindo Products/Versions/Environments/Components/Dependencies/Owners.
/// </summary>
public interface IProductTechnicalContextRepository
{
    Task<ProductTechnicalProfile?> GetProfileByProductIdAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductTechnicalProfile>> GetProfilesByProductIdsAsync(IReadOnlyList<long> productIds, CancellationToken ct = default);
    Task UpsertProfileAsync(ProductTechnicalProfile profile, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetTechnologyNamesByProductIdAsync(long productId, CancellationToken ct = default);
    Task SetProductTechnologiesAsync(long productId, IEnumerable<string> technologyNames, CancellationToken ct = default);

    Task<IReadOnlyList<ProductTechnicalSource>> GetSourcesByProductIdAsync(long productId, bool includeInactive = false, CancellationToken ct = default);
    Task<ProductTechnicalSource?> GetSourceByIdAsync(long sourceId, CancellationToken ct = default);
    Task<long> AddSourceAsync(ProductTechnicalSource source, CancellationToken ct = default);
    Task UpdateSourceAsync(ProductTechnicalSource source, CancellationToken ct = default);
    Task SetSourceActiveAsync(long sourceId, bool isActive, long? updatedBy, CancellationToken ct = default);

    Task<IReadOnlyList<ProductExternalResearchDomain>> GetAllowedDomainsByProductIdAsync(long productId, bool includeInactive = false, CancellationToken ct = default);
    Task<long> AddAllowedDomainAsync(ProductExternalResearchDomain domain, CancellationToken ct = default);
    Task<bool> RemoveAllowedDomainAsync(long domainId, CancellationToken ct = default);
}
