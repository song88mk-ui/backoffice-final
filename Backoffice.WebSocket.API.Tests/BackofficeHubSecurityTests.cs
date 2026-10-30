using System.Security.Claims;
using System.Text;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.OutgoingWebSocketMessages;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backoffice.WebSocket.API.Tests
{
    // BO-131: runs the real hub in a test server and talks to it with the same SignalR client the
    // Web and WebSocket.Service use, so the checks cover negotiate, connect and method invocation.
    public class BackofficeHubSecurityTests : IDisposable
    {
        private const string PublisherKey = "publisher-key";
        private const string SubscriberKey = "subscriber-key";
        private const string JwtIssuer = "bo.auth.api";
        private const string JwtKey = "test-jwt-signing-key-of-at-least-32-bytes";
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(500);

        private readonly List<HubConnection> _connections = new();
        private WebApplicationFactory<Program> _factory;

        private WebApplicationFactory<Program> StartHub(bool enforce)
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Security:Enforce", enforce.ToString());
                builder.UseSetting("Security:PublisherApiKey", PublisherKey);
                builder.UseSetting("Security:SubscriberApiKey", SubscriberKey);
                builder.UseSetting("API:AuthenticationAPI:JWT:Key", JwtKey);
                builder.UseSetting("API:AuthenticationAPI:JWT:Issuer", JwtIssuer);
                builder.UseSetting("Telemetry:Enabled", "false");
            });
            _factory.Server.BaseAddress = new Uri("http://localhost/");
            return _factory;
        }

        private HubConnection Connect(string apiKey = null, string userToken = null, string queryUserId = null)
        {
            var url = new Uri(_factory.Server.BaseAddress, queryUserId == null ? "bohub" : $"bohub?userId={queryUserId}");
            var connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    if (apiKey != null)
                        options.Headers["x-api-key"] = apiKey;
                    if (userToken != null)
                        options.AccessTokenProvider = () => Task.FromResult(userToken);
                })
                .AddMessagePackProtocol(options =>
                {
                    options.SerializerOptions = MessagePackSerializerOptions.Standard
                        .WithResolver(ContractlessStandardResolver.Instance)
                        .WithSecurity(MessagePackSecurity.UntrustedData);
                })
                .Build();
            _connections.Add(connection);
            return connection;
        }

        private static string UserToken(string userId, string key = JwtKey, string issuer = JwtIssuer) =>
            new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("UserId", userId) }),
                Issuer = issuer,
                Audience = issuer,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
            });

        private static TaskCompletionSource<NotificationsModel> ListenForNotification(HubConnection connection)
        {
            var received = new TaskCompletionSource<NotificationsModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.On<NotificationsModel>("OnNotification", message => received.TrySetResult(message));
            return received;
        }

        private static OutgoingNotificationMessage Notification(string userId, string details) => new()
        {
            Gr = null,
            UsId = userId,
            Pa = new NotificationsModel { Id = Guid.NewGuid(), Details = details, UserId = userId }
        };

        private static async Task<bool> ArrivesWithin(Task task, TimeSpan timeout) => await Task.WhenAny(task, Task.Delay(timeout)) == task;

        [Fact]
        public async Task Enforced_ConnectWithoutApiKey_IsRefused()
        {
            StartHub(enforce: true);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Connect().StartAsync());
            Assert.Contains("401", ex.Message);
        }

        [Fact]
        public async Task Enforced_ConnectWithUnknownApiKey_IsRefused()
        {
            StartHub(enforce: true);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Connect(apiKey: "guessed-key").StartAsync());
            Assert.Contains("401", ex.Message);
        }

        [Fact]
        public async Task Enforced_ConnectWithForgedUserToken_IsRefused()
        {
            StartHub(enforce: true);
            var forged = UserToken("victim", key: "some-other-signing-key-of-32-bytes-or-more");

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Connect(SubscriberKey, forged).StartAsync());
            Assert.Contains("401", ex.Message);
        }

        [Fact]
        public async Task Enforced_SubscriberCannotPublish_AndNothingIsBroadcast()
        {
            StartHub(enforce: true);
            var listener = Connect(SubscriberKey, UserToken("user-1"));
            var received = ListenForNotification(listener);
            await listener.StartAsync();

            var attacker = Connect(SubscriberKey);
            await attacker.StartAsync();

            await Assert.ThrowsAsync<HubException>(() => attacker.InvokeAsync("PublishNotification", Notification(null, "fake pop-up")));
            await Assert.ThrowsAsync<HubException>(() => attacker.InvokeAsync("PublishBetStopGlobal", new OutgoingBetStopGlobalMessage { Pa = new BetStopGlobalMessage { IsBetStop = true } }));
            Assert.False(await ArrivesWithin(received.Task, Quiet));
        }

        [Fact]
        public async Task Enforced_PublisherBetStopGlobal_ReachesSubscribedScreens()
        {
            StartHub(enforce: true);
            var subscriber = Connect(SubscriberKey, UserToken("user-1"));
            var received = new TaskCompletionSource<BetStopGlobalMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            subscriber.On<BetStopGlobalMessage>("OnBetStopGlobal", message => received.TrySetResult(message));
            await subscriber.StartAsync();
            await subscriber.InvokeAsync("SubscribeToChannel", "onbetstopglobal");

            var publisher = Connect(PublisherKey);
            await publisher.StartAsync();
            await publisher.InvokeAsync("PublishBetStopGlobal", new OutgoingBetStopGlobalMessage { Pa = new BetStopGlobalMessage { IsBetStop = true, CorrelationId = "c-1" } });

            Assert.True(await ArrivesWithin(received.Task, Wait));
            Assert.Equal("c-1", received.Task.Result.CorrelationId);
        }

        [Fact]
        public async Task Enforced_UserNotification_GoesToTheTokenUser_NotToAQueryStringImpostor()
        {
            StartHub(enforce: true);
            var realUser = Connect(SubscriberKey, UserToken("user-1"));
            var realReceived = ListenForNotification(realUser);
            await realUser.StartAsync();

            // Connects later with the victim's id in the address, which used to take over the mapping.
            var impostor = Connect(SubscriberKey, UserToken("user-2"), queryUserId: "user-1");
            var impostorReceived = ListenForNotification(impostor);
            await impostor.StartAsync();

            var publisher = Connect(PublisherKey);
            await publisher.StartAsync();
            await publisher.InvokeAsync("PublishNotification", Notification("user-1", "for user-1"));

            Assert.True(await ArrivesWithin(realReceived.Task, Wait));
            Assert.Equal("for user-1", realReceived.Task.Result.Details);
            Assert.False(await ArrivesWithin(impostorReceived.Task, Quiet));
        }

        [Fact]
        public async Task UserNotification_ForUserWhoIsNotConnected_IsNotSentToEveryone()
        {
            StartHub(enforce: true);
            var otherUser = Connect(SubscriberKey, UserToken("user-1"));
            var received = ListenForNotification(otherUser);
            await otherUser.StartAsync();

            var publisher = Connect(PublisherKey);
            await publisher.StartAsync();
            await publisher.InvokeAsync("PublishNotification", Notification("offline-user", "private to offline-user"));

            Assert.False(await ArrivesWithin(received.Task, Quiet));
        }

        [Fact]
        public async Task LogOnly_ClientsWithoutCredentials_KeepWorking()
        {
            // Rollout mode: an old Web pod (no key, user id in the query) and an old WebSocket.Service
            // (no key) must keep working until Security:Enforce is turned on.
            StartHub(enforce: false);
            var oldWeb = Connect(queryUserId: "user-1");
            var received = ListenForNotification(oldWeb);
            await oldWeb.StartAsync();

            var oldPublisher = Connect();
            await oldPublisher.StartAsync();
            await oldPublisher.InvokeAsync("PublishNotification", Notification("user-1", "still delivered"));

            Assert.True(await ArrivesWithin(received.Task, Wait));
        }

        [Fact]
        public async Task HealthCheck_StaysAnonymous()
        {
            StartHub(enforce: true);

            var response = await _factory.CreateClient().GetAsync("/healthz");

            Assert.True(response.IsSuccessStatusCode);
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
                connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _factory?.Dispose();
        }
    }
}
