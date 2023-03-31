using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace PTTest.Server;

public class PositionHub : Hub
{
    private const string PushPositionsName = "PushPositions";

    private readonly ConcurrentDictionary<string, Guid> _activeConnections = new();
    private readonly ILogger<PositionHub> _logger;
    private readonly IPositionService _positionService;

    public PositionHub(ILogger<PositionHub> logger, IPositionService positionService)
    {
        _logger = logger;
        _positionService = positionService;
    }

    // Client methods
    public override Task OnConnectedAsync()
    {
        // Whenever a client (re)connects, give them a new ID.
        Guid playerId = Guid.NewGuid();
        _activeConnections.AddOrUpdate(Context.ConnectionId, playerId, (cid, pid) => pid);
        Clients.Caller.SendAsync("Initialize", playerId);

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        bool removed = _activeConnections.TryRemove(Context.ConnectionId, out Guid playerId);
        _logger.LogInformation(
            "Attempted to remove connection {cid} associated with player {pid}. Success?: {removed}",
            Context.ConnectionId,
            playerId,
            removed
        );

        // TODO: Pause ticking if this was the last client.

        return Task.CompletedTask;
    }

    public Task SendPosition(Guid playerId, float x, float y)
    {
        _positionService.WritePosition(new PlayerPosition(playerId, x, y));
        return Task.CompletedTask;
    }

    // Server methods

    /// <summary>
    /// Send the latest set of positions to all clients.
    /// </summary>
    /// <param name="positions">The most up-to-date list of positions.</param>
    public async Task UpdateClients(ICollection<PlayerPosition> positions)
    {
        await Clients.All.SendAsync(PushPositionsName, positions);
    }
}
