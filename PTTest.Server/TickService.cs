using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace PTTest.Server;

/// <summary>
/// Service for ticking the state of the server forward, and communicating that state to all clients.
/// </summary>
public class TickService : BackgroundService
{
    private const int TickRate = 30;
    private const string PushPositionsName = "PushPositions";
    private static readonly TimeSpan TimePerTick = TimeSpan.FromMilliseconds(1000 / TickRate);

    private readonly ILogger<TickService> _logger;
    private readonly IPositionService _positionService;
    private readonly IHubContext<PositionApiHub> _positionHub;

    private Stopwatch _stopwatch;

    public TickService(
        ILogger<TickService> logger,
        IPositionService positionService,
        IHubContext<PositionApiHub> positionHub
    )
    {
        _logger = logger;
        _positionService = positionService;
        _positionHub = positionHub;

        _stopwatch = new Stopwatch();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _stopwatch.Restart();

            await Tick(stoppingToken);

            _stopwatch.Stop();
            TimeSpan elapsedTime = _stopwatch.Elapsed;

            // If we completed the current tick faster than our tick rate, sleep the difference away.
            if (elapsedTime < TimePerTick)
            {
                TimeSpan difference = TimePerTick - elapsedTime;
                await Task.Delay(difference);
            }
        }
    }

    private async Task Tick(CancellationToken stoppingToken)
    {
        ICollection<PlayerPosition> latestPositions = _positionService.GetLatestPositions();
        await _positionHub.Clients.All.SendAsync(
            method: PushPositionsName,
            latestPositions,
            stoppingToken
        );
    }
}
