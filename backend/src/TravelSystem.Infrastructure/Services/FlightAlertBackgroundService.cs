using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelSystem.Application.Interfaces;

namespace TravelSystem.Infrastructure.Services;

/// <summary>
/// Background service that checks flight price alerts every hour.
/// Runs as a hosted service registered in Program.cs.
/// </summary>
public class FlightAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FlightAlertBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public FlightAlertBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FlightAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Flight Alert Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking flight alerts.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Checking flight price alerts at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
        await flightService.CheckAlertsAsync(ct);

        _logger.LogInformation("Flight alert check completed.");
    }
}
