using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace PTTest.Server;

// The interface isn't strictly necessary, but would make unit testing easier.
// Also limits API surface area.
public interface IPositionService
{
    void WritePosition(PlayerPosition position);
    ICollection<PlayerPosition> GetLatestPositions();
}

public class PositionService : BackgroundService, IPositionService
{
    // A nice-to-have would be TTLs for entries in _currentPositions, so we could remove players who have been idle too long.
    private ConcurrentDictionary<Guid, PlayerPosition> _currentPositions = new();
    private Channel<PlayerPosition> _positionUpdates;
    private readonly ILogger<PositionService> _logger;

    public PositionService(ILogger<PositionService> logger)
    {
        _positionUpdates = Channel.CreateBounded<PlayerPosition>(
            new BoundedChannelOptions(capacity: 5000) // TODO: Probably want this configurable.
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            }
        );
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdatePositions(stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _positionUpdates.Writer.TryComplete();
        return Task.CompletedTask;
    }

    private async Task UpdatePositions(CancellationToken stoppingToken)
    {
        // Run forever, until the channel is closed, and keep _currentPositions as up-to-date as possible.
        while (await _positionUpdates.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_positionUpdates.Reader.TryRead(out PlayerPosition posUpdate))
            {
                _currentPositions.AddOrUpdate(posUpdate.Id, posUpdate, (id, newPos) => newPos);
            }
        }
    }

    /// <summary>
    /// Write a position update to the update stream.
    /// </summary>
    /// <param name="position">The position to write.</param>
    public void WritePosition(PlayerPosition position)
    {
        bool writeSuccees = _positionUpdates.Writer.TryWrite(position);
        if (!writeSuccees)
        {
            _logger.LogInformation(
                "Dropping position update from {playerId} because channel is shut down.",
                position.Id
            );
        }
    }

    /// <summary>
    /// Get the most up-to-date list of positions.
    /// </summary>
    public ICollection<PlayerPosition> GetLatestPositions()
    {
        return _currentPositions.Values.ToImmutableArray();
    }
}
