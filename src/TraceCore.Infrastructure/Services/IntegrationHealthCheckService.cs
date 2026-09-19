using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Services;

/// <summary>
/// Fase 15 (M10): Implementação determinística de verificação real de saúde para integrações.
/// Princípio de Falha Segura (§26):
/// NUNCA mascara falha de rede, timeout ou código inesperado como sucesso.
/// Sucesso é gravado APENAS com resposta real do endpoint no status esperado.
/// </summary>
public class IntegrationHealthCheckService : IIntegrationHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationRepository _integrationRepository;

    public IntegrationHealthCheckService(
        IHttpClientFactory httpClientFactory,
        IIntegrationRepository integrationRepository)
    {
        _httpClientFactory = httpClientFactory;
        _integrationRepository = integrationRepository;
    }

    public async Task<IntegrationRun> ExecuteHealthCheckAsync(long integrationId, CancellationToken ct = default)
    {
        var integration = await _integrationRepository.GetIntegrationByIdAsync(integrationId, ct)
            ?? throw new KeyNotFoundException($"Integração com ID {integrationId} não encontrada.");

        var startedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(integration.HealthCheckUrl))
        {
            var failedRun = new IntegrationRun(
                integrationId: integration.Id,
                status: "Failed",
                startedAt: startedAt,
                recordsProcessed: null,
                errorMessage: "URL de health-check não configurada para esta integração.",
                recordedBy: null,
                finishedAt: DateTime.UtcNow,
                triggeredBy: "Automated");

            await _integrationRepository.AddRunAsync(failedRun, ct);
            return failedRun;
        }

        string status = "Failed";
        string? errorMessage = null;
        long? statusCodeOrPort = null;

        var timeoutSeconds = integration.HealthCheckTimeoutSeconds > 0 ? integration.HealthCheckTimeoutSeconds : 5;

        if (string.Equals(integration.HealthCheckMethod, "Tcp", StringComparison.OrdinalIgnoreCase))
        {
            // Verificação TCP real via socket
            try
            {
                string host = integration.HealthCheckUrl.Trim();
                int port = 80;

                if (Uri.TryCreate(integration.HealthCheckUrl, UriKind.Absolute, out var uri))
                {
                    host = uri.Host;
                    port = uri.Port > 0 ? uri.Port : 80;
                }
                else if (host.Contains(':'))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    if (int.TryParse(parts[1], out var parsedPort))
                        port = parsedPort;
                }

                statusCodeOrPort = port;
                using var tcp = new TcpClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                await tcp.ConnectAsync(host, port, timeoutCts.Token);

                if (tcp.Connected)
                {
                    status = "Success";
                }
                else
                {
                    status = "Failed";
                    errorMessage = $"Não foi possível estabelecer conexão TCP com {host}:{port}.";
                }
            }
            catch (OperationCanceledException)
            {
                status = "Failed";
                errorMessage = $"Timeout de conexão TCP excedido ({timeoutSeconds}s).";
            }
            catch (Exception ex)
            {
                status = "Failed";
                errorMessage = $"Falha de conexão TCP: {ex.Message}";
            }
        }
        else
        {
            // Verificação HTTP real via HttpClient
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                var response = await client.GetAsync(integration.HealthCheckUrl, ct);
                statusCodeOrPort = (int)response.StatusCode;

                int expectedCode = integration.HealthCheckExpectedStatusCode ?? 200;
                if ((int)response.StatusCode == expectedCode)
                {
                    status = "Success";
                }
                else
                {
                    status = "Failed";
                    errorMessage = $"Código HTTP inesperado: {(int)response.StatusCode} {response.ReasonPhrase}. Esperado: {expectedCode}.";
                }
            }
            catch (OperationCanceledException)
            {
                status = "Failed";
                errorMessage = $"Timeout de requisição HTTP excedido ({timeoutSeconds}s).";
            }
            catch (Exception ex)
            {
                status = "Failed";
                errorMessage = $"Falha de rede HTTP: {ex.Message}";
            }
        }

        var run = new IntegrationRun(
            integrationId: integration.Id,
            status: status,
            startedAt: startedAt,
            recordsProcessed: statusCodeOrPort,
            errorMessage: errorMessage,
            recordedBy: null,
            finishedAt: DateTime.UtcNow,
            triggeredBy: "Automated");

        await _integrationRepository.AddRunAsync(run, ct);
        return run;
    }
}
