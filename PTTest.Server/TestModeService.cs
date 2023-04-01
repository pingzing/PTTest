namespace PTTest.Server;

/// <summary>
/// Service for stress-testing the system. Simulates multiple players sending requests simultaneously.
/// </summary>
public class TestModeService
{
    private readonly ILogger<TestModeService> _logger;
    private readonly IPositionService _positionService;

    private List<Guid> _testModePlayers = new();
    private CancellationTokenSource? _testModeCancellationToken;

    private object TestModeLock = new object();
    private bool _isTestModeRunning;
    private bool IsTestModeRunning
    {
        get
        {
            lock (TestModeLock)
            {
                return _isTestModeRunning;
            }
        }
        set
        {
            lock (TestModeLock)
            {
                _isTestModeRunning = value;
            }
        }
    }

    public TestModeService(ILogger<TestModeService> logger, IPositionService positionService)
    {
        _logger = logger;
        _positionService = positionService;
    }

    public Task EngageTestMode(int playerCount)
    {
        if (IsTestModeRunning)
        {
            return Task.CompletedTask;
        }

        if (playerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerCount),
                "playerCount must be positive."
            );
        }

        IsTestModeRunning = true;

        // Simulate a bunch of users all connecting at once
        _testModeCancellationToken = new CancellationTokenSource();
        _testModePlayers = Enumerable.Range(0, playerCount).Select(x => Guid.NewGuid()).ToList();
        return Task.Run(() =>
        {
            while (!_testModeCancellationToken.Token.IsCancellationRequested)
            {
                Parallel.ForEach(
                    _testModePlayers,
                    (playerId, token) =>
                    {
                        int randX = Random.Shared.Next(0, 400);
                        int randY = Random.Shared.Next(0, 400);
                        _positionService.WritePosition(new PlayerPosition(playerId, randX, randY));
                    }
                );
            }
        });
    }

    public void EndTestMode()
    {
        _testModeCancellationToken?.Cancel();
        IsTestModeRunning = false;

        foreach (var testPlayer in _testModePlayers)
        {
            _positionService.RemovePlayer(testPlayer);
        }
    }
}
