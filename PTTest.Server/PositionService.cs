using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace PTTest.Server;

/// <summary>
/// Service for maintaining an up-to-date list of where every player is located.
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// Write a position update to the update stream.
    /// </summary>
    /// <param name="position">The position to write.</param>
    void WritePosition(PlayerPosition position);

    /// <summary>
    /// Get the most up-to-date list of positions.
    /// </summary>
    ICollection<PlayerPosition> GetLatestPositions();

    /// <summary>
    /// Remove the given player from the maintained list of positions.
    /// </summary>
    /// <param name="playerId">ID of the player to remove.</param>
    void RemovePlayer(Guid playerId);
}

public class PositionService : BackgroundService, IPositionService
{
    private readonly ConcurrentDictionary<Guid, PlayerPosition> _currentPositions = new();
    private readonly Channel<PlayerPosition> _positionUpdates;
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
                _currentPositions.AddOrUpdate(posUpdate.Id, posUpdate, (id, newPos) => posUpdate);
            }
        }
    }

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

    public ICollection<PlayerPosition> GetLatestPositions()
    {
        return _currentPositions.Values.ToImmutableArray();
    }

    public void RemovePlayer(Guid playerId)
    {
        bool playerRemoved = _currentPositions.TryRemove(playerId, out _);
        _logger.LogInformation(
            "Attempted to remove player {pid} from PositionService. Succes?: {removed}",
            playerId,
            playerRemoved
        );
    }
}
