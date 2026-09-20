using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

/// <summary>
/// Prompt 4 — orquestra política + sanitização + provedor de pesquisa externa.
/// É a ÚNICA porta de entrada para pesquisa externa no TraceCore: o Copiloto
/// investigativo (Prompt 3) nunca chama IExternalResearchProvider diretamente, e o
/// LLM nunca decide a política — só passa productId/query/maxResults (§19/§20).
/// </summary>
public interface IExternalResearchService
{
    Task<ExternalResearchOutcomeDto> SearchAsync(long productId, string rawQuery, int maxResults, long? userId, CancellationToken ct = default);
}
