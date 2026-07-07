using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oryxen.Application.Telemetry;

namespace Oryxen.Infrastructure.Workers;

public sealed class IoTSimulationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IoTSimulationWorker> _logger;
    private readonly bool _enabled;
    private readonly int _intervalMinutes;

    public IoTSimulationWorker(
        IServiceProvider serviceProvider, 
        IConfiguration configuration,
        ILogger<IoTSimulationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _enabled = configuration.GetValue("IoTSimulation:Enabled", false);
        _intervalMinutes = configuration.GetValue("IoTSimulation:IntervalMinutes", 15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("IoT Simulation Worker is disabled in configuration.");
            return;
        }

        _logger.LogInformation("IoT Simulation Worker started. Running every {Interval} minutes.", _intervalMinutes);

        // Optional: Wait a bit before starting the first cycle to let the app fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var simulator = scope.ServiceProvider.GetRequiredService<IIoTSimulationService>();
                
                await simulator.GenerateRealtimeReadingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating real-time IoT readings.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
        }
    }
}
