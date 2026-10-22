namespace Backoffice.Web.Services;

public record MetabaseUser(int Id, string Email, DateTimeOffset? LastLogin, bool IsSuperuser);

/// <summary>The few Metabase admin API calls the idle account cleanup needs.</summary>
public interface IMetabaseAdminClient
{
    /// <summary>False when <c>Metabase:SiteUrl</c> or <c>Metabase:ApiKey</c> is missing or an unresolved placeholder.</summary>
    bool IsConfigured { get; }

    /// <summary>Returns the id of the group with exactly this name, or null when there is none.</summary>
    Task<int?> FindGroupIdAsync(string groupName, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every active member of the group. Throws when Metabase does not report what is needed to classify an
    /// account (group_ids, last_login, is_superuser), so nothing is changed on a guess.
    /// </summary>
    Task<IReadOnlyList<MetabaseUser>> ListActiveGroupMembersAsync(int groupId, CancellationToken cancellationToken);

    /// <summary>Deactivates the account. Metabase keeps it and reactivates it on the next SSO login.</summary>
    Task DeactivateUserAsync(int userId, CancellationToken cancellationToken);
}
