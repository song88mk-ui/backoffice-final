using System.Globalization;
using System.Text.Json;

namespace Backoffice.Web.Services;

/// <summary>
/// Calls the Metabase admin API with an API key (<c>x-api-key</c>). The key is added to each request
/// instead of the shared HttpClient, so it never reaches another consumer of the client or a log line.
/// </summary>
public class MetabaseAdminClient : IMetabaseAdminClient
{
    public const string HttpClientName = "metabase-admin";
    public const int PageSize = 100;

    private readonly IHttpClientFactory _clientFactory;
    private readonly Uri? _baseUri;
    private readonly string? _apiKey;

    public MetabaseAdminClient(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _clientFactory = clientFactory;

        var siteUrl = configuration["Metabase:SiteUrl"];
        var apiKey = configuration["Metabase:ApiKey"];

        if (IsUsable(siteUrl) && IsUsable(apiKey)
            && Uri.TryCreate(siteUrl!.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
        {
            _baseUri = baseUri;
            _apiKey = apiKey;
        }
    }

    public bool IsConfigured => _baseUri != null && _apiKey != null;

    public async Task<int?> FindGroupIdAsync(string groupName, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/permissions/group", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);

        foreach (var group in EnumerateItems(document.RootElement))
        {
            if (group.TryGetProperty("name", out var name)
                && string.Equals(name.GetString(), groupName, StringComparison.Ordinal)
                && group.TryGetProperty("id", out var id))
            {
                return id.GetInt32();
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<MetabaseUser>> ListActiveGroupMembersAsync(int groupId, CancellationToken cancellationToken)
    {
        var members = new List<MetabaseUser>();
        var seenIds = new HashSet<int>();
        var offset = 0;

        while (true)
        {
            using var response = await SendAsync(
                HttpMethod.Get, $"api/user?group_id={groupId}&status=active&limit={PageSize}&offset={offset}", cancellationToken);
            using var document = await ReadJsonAsync(response, cancellationToken);

            var items = EnumerateItems(document.RootElement).ToList();
            var newIds = 0;

            foreach (var item in items)
            {
                var user = ToUser(item);
                if (!seenIds.Add(user.Id))
                    continue;

                newIds++;

                // The group_id filter does the selecting. Each user's own group list is checked as well, so an account
                // that is not in the group is never returned even if the filter were ignored.
                if (IsMemberOf(item, user.Id, groupId))
                    members.Add(user);
            }

            // Advance by what the server actually returned, in case it caps the page below the requested limit.
            offset += items.Count;

            // Stop on an empty page, on a page with nothing new (the server ignored the offset), or once the
            // total Metabase reports has been read. Without a total, the empty page after the last one ends it.
            if (items.Count == 0 || newIds == 0 || offset >= GetTotal(document.RootElement))
                break;
        }

        return members;
    }

    public async Task DeactivateUserAsync(int userId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"api/user/{userId}", cancellationToken);
    }

    /// <summary>A value that is empty or still an unresolved <c>${NAME}</c> placeholder is not usable.</summary>
    public static bool IsUsable(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith("${", StringComparison.Ordinal);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativeUrl, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Metabase:SiteUrl or Metabase:ApiKey is not configured.");

        using var request = new HttpRequestMessage(method, new Uri(_baseUri!, relativeUrl));
        request.Headers.Add("x-api-key", _apiKey);

        var response = await _clientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException($"Metabase {method} {relativeUrl} failed with status {(int)statusCode}.", null, statusCode);
        }

        return response;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    /// <summary>Metabase returns either a plain array or an object with the items under "data", depending on the endpoint and version.</summary>
    private static IEnumerable<JsonElement> EnumerateItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray();

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array)
        {
            return data.EnumerateArray();
        }

        throw new InvalidOperationException("Unexpected Metabase response shape.");
    }

    /// <summary>The "total" of a paged response, or int.MaxValue when the response has none.</summary>
    private static int GetTotal(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty("total", out var total) && total.TryGetInt32(out var value)
            ? value
            : int.MaxValue;

    // Metabase reports group_ids for an administrator's list. Without it membership cannot be confirmed, and guessing could
    // deactivate an account outside the group, so the round fails (loudly) instead of continuing.
    private static bool IsMemberOf(JsonElement user, int userId, int groupId)
    {
        if (!user.TryGetProperty("group_ids", out var groupIds) || groupIds.ValueKind != JsonValueKind.Array)
            throw MissingField("group_ids", userId);

        return groupIds.EnumerateArray().Any(g => g.ValueKind == JsonValueKind.Number && g.GetInt32() == groupId);
    }

    private static InvalidOperationException MissingField(string field, int userId) =>
        new($"Metabase did not report {field} for user {userId}. Is Metabase:ApiKey an administrator key? No account was changed.");

    private static MetabaseUser ToUser(JsonElement item)
    {
        var id = item.GetProperty("id").GetInt32();
        var email = item.TryGetProperty("email", out var emailValue) ? emailValue.GetString() ?? string.Empty : string.Empty;

        // last_login is null for an account that never logged in, but the field has to be there. is_superuser has to be
        // a boolean. An account that cannot be classified is never guessed at.
        if (!item.TryGetProperty("last_login", out var lastLoginValue))
            throw MissingField("last_login", id);

        DateTimeOffset? lastLogin = null;
        if (lastLoginValue.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(lastLoginValue.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            lastLogin = parsed;
        }

        if (!item.TryGetProperty("is_superuser", out var superuserValue)
            || (superuserValue.ValueKind != JsonValueKind.True && superuserValue.ValueKind != JsonValueKind.False))
        {
            throw MissingField("is_superuser", id);
        }

        return new MetabaseUser(id, email, lastLogin, superuserValue.ValueKind == JsonValueKind.True);
    }
}
