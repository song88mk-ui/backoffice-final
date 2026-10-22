using System.Net;
using System.Text.Json;
using Backoffice.Web.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class MetabaseAdminClientTests
    {
        private const string ApiKey = "mb_test_api_key_value";
        private const string SiteUrl = "https://metabase.example";

        private static readonly int[] GroupSeven = { 7 };
        private static readonly int[] OtherGroups = { 1, 2 };
        private static readonly int[] OneAndTwo = { 1, 2 };
        private static readonly int[] OneAndThree = { 1, 3 };
        private static readonly int[] ExpectedOffsets = { 0, 50, 100 };

        private sealed record CapturedRequest(HttpMethod Method, string Url, string? ApiKeyHeader);

        private static (MetabaseAdminClient sut, List<CapturedRequest> requests) BuildSut(
            Func<HttpRequestMessage, HttpResponseMessage>? responder = null,
            string? siteUrl = SiteUrl,
            string? apiKey = ApiKey)
        {
            var requests = new List<CapturedRequest>();
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken _) =>
                {
                    requests.Add(new CapturedRequest(
                        request.Method,
                        request.RequestUri!.ToString(),
                        request.Headers.TryGetValues("x-api-key", out var values) ? values.Single() : null));

                    return Task.FromResult(responder != null ? responder(request) : Json(HttpStatusCode.OK, "[]"));
                });

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(MetabaseAdminClient.HttpClientName)).Returns(() => new HttpClient(handler.Object));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Metabase:SiteUrl"] = siteUrl,
                    ["Metabase:ApiKey"] = apiKey
                })
                .Build();

            return (new MetabaseAdminClient(factory.Object, configuration), requests);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body) };

        // Shaped like an administrator's GET /api/user item. A field is left out only when a test asks for it.
        private static Dictionary<string, object?> User(
            int id,
            string? lastLogin = "2026-10-20T06:00:00Z",
            bool? superuser = false,
            int[]? groupIds = null,
            bool omitLastLogin = false,
            bool omitGroupIds = false)
        {
            var user = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["email"] = $"u{id}@clients.song88.com"
            };

            if (!omitLastLogin)
                user["last_login"] = lastLogin;
            if (superuser != null)
                user["is_superuser"] = superuser;
            if (!omitGroupIds)
                user["group_ids"] = groupIds ?? GroupSeven;

            return user;
        }

        private static IEnumerable<Dictionary<string, object?>> UsersFrom(int firstId, int count, int[]? groupIds = null) =>
            Enumerable.Range(firstId, count).Select(id => User(id, groupIds: groupIds));

        // A plain array, as older Metabase versions and the group list return.
        private static string Users(params Dictionary<string, object?>[] users) => JsonSerializer.Serialize(users);

        private static string Users(IEnumerable<Dictionary<string, object?>> users) => JsonSerializer.Serialize(users);

        // The paged shape Metabase 0.50 returns for GET /api/user.
        private static string Page(int total, IEnumerable<Dictionary<string, object?>> users) =>
            "{\"data\":" + JsonSerializer.Serialize(users) + ",\"total\":" + total + ",\"limit\":100,\"offset\":0}";

        private static int OffsetOf(HttpRequestMessage request) =>
            int.Parse(System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query)["offset"]!);

        [Theory]
        [InlineData("https://metabase.example", "key", true)]
        [InlineData("http://localhost:5999", "key", true)]
        [InlineData("https://metabase.example", null, false)]
        [InlineData("https://metabase.example", "", false)]
        [InlineData("https://metabase.example", "${METABASE_API_KEY}", false)]
        [InlineData(null, "key", false)]
        [InlineData("${METABASE_SITE_URL}", "key", false)]
        [InlineData("not a url", "key", false)]
        [InlineData("localhost:5999", "key", false)]
        [InlineData("ftp://metabase.example", "key", false)]
        public void IsConfigured_NeedsARealHttpSiteUrlAndApiKey(string? siteUrl, string? apiKey, bool expected)
        {
            var (sut, _) = BuildSut(siteUrl: siteUrl, apiKey: apiKey);

            Assert.Equal(expected, sut.IsConfigured);
        }

        [Fact]
        public async Task FindGroupIdAsync_ReturnsTheIdOfTheGroupWithTheExactName()
        {
            var (sut, requests) = BuildSut(_ => Json(HttpStatusCode.OK,
                "[{\"id\":1,\"name\":\"All Users\"},{\"id\":3,\"name\":\"Clients\"},{\"id\":4,\"name\":\"clients\"}]"));

            var groupId = await sut.FindGroupIdAsync("Clients", CancellationToken.None);

            Assert.Equal(3, groupId);
            var request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://metabase.example/api/permissions/group", request.Url);
            Assert.Equal(ApiKey, request.ApiKeyHeader);
        }

        [Fact]
        public async Task FindGroupIdAsync_NoGroupWithThatExactName_ReturnsNull()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, "[{\"id\":4,\"name\":\"clients\"},{\"id\":5,\"name\":\"Clients 2\"}]"));

            Assert.Null(await sut.FindGroupIdAsync("Clients", CancellationToken.None));
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_AsksMetabaseForActiveMembersOfTheGroup()
        {
            var (sut, requests) = BuildSut(_ => Json(HttpStatusCode.OK, Page(1, UsersFrom(1, 1))));

            await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            var request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://metabase.example/api/user?group_id=7&status=active&limit=100&offset=0", request.Url);
            Assert.Equal(ApiKey, request.ApiKeyHeader);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ListActiveGroupMembersAsync_ReadsAPlainArrayAndADataEnvelope(bool envelope)
        {
            var users = UsersFrom(1, 2).ToList();
            var body = envelope ? Page(2, users) : Users(users);
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, body));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(OneAndTwo, members.Select(m => m.Id));
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_ParsesEmailLastLoginAndSuperuserFlag()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(2, new[]
            {
                User(1, lastLogin: "2026-10-20T06:15:30.123456Z", superuser: false),
                User(2, lastLogin: null, superuser: true)
            })));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal("u1@clients.song88.com", members[0].Email);
            Assert.Equal(new DateTimeOffset(2026, 10, 20, 6, 15, 30, TimeSpan.Zero).AddTicks(1234560), members[0].LastLogin);
            Assert.False(members[0].IsSuperuser);
            Assert.Null(members[1].LastLogin);
            Assert.True(members[1].IsSuperuser);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_ReadsALastLoginWithAnOffset()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(1, new[] { User(1, lastLogin: "2026-10-20T14:00:00+08:00") })));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(new DateTimeOffset(2026, 10, 20, 6, 0, 0, TimeSpan.Zero), members[0].LastLogin);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_UserWithoutASuperuserFlag_FailsTheListing()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(2, new[] { User(1), User(2, superuser: null) })));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListActiveGroupMembersAsync(7, CancellationToken.None));

            Assert.Contains("is_superuser", ex.Message);
            Assert.Contains("user 2", ex.Message);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_UserWithoutGroupIds_FailsTheListing()
        {
            // Membership that cannot be confirmed must never turn into "member": nothing may be deactivated on a guess.
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(2, new[] { User(1), User(2, omitGroupIds: true) })));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListActiveGroupMembersAsync(7, CancellationToken.None));

            Assert.Contains("group_ids", ex.Message);
            Assert.Contains("user 2", ex.Message);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_UserWithoutALastLoginField_FailsTheListing()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(1, new[] { User(1, omitLastLogin: true) })));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListActiveGroupMembersAsync(7, CancellationToken.None));

            Assert.Contains("last_login", ex.Message);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_FollowsPagesUntilTheTotalIsReached()
        {
            var (sut, requests) = BuildSut(request => OffsetOf(request) == 0
                ? Json(HttpStatusCode.OK, Page(103, UsersFrom(1, 100)))
                : Json(HttpStatusCode.OK, Page(103, UsersFrom(101, 3))));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(103, members.Count);
            Assert.Equal(2, requests.Count);
            Assert.EndsWith("limit=100&offset=100", requests[1].Url);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_WithoutATotal_StopsAtTheEmptyPage()
        {
            var (sut, requests) = BuildSut(request => OffsetOf(request) switch
            {
                0 => Json(HttpStatusCode.OK, Users(UsersFrom(1, 100))),
                100 => Json(HttpStatusCode.OK, Users(UsersFrom(101, 3))),
                _ => Json(HttpStatusCode.OK, "[]")
            });

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(103, members.Count);
            Assert.Equal(3, requests.Count);
            Assert.EndsWith("limit=100&offset=103", requests[2].Url);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_ServerCappingThePageBelowTheLimit_StillReadsEveryMember()
        {
            // Metabase versions may return fewer rows than the limit asked for.
            var (sut, requests) = BuildSut(request =>
            {
                var offset = OffsetOf(request);
                return Json(HttpStatusCode.OK, Page(120, UsersFrom(offset + 1, Math.Min(50, 120 - offset))));
            });

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(120, members.Count);
            Assert.Equal(ExpectedOffsets, requests.Select(r => int.Parse(r.Url.Split("offset=")[1])).ToArray());
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_ServerIgnoringTheOffset_StopsInsteadOfLooping()
        {
            var (sut, requests) = BuildSut(_ => Json(HttpStatusCode.OK, Users(UsersFrom(1, MetabaseAdminClient.PageSize))));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(MetabaseAdminClient.PageSize, members.Count);
            Assert.Equal(2, requests.Count);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_SkipsAUserWhoseGroupsDoNotIncludeTheGroup()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, Page(3, new[]
            {
                User(1, groupIds: GroupSeven),
                User(2, groupIds: OtherGroups),
                User(3)
            })));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(OneAndThree, members.Select(m => m.Id));
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_AWholePageOutsideTheGroup_DoesNotStopPaging()
        {
            // Only reachable if the server ignored group_id, but the next page must still be read.
            var (sut, requests) = BuildSut(request => OffsetOf(request) == 0
                ? Json(HttpStatusCode.OK, Page(101, UsersFrom(1, 100, OtherGroups)))
                : Json(HttpStatusCode.OK, Page(101, new[] { User(101, groupIds: GroupSeven) })));

            var members = await sut.ListActiveGroupMembersAsync(7, CancellationToken.None);

            Assert.Equal(101, Assert.Single(members).Id);
            Assert.Equal(2, requests.Count);
        }

        [Fact]
        public async Task ListActiveGroupMembersAsync_UnexpectedResponseShape_Throws()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, "{\"message\":\"nope\"}"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListActiveGroupMembersAsync(7, CancellationToken.None));
        }

        [Fact]
        public async Task DeactivateUserAsync_DeletesTheUser()
        {
            var (sut, requests) = BuildSut(_ => Json(HttpStatusCode.OK, "{\"success\":true}"));

            await sut.DeactivateUserAsync(42, CancellationToken.None);

            var request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("https://metabase.example/api/user/42", request.Url);
            Assert.Equal(ApiKey, request.ApiKeyHeader);
        }

        [Fact]
        public async Task SiteUrlWithAPathAndTrailingSlash_KeepsThePath()
        {
            var (sut, requests) = BuildSut(_ => Json(HttpStatusCode.OK, "{}"), siteUrl: "https://example.com/metabase/");

            await sut.DeactivateUserAsync(5, CancellationToken.None);

            Assert.Equal("https://example.com/metabase/api/user/5", Assert.Single(requests).Url);
        }

        [Fact]
        public async Task FailedRequest_ThrowsWithTheStatusAndNeverTheApiKey()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.Forbidden, "{\"message\":\"You don't have permissions to do that.\"}"));

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.DeactivateUserAsync(42, CancellationToken.None));

            Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
            Assert.DoesNotContain(ApiKey, ex.ToString());
        }

        [Fact]
        public async Task NotConfigured_ThrowsAndMakesNoRequest()
        {
            var (sut, requests) = BuildSut(apiKey: "${METABASE_API_KEY}");

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.FindGroupIdAsync("Clients", CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListActiveGroupMembersAsync(7, CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeactivateUserAsync(1, CancellationToken.None));

            Assert.Empty(requests);
        }
    }
}
