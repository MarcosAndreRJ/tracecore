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
    Task<IntegrationRun> ExecuteHealthCheckAsync(long integrationId, CancellationToken ct = default);
}
