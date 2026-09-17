using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TraceCore.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker executing.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}