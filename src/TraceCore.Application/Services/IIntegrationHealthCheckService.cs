using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Application.Services;

/// <summary>
/// Fase 15 (M10): Serviço de verificação automática e determinística de saúde (health-check)
/// para integrações configuradas (HTTP / TCP).
/// Falha segura (§26): falha de rede, timeout ou status code inesperado NUNCA grava Success.
/// </summary>
public interface IIntegrationHealthCheckService
{
    /// <summary>
    /// Executa a verificação (HTTP/TCP) e grava a execução no mecanismo único de
    /// IntegrationRun. Quando <paramref name="runContext"/> é omisso, a execução é
    /// classificada como "AutomatedHealthCheck" (verificação automática).
    /// <paramref name="caseId"/> opcional identifica o caso que originou o teste.
    /// </summary>
    Task<IntegrationRun> ExecuteHealthCheckAsync(long integrationId, string? runContext = null, long? caseId = null, CancellationToken ct = default);
}
