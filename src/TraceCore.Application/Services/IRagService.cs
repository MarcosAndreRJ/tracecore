using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IRagService
{
    Task<RagAnswerDto> AskAsync(string question, long? userId, CancellationToken ct = default);
    Task SubmitFeedbackAsync(AiFeedbackCommand command, long? userId, CancellationToken ct = default);
    Task<long> CreateDraftFromInteractionAsync(long interactionId, long? userId, CancellationToken ct = default);
}