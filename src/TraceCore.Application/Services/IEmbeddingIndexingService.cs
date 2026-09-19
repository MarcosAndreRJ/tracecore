using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IEmbeddingIndexingService
{
    Task<EmbeddingIndexingResultDto> IndexReadyContentAsync(int limit = 200, CancellationToken ct = default);
}