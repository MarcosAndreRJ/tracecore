using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface ISearchService
{
    Task<SearchResultsResponseDto> SearchAsync(ExecuteSearchCommand command, long? currentUserId = null, CancellationToken ct = default);
    Task<long> RecordInteractionAsync(RecordResultInteractionCommand command, CancellationToken ct = default);
    Task RecordFeedbackAsync(RecordFeedbackCommand command, CancellationToken ct = default);
}
