namespace GuessWho.Services;

/// <summary>
/// Background service that periodically removes stale game sessions from memory.
/// Runs every <see cref="CleanupInterval"/> and delegates to
/// <see cref="GameSessionService.RemoveStaleSessions"/>, which removes sessions that
/// have ended (<c>GameEnd</c> phase) or have been idle longer than
/// <see cref="GameSessionService.SessionIdleTimeout"/>.
/// </summary>
public sealed class SessionCleanupService : BackgroundService
{
    /// <summary>How often the cleanup pass runs.</summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);

    private readonly GameSessionService _sessionService;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(
        GameSessionService sessionService,
        ILogger<SessionCleanupService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Session cleanup service started. Interval: {Interval}, Idle timeout: {IdleTimeout}",
            CleanupInterval,
            GameSessionService.SessionIdleTimeout);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Application is shutting down — exit cleanly.
                break;
            }

            var removed = _sessionService.RemoveStaleSessions();

            if (removed > 0)
                _logger.LogInformation(
                    "Session cleanup: removed {Count} stale session(s).", removed);
            else
                _logger.LogDebug("Session cleanup: no stale sessions found.");
        }

        _logger.LogInformation("Session cleanup service stopped.");
    }
}
