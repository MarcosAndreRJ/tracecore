using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

/// <summary>
/// Copiloto de investigação baseado no histórico corporativo (Prompt 3). Diferente de
/// <see cref="IRagService"/> (RAG semântico, que continua existindo e é usado aqui só
/// como ferramenta opcional de enriquecimento), este serviço NUNCA exige um provedor
/// de embedding configurado — a recuperação primária é busca estruturada (cliente,
/// produto, componente, versão, erro), similaridade determinística de casos
/// (<see cref="ICaseRelationService"/>) e conhecimento validado.
/// </summary>
public interface IInvestigationCopilotService
{
    Task<InvestigationCopilotAnswerDto> AskAsync(string question, long? userId, CancellationToken ct = default);
}
