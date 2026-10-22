namespace Backoffice.Web.Services;

/// <summary>
/// Settings of the job that deactivates idle Metabase accounts (GM-987), bound from <c>Metabase:IdleCleanup</c>.
/// The job is off unless <see cref="Enabled"/> is set.
/// </summary>
public class MetabaseIdleCleanupOptions
{
    public const string SectionName = "Metabase:IdleCleanup";

    public bool Enabled { get; set; }

    /// <summary>A "Clients" member whose last login is older than this many hours is deactivated.</summary>
    public double IdleHours { get; set; } = 3;

    public double IntervalMinutes { get; set; } = 15;

    /// <summary>Pause before the first round so the job does not compete with application startup.</summary>
    public double StartupDelaySeconds { get; set; } = 30;

    /// <summary>The Metabase group the job is allowed to touch. Matched by exact name.</summary>
    public string GroupName { get; set; } = "Clients";
}
