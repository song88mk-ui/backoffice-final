using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Backoffice.Web.Services;

public record MetabaseIdleCleanupRound(
    bool GroupFound,
    int Members,
    int Deactivated,
    int KeptActive,
    int SkippedSuperuser,
    int SkippedNoLogin,
    int Failed);

/// <summary>
/// Deactivates Metabase accounts of the "Clients" group that have not logged in for a while (GM-987).
/// Every BO user gets one Metabase account through the SSO login, and Metabase bills every active account.
/// A deactivated account is reactivated by the next SSO login (Metabase's JWT user provisioning), so only accounts
/// in use at the same time are paid for.
/// The job lives in the web app, so nothing in it may let an exception escape: an unhandled exception in a hosted
/// service stops the host. That includes a setting that cannot be bound or converted, so the settings are read
/// inside the guarded part and not in the constructor.
/// </summary>
public class MetabaseIdleUserCleanupService : BackgroundService
{
    private const double MaxIdleHours = 24 * 366;
    private const double MinIntervalMinutes = 0.001;
    private const double MaxIntervalMinutes = 24 * 60;
    private const double MaxStartupDelaySeconds = 60 * 60;

    private readonly IMetabaseAdminClient _client;
    private readonly IOptions<MetabaseIdleCleanupOptions> _optionsAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MetabaseIdleUserCleanupService> _logger;

    public MetabaseIdleUserCleanupService(
        IMetabaseAdminClient client,
        IOptions<MetabaseIdleCleanupOptions> options,
        TimeProvider timeProvider,
        ILogger<MetabaseIdleUserCleanupService> logger)
    {
        _client = client;
        _optionsAccessor = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private MetabaseIdleCleanupOptions Options => _optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!Options.Enabled)
            {
                _logger.LogInformation("Metabase idle user cleanup is disabled");
                return;
            }

            if (!TryValidateSettings(out var problem))
            {
                _logger.LogError("Metabase idle user cleanup is enabled but will not run: {Problem}", problem);
                return;
            }

            _logger.LogInformation(
                "Metabase idle user cleanup started: Group {GroupName}, IdleHours {IdleHours}, IntervalMinutes {IntervalMinutes}",
                Options.GroupName, Options.IdleHours, Options.IntervalMinutes);

            await Task.Delay(TimeSpan.FromSeconds(Options.StartupDelaySeconds), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Options.IntervalMinutes));
            do
            {
                await RunRoundGuardedAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Metabase idle user cleanup stopped");
        }
        catch (Exception ex)
        {
            // Not rethrown on purpose: the app must keep running without the job.
            _logger.LogError(ex, "Metabase idle user cleanup stopped because of an unexpected error and will not run again until the app restarts");
        }
    }

    /// <summary>Runs one round and logs a failure instead of throwing, so the next interval retries it.</summary>
    public async Task RunRoundGuardedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunRoundAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metabase idle user cleanup round failed. It will be retried in {IntervalMinutes} minutes", Options.IntervalMinutes);
        }
    }

    public async Task<MetabaseIdleCleanupRound> RunRoundAsync(CancellationToken cancellationToken)
    {
        var duration = Stopwatch.StartNew();
        var cutoff = _timeProvider.GetUtcNow() - TimeSpan.FromHours(Options.IdleHours);

        var groupId = await _client.FindGroupIdAsync(Options.GroupName, cancellationToken);
        if (groupId == null)
        {
            _logger.LogWarning("Metabase idle user cleanup found no group named {GroupName}, nothing was changed", Options.GroupName);
            return new MetabaseIdleCleanupRound(false, 0, 0, 0, 0, 0, 0);
        }

        var members = await _client.ListActiveGroupMembersAsync(groupId.Value, cancellationToken);

        int deactivated = 0, keptActive = 0, skippedSuperuser = 0, skippedNoLogin = 0, failed = 0;

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member.IsSuperuser)
            {
                skippedSuperuser++;
            }
            else if (member.LastLogin == null)
            {
                skippedNoLogin++;
            }
            else if (member.LastLogin.Value >= cutoff)
            {
                keptActive++;
            }
            else
            {
                try
                {
                    await _client.DeactivateUserAsync(member.Id, cancellationToken);
                    deactivated++;
                    _logger.LogInformation(
                        "Metabase idle user cleanup deactivated user {UserId} {Email}, last login {LastLogin}",
                        member.Id, member.Email, member.LastLogin);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Metabase idle user cleanup failed to deactivate user {UserId} {Email}", member.Id, member.Email);
                }
            }
        }

        _logger.LogInformation(
            "Metabase idle user cleanup round finished: Group {GroupName}, Members {Members}, Deactivated {Deactivated}, KeptActive {KeptActive}, SkippedSuperuser {SkippedSuperuser}, SkippedNoLogin {SkippedNoLogin}, Failed {Failed}, DurationMs {DurationMs}",
            Options.GroupName, members.Count, deactivated, keptActive, skippedSuperuser, skippedNoLogin, failed, duration.ElapsedMilliseconds);

        return new MetabaseIdleCleanupRound(true, members.Count, deactivated, keptActive, skippedSuperuser, skippedNoLogin, failed);
    }

    // The bounds keep every value convertible to a TimeSpan and above the 1 ms floor of PeriodicTimer.
    private bool TryValidateSettings(out string problem)
    {
        var options = Options;

        if (!_client.IsConfigured)
            problem = "Metabase:SiteUrl or Metabase:ApiKey is missing or an unresolved placeholder";
        else if (string.IsNullOrWhiteSpace(options.GroupName))
            problem = "Metabase:IdleCleanup:GroupName is empty";
        else if (!(options.IdleHours > 0 && options.IdleHours <= MaxIdleHours))
            problem = $"Metabase:IdleCleanup:IdleHours must be greater than 0 and at most {MaxIdleHours}";
        else if (!(options.IntervalMinutes >= MinIntervalMinutes && options.IntervalMinutes <= MaxIntervalMinutes))
            problem = $"Metabase:IdleCleanup:IntervalMinutes must be between {MinIntervalMinutes} and {MaxIntervalMinutes}";
        else if (!(options.StartupDelaySeconds >= 0 && options.StartupDelaySeconds <= MaxStartupDelaySeconds))
            problem = $"Metabase:IdleCleanup:StartupDelaySeconds must be between 0 and {MaxStartupDelaySeconds}";
        else
            problem = string.Empty;

        return problem.Length == 0;
    }
}
