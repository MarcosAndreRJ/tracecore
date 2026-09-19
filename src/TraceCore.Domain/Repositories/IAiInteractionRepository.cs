using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IAiInteractionRepository
{
    Task<long> AddInteractionAsync(AiInteraction interaction, CancellationToken ct = default);
    Task AddSourcesAsync(IEnumerable<AiSource> sources, CancellationToken ct = default);
    Task<long> AddFeedbackAsync(AiInteractionFeedback feedback, CancellationToken ct = default);
    Task<AiInteraction?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<AiSource>> GetSourcesByInteractionIdAsync(long interactionId, CancellationToken ct = default);
    Task<IReadOnlyList<AiInteraction>> GetInteractionsByUserAsync(long userId, int take = 20, CancellationToken ct = default);
}