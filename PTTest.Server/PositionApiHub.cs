using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace PTTest.Server;

/// <summary>
/// Exposes methods that clients may call via the SignalR connection, as well as
/// connection lifecycle methods.
/// </summary>
public class PositionApiHub : Hub
{
    private const string InitializeName = "Initialize";

    // Tracks connection IDs and all connected players.
    private static readonly ConcurrentDictionary<string, Guid> _activeConnections = new();

    private readonly ILogger<PositionApiHub> _logger;
    private readonly IPositionService _positionService;

    public PositionApiHub(ILogger<PositionApiHub> logger, IPositionService positionService)
    {
        _logger = logger;
        _positionService = positionService;
    }

    public override Task OnConnectedAsync()
    {
        // Whenever a client (re)connects, give them a new ID.
        Guid playerId = Guid.NewGuid();
        _activeConnections.AddOrUpdate(Context.ConnectionId, playerId, (cid, pid) => pid);
        Clients.Caller.SendAsync(InitializeName, playerId);

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        bool removed = _activeConnections.TryRemove(Context.ConnectionId, out Guid playerId);
        if (removed)
        {
            _positionService.RemovePlayer(playerId);
        }

        _logger.LogInformation(
            "Attempted to remove connection {cid} associated with player {pid}. Success?: {removed}",
            Context.ConnectionId,
            playerId,
            removed
        );

        // Room for improvement: Turn off the PositionService and TickService if no clients connected;

        return Task.CompletedTask;
    }

    // Clients must use this method name with exact spelling in order to call it.
    public Task SendPosition(Guid playerId, float x, float y)
    {
        _positionService.WritePosition(new PlayerPosition(playerId, x, y));
        return Task.CompletedTask;
    }
}
