using System.Collections.Concurrent;
using Backoffice.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class MetabaseIdleUserCleanupServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

        private sealed class ListLogger : ILogger<MetabaseIdleUserCleanupService>
        {
            private readonly ConcurrentQueue<LogEntry> _entries = new();

            public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                _entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTimeOffset now) => _now = now;

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private static MetabaseIdleCleanupOptions BuildOptions(
            bool enabled = true,
            double idleHours = 3,
            double intervalMinutes = 15,
            double startupDelaySeconds = 0,
            string groupName = "Clients") =>
            new()
            {
                Enabled = enabled,
                IdleHours = idleHours,
                IntervalMinutes = intervalMinutes,
                StartupDelaySeconds = startupDelaySeconds,
                GroupName = groupName
            };

        // The client is strict: any call a test did not set up fails it, which is how "makes no Metabase calls" is proved.
        private static (MetabaseIdleUserCleanupService sut, Mock<IMetabaseAdminClient> client, ListLogger logger) BuildSut(
            Action<Mock<IMetabaseAdminClient>>? setup = null,
            MetabaseIdleCleanupOptions? options = null,
            IOptions<MetabaseIdleCleanupOptions>? optionsAccessor = null)
        {
            var client = new Mock<IMetabaseAdminClient>(MockBehavior.Strict);
            setup?.Invoke(client);

            var logger = new ListLogger();
            var sut = new MetabaseIdleUserCleanupService(
                client.Object, optionsAccessor ?? Options.Create(options ?? BuildOptions()), new FixedTimeProvider(Now), logger);

            return (sut, client, logger);
        }

        // The host starts ExecuteAsync on its own, so a job that ends by itself has to be waited for before stopping.
        private static async Task RunToCompletionAsync(MetabaseIdleUserCleanupService sut)
        {
            await sut.StartAsync(CancellationToken.None);
            await sut.ExecuteTask!.WaitAsync(Wait);
            await sut.StopAsync(CancellationToken.None);
        }

        private static MetabaseUser Member(int id, TimeSpan idle, bool superuser = false) =>
            new(id, $"u{id}@clients.song88.com", Now - idle, superuser);

        private static IReadOnlyList<MetabaseUser> Members(params MetabaseUser[] members) => members;

        private static void SetupGroup(Mock<IMetabaseAdminClient> client, IReadOnlyList<MetabaseUser> members)
        {
            client.Setup(c => c.FindGroupIdAsync("Clients", It.IsAny<CancellationToken>())).ReturnsAsync(7);
            client.Setup(c => c.ListActiveGroupMembersAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(members);
            client.Setup(c => c.DeactivateUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Disabled_LogsOnceAndMakesNoMetabaseCalls()
        {
            var (sut, client, logger) = BuildSut(options: BuildOptions(enabled: false));

            await RunToCompletionAsync(sut);

            client.VerifyNoOtherCalls();
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("disabled", entry.Message);
        }

        [Fact]
        public async Task EnabledButNotConfigured_LogsAnErrorAndMakesNoMetabaseCalls()
        {
            var (sut, client, logger) = BuildSut(c => c.Setup(x => x.IsConfigured).Returns(false));

            await RunToCompletionAsync(sut);

            client.Verify(c => c.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains("Metabase:ApiKey", entry.Message);
        }

        [Theory]
        [InlineData(0, 15, 0, "Clients")]
        [InlineData(-1, 15, 0, "Clients")]
        [InlineData(double.NaN, 15, 0, "Clients")]
        [InlineData(3, 0, 0, "Clients")]
        [InlineData(3, 15, -5, "Clients")]
        [InlineData(3, 15, 0, "")]
        [InlineData(1e300, 15, 0, "Clients")]
        [InlineData(3, 1e300, 0, "Clients")]
        [InlineData(3, 1e-9, 0, "Clients")]
        [InlineData(3, 15, 1e300, "Clients")]
        public async Task EnabledWithInvalidSettings_LogsAnErrorAndMakesNoMetabaseCalls(
            double idleHours, double intervalMinutes, double startupDelaySeconds, string groupName)
        {
            var (sut, client, logger) = BuildSut(
                c => c.Setup(x => x.IsConfigured).Returns(true),
                BuildOptions(idleHours: idleHours, intervalMinutes: intervalMinutes, startupDelaySeconds: startupDelaySeconds, groupName: groupName));

            await RunToCompletionAsync(sut);

            client.Verify(c => c.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
        }

        [Fact]
        public async Task SettingsThatCannotBeRead_AreLoggedAndDoNotStopTheHost()
        {
            // A value that cannot be bound (for example IdleHours=three) makes IOptions.Value throw. An exception that
            // escaped ExecuteAsync would stop the whole web host, so it has to end up in the log instead.
            var failure = new InvalidOperationException("Failed to convert configuration value");
            var accessor = new Mock<IOptions<MetabaseIdleCleanupOptions>>();
            accessor.Setup(o => o.Value).Throws(failure);
            var (sut, client, logger) = BuildSut(optionsAccessor: accessor.Object);

            await RunToCompletionAsync(sut);

            client.VerifyNoOtherCalls();
            var error = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, error.Level);
            Assert.Same(failure, error.Exception);
        }

        [Fact]
        public async Task RunRoundAsync_DeactivatesOnlyMembersIdleLongerThanTheCutoff()
        {
            var (sut, client, _) = BuildSut(c => SetupGroup(c, Members(
                Member(1, TimeSpan.FromHours(4)),
                Member(2, TimeSpan.FromHours(2)),
                Member(3, TimeSpan.FromHours(3)),
                Member(4, TimeSpan.FromHours(3) + TimeSpan.FromMinutes(1)))));

            var round = await sut.RunRoundAsync(CancellationToken.None);

            client.Verify(c => c.DeactivateUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(c => c.DeactivateUserAsync(4, It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(c => c.DeactivateUserAsync(2, It.IsAny<CancellationToken>()), Times.Never);
            client.Verify(c => c.DeactivateUserAsync(3, It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal(new MetabaseIdleCleanupRound(true, 4, 2, 2, 0, 0, 0), round);
        }

        [Fact]
        public async Task RunRoundAsync_NeverDeactivatesSuperusersOrAccountsWithoutALastLogin()
        {
            var (sut, client, _) = BuildSut(c => SetupGroup(c, Members(
                Member(1, TimeSpan.FromDays(30), superuser: true),
                new MetabaseUser(2, "u2@clients.song88.com", null, false),
                Member(3, TimeSpan.FromDays(30)))));

            var round = await sut.RunRoundAsync(CancellationToken.None);

            client.Verify(c => c.DeactivateUserAsync(3, It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(c => c.DeactivateUserAsync(1, It.IsAny<CancellationToken>()), Times.Never);
            client.Verify(c => c.DeactivateUserAsync(2, It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal(new MetabaseIdleCleanupRound(true, 3, 1, 0, 1, 1, 0), round);
        }

        [Fact]
        public async Task RunRoundAsync_OnlyAsksAboutTheConfiguredGroup()
        {
            var (sut, client, _) = BuildSut(
                c =>
                {
                    c.Setup(x => x.FindGroupIdAsync("Staff", It.IsAny<CancellationToken>())).ReturnsAsync(9);
                    c.Setup(x => x.ListActiveGroupMembersAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(Members());
                },
                BuildOptions(groupName: "Staff"));

            await sut.RunRoundAsync(CancellationToken.None);

            client.Verify(c => c.FindGroupIdAsync("Staff", It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(c => c.ListActiveGroupMembersAsync(9, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunRoundAsync_GroupNotFound_ChangesNothingAndWarns()
        {
            var (sut, client, logger) = BuildSut(c =>
                c.Setup(x => x.FindGroupIdAsync("Clients", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null));

            var round = await sut.RunRoundAsync(CancellationToken.None);

            Assert.False(round.GroupFound);
            client.Verify(c => c.ListActiveGroupMembersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            client.Verify(c => c.DeactivateUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Contains("Clients", entry.Message);
        }

        [Fact]
        public async Task RunRoundAsync_OneDeactivationFailing_DoesNotStopTheOthers()
        {
            var failure = new HttpRequestException("Metabase DELETE api/user/1 failed with status 500.");
            var (sut, client, logger) = BuildSut(c =>
            {
                SetupGroup(c, Members(Member(1, TimeSpan.FromHours(5)), Member(2, TimeSpan.FromHours(5))));
                c.Setup(x => x.DeactivateUserAsync(1, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
            });

            var round = await sut.RunRoundAsync(CancellationToken.None);

            client.Verify(c => c.DeactivateUserAsync(2, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(1, round.Deactivated);
            Assert.Equal(1, round.Failed);
            var error = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
            Assert.Same(failure, error.Exception);
        }

        [Fact]
        public async Task RunRoundAsync_LogsEachDeactivationAndOneSummary()
        {
            var (sut, _, logger) = BuildSut(c => SetupGroup(c, Members(
                Member(1, TimeSpan.FromHours(5)),
                Member(2, TimeSpan.FromHours(1)),
                Member(3, TimeSpan.FromHours(6)),
                Member(4, TimeSpan.FromDays(9), superuser: true))));

            await sut.RunRoundAsync(CancellationToken.None);

            Assert.Equal(2, logger.Entries.Count(e => e.Message.Contains("deactivated user")));
            Assert.Contains(logger.Entries, e => e.Message.Contains("deactivated user 1 u1@clients.song88.com"));
            var summary = Assert.Single(logger.Entries, e => e.Message.Contains("round finished"));
            Assert.Contains("Group Clients, Members 4, Deactivated 2, KeptActive 1, SkippedSuperuser 1, SkippedNoLogin 0, Failed 0", summary.Message);
        }

        [Fact]
        public async Task RunRoundGuardedAsync_AFailingRound_IsLoggedAndNotThrown()
        {
            var failure = new InvalidOperationException("Metabase is down");
            var (sut, _, logger) = BuildSut(c =>
                c.Setup(x => x.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure));

            await sut.RunRoundGuardedAsync(CancellationToken.None);

            var error = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, error.Level);
            Assert.Same(failure, error.Exception);
            Assert.Contains("retried in 15 minutes", error.Message);
        }

        [Fact]
        public async Task RunRoundGuardedAsync_ARequestTimeout_IsLoggedAndNotThrown()
        {
            // HttpClient reports its own timeout as a TaskCanceledException while the host is not stopping.
            var (sut, _, logger) = BuildSut(c =>
                c.Setup(x => x.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TaskCanceledException()));

            await sut.RunRoundGuardedAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
        }

        [Fact]
        public async Task RunRoundGuardedAsync_WhenTheHostIsStopping_LetsTheCancellationThrough()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var (sut, _, _) = BuildSut(c =>
                c.Setup(x => x.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException(cts.Token)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunRoundGuardedAsync(cts.Token));
        }

        [Fact]
        public async Task Enabled_KeepsRunningRoundsAfterOneFails()
        {
            var calls = 0;
            var secondRound = new TaskCompletionSource();
            var (sut, _, logger) = BuildSut(
                c =>
                {
                    c.Setup(x => x.IsConfigured).Returns(true);
                    c.Setup(x => x.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(() =>
                    {
                        if (Interlocked.Increment(ref calls) == 1)
                            throw new InvalidOperationException("Metabase is down");

                        secondRound.TrySetResult();
                        return Task.FromResult<int?>(null);
                    });
                },
                BuildOptions(intervalMinutes: 0.001));

            await sut.StartAsync(CancellationToken.None);
            await secondRound.Task.WaitAsync(Wait);
            await sut.StopAsync(CancellationToken.None);

            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("round failed"));
        }

        [Fact]
        public async Task Stopping_InTheMiddleOfARound_EndsTheJobWithoutAnError()
        {
            var roundStarted = new TaskCompletionSource();
            var (sut, _, logger) = BuildSut(c =>
            {
                c.Setup(x => x.IsConfigured).Returns(true);
                c.Setup(x => x.FindGroupIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(async (string _, CancellationToken token) =>
                {
                    roundStarted.TrySetResult();
                    await Task.Delay(Timeout.Infinite, token);
                    return (int?)null;
                });
            });

            await sut.StartAsync(CancellationToken.None);
            await roundStarted.Task.WaitAsync(Wait);
            await sut.StopAsync(CancellationToken.None).WaitAsync(Wait);

            Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
            Assert.Contains(logger.Entries, e => e.Message.Contains("stopped"));
        }
    }
}
