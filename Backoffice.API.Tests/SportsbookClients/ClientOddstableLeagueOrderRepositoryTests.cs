using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.SportsbookClients
{
    public class ClientOddstableLeagueOrderRepositoryTests
    {
        private const string OddstableRoutingKey = "r.oe.cachesync.onoddstableleaguesortorderchange";
        private const int Football = 1;
        private const int Basketball = 2;

        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static SportsbookContext BuildContext()
        {
            var opts = new DbContextOptionsBuilder<SportsbookContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SportsbookContext(opts);
        }

        private static SportsbookRepository BuildSut(
            SportsbookContext context,
            List<MessageRequest> published,
            List<OnActivityLogMessage> auditLogs = null,
            Exception auditFailure = null,
            IBackofficeActivityLogRepository activityLogRepository = null)
        {
            var platform = new Mock<IPlatformRepository>();
            platform
                .Setup(p => p.GetClients(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = ClientAId, ClientCode = "QTE-A", ClientName = "Client A" },
                        new PlatformClientResponse { ClientId = ClientBId, ClientCode = "QTE-B", ClientName = "Client B" },
                        new PlatformClientResponse { ClientId = ClientCId, ClientCode = "QTE-C", ClientName = "Client C" },
                    }
                });

            var rabbitMq = new Mock<IRabbitMqRepository>();
            rabbitMq
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((request, _) => published.Add(request));

            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            var logSetup = activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()));

            if (auditFailure != null)
                logSetup.ThrowsAsync(auditFailure);
            else
                logSetup
                    .Callback<OnActivityLogListMessage, string>((message, _) => auditLogs?.AddRange(message.OnActivityLogMessages))
                    .Returns(Task.CompletedTask);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Custom:MongoDB-Sportsbook"] = "mongodb://localhost:27017"
                })
                .Build();

            return new SportsbookRepository(
                context,
                configuration,
                platform.Object,
                activityLogRepository ?? activityLog.Object,
                rabbitMq.Object,
                new Mock<IMemCache>().Object);
        }

        private static ClientOddstableLeagueOrder Row(Guid clientId, int sportId, int segmentId, int leagueId, int sortOrder, string leagueName = null) =>
            new ClientOddstableLeagueOrder
            {
                ClientId = clientId,
                SportId = sportId,
                SportName = sportId == Football ? "Football" : "Basketball",
                SegmentId = segmentId,
                SegmentName = $"Segment {segmentId}",
                LeagueId = leagueId,
                LeagueName = leagueName ?? $"League {leagueId}",
                SortOrder = sortOrder
            };

        private static OddstableLeagueSortOrder Item(int segmentId, int leagueId, int sortOrder, int sportId = Football, string leagueName = null) =>
            new OddstableLeagueSortOrder
            {
                SportId = sportId,
                SportName = sportId == Football ? "Football" : "Basketball",
                SegmentId = segmentId,
                SegmentName = $"Segment {segmentId}",
                LeagueId = leagueId,
                LeagueName = leagueName ?? $"League {leagueId}",
                SortOrder = sortOrder
            };

        private static ClientOddstableLeagueSettingRequest SaveRequest(string clientCode, int sportId, params OddstableLeagueSortOrder[] leagues) =>
            new ClientOddstableLeagueSettingRequest
            {
                ClientCode = clientCode,
                SportId = sportId,
                Leagues = leagues.ToList(),
                UserId = "tester",
                UserName = "Test User",
                TimeStamp = DateTime.UtcNow
            };

        private static CopyClientOddstableLeagueSettingRequest CopyRequest(string source, int sportId, params string[] targets) =>
            new CopyClientOddstableLeagueSettingRequest
            {
                SourceClientCode = source,
                TargetClientCodes = targets.ToList(),
                SportId = sportId,
                UserId = "tester",
                UserName = "Test User",
                TimeStamp = DateTime.UtcNow
            };

        private static async Task<List<(int SegmentId, int LeagueId, int SortOrder)>> Order(SportsbookContext context, Guid clientId, int sportId) =>
            (await context.ClientOddstableLeagueOrder.AsNoTracking()
                .Where(c => c.ClientId == clientId && c.SportId == sportId)
                .OrderBy(c => c.SortOrder)
                .ToListAsync())
            .Select(c => (c.SegmentId, c.LeagueId, c.SortOrder))
            .ToList();

        private static OddstableLeagueSortingAudit AuditOf(string json) =>
            JsonSerializer.Deserialize<OddstableLeagueSortingAudit>(json);

        private static void AssertRejected(BaseResponse<ClientOddstableLeagueSettingResponse> response, string message)
        {
            Assert.Equal(StatusCode.Error, response.Result.StatusCode);
            Assert.Equal(message, response.Result.Message);
        }

        // ---------------------------------------------------------------
        // GetClientOddstableLeagueOrderList
        // ---------------------------------------------------------------

        [Fact]
        public async Task GetList_FiltersByClientAndSport_OrderedBySortOrder()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 2),
                Row(ClientAId, Football, 10, 101, 1),
                Row(ClientAId, Basketball, 20, 200, 1),
                Row(ClientBId, Football, 10, 100, 1));
            await context.SaveChangesAsync();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.GetClientOddstableLeagueOrderList(new ClientOddstableLeagueOrderListRequest { ClientCode = "QTE-A", SportId = Football });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { 101, 100 }, response.Result.Select(r => r.LeagueId));
            Assert.All(response.Result, r =>
            {
                Assert.Equal("QTE-A", r.ClientCode);
                Assert.Equal("Client A", r.ClientName);
                Assert.Equal(Football, r.SportId);
            });
        }

        [Fact]
        public async Task GetList_WithoutClientCode_ReturnsEveryClientsRows()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientBId, Football, 10, 100, 1));
            await context.SaveChangesAsync();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.GetClientOddstableLeagueOrderList(new ClientOddstableLeagueOrderListRequest());

            Assert.Equal(new[] { "QTE-A", "QTE-B" }, response.Result.Select(r => r.ClientCode));
        }

        [Fact]
        public async Task GetList_UnknownClientCode_ReturnsEmptyInsteadOfEveryone()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.Add(Row(ClientAId, Football, 10, 100, 1));
            await context.SaveChangesAsync();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.GetClientOddstableLeagueOrderList(new ClientOddstableLeagueOrderListRequest { ClientCode = "NOPE" });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Empty(response.Result);
        }

        // ---------------------------------------------------------------
        // AddOrUpdateClientOddstableLeagueSettings
        // ---------------------------------------------------------------

        [Fact]
        public async Task Save_PersistsLeagues_AndPublishesOddstableCacheMessage()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(
                SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(11, 101, 2)));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(StatusCode.Success, response.Result.StatusCode);
            Assert.Equal(new[] { (10, 100, 1), (11, 101, 2) }, await Order(context, ClientAId, Football));

            var message = Assert.Single(published);
            Assert.Equal(OddstableRoutingKey, message.RoutingKey);
            Assert.Equal("OnOddstableLeagueSortOrderChange", message.MessageType);
            var payload = JsonSerializer.Deserialize<OnDeleteCacheMessage>(message.Payload);
            Assert.Equal(CacheType.OddstableLeagueSortOrder, payload.CacheType);
            Assert.Equal("QTE-A", payload.ClientCode);
            Assert.Equal(ClientAId, payload.ClientId);
        }

        [Fact]
        public async Task Save_ReplacesExistingOrderForThatClientAndSportOnly()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2),
                Row(ClientAId, Football, 10, 102, 3),
                Row(ClientAId, Basketball, 20, 200, 1),
                Row(ClientBId, Football, 10, 100, 1));
            await context.SaveChangesAsync();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(
                SaveRequest("QTE-A", Football, Item(10, 101, 1, leagueName: "Renamed"), Item(10, 103, 2)));

            Assert.Equal(StatusCode.Success, response.Result.StatusCode);
            Assert.Equal(new[] { (10, 101, 1), (10, 103, 2) }, await Order(context, ClientAId, Football));
            Assert.Equal("Renamed", (await context.ClientOddstableLeagueOrder.AsNoTracking().SingleAsync(c => c.ClientId == ClientAId && c.LeagueId == 101)).LeagueName);
            Assert.Equal(new[] { (20, 200, 1) }, await Order(context, ClientAId, Basketball));
            Assert.Equal(new[] { (10, 100, 1) }, await Order(context, ClientBId, Football));
        }

        [Fact]
        public async Task Save_DoesNotTouchTheSportTreeLeagueOrder()
        {
            using var context = BuildContext();
            context.ClientLeagueOrder.Add(new ClientLeagueOrder
            {
                ClientId = ClientAId,
                SportId = Football,
                SportName = "Football",
                SegmentId = 10,
                SegmentName = "Segment 10",
                LeagueId = 100,
                LeagueName = "League 100",
                SortOrder = 5
            });
            await context.SaveChangesAsync();
            var sut = BuildSut(context, new List<MessageRequest>());

            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 101, 1)));

            var sportTree = await context.ClientLeagueOrder.AsNoTracking().SingleAsync();
            Assert.Equal((100, 5), (sportTree.LeagueId, sportTree.SortOrder));
        }

        [Fact]
        public async Task Save_Rejected_WhenClientCodeMissing()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("", Football, Item(10, 100, 1))), "Invalid Client Code");
            Assert.Empty(published);
        }

        [Fact]
        public async Task Save_Rejected_WhenSportMissing()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, new List<MessageRequest>());

            AssertRejected(await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", 0, Item(10, 100, 1, sportId: 0))), "Invalid Sport");
        }

        [Fact]
        public async Task Save_Rejected_WhenNoLeagues()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football)), "No Client League Order to be updated");
            Assert.Empty(published);
        }

        [Theory]
        [InlineData(0, 100, 1)]
        [InlineData(10, 0, 1)]
        [InlineData(10, 100, 0)]
        public async Task Save_Rejected_WhenARowHasAnEmptyField(int segmentId, int leagueId, int sortOrder)
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.Add(Row(ClientAId, Football, 10, 100, 1));
            await context.SaveChangesAsync();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(
                SaveRequest("QTE-A", Football, Item(segmentId, leagueId, sortOrder)));

            AssertRejected(response, "Error! All fields cannot be empty. Please fill in accordingly or remove the empty row");
            Assert.Equal(new[] { (10, 100, 1) }, await Order(context, ClientAId, Football));
            Assert.Empty(published);
        }

        [Fact]
        public async Task Save_Rejected_WhenARowBelongsToAnotherSport()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(
                SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(20, 200, 2, sportId: Basketball)));

            Assert.Equal(StatusCode.Error, response.Result.StatusCode);
            Assert.Empty(context.ClientOddstableLeagueOrder);
        }

        [Fact]
        public async Task Save_Rejected_WhenALeagueIsListedTwice()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, new List<MessageRequest>());

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(
                SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(10, 100, 2)));

            AssertRejected(response, "Duplicate client league found");
            Assert.Empty(context.ClientOddstableLeagueOrder);
        }

        [Fact]
        public async Task Save_Rejected_WhenClientUnknown()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("NOPE", Football, Item(10, 100, 1))), "No client found");
            Assert.Empty(published);
        }

        // ---------------------------------------------------------------
        // Audit log (ActivityLog2, Page = SBC.OE.LeagueSorting)
        // ---------------------------------------------------------------

        [Fact]
        public async Task Save_FirstOrder_WritesAnAddLog_WithUserSportAndNewOrder()
        {
            using var context = BuildContext();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(11, 101, 2)));

            var log = Assert.Single(logs);
            Assert.Equal("SBC.OE.LeagueSorting", log.Page);
            Assert.Equal("QTE-A", log.Payload);
            Assert.Equal("Add", log.Activity);
            Assert.Equal("tester", log.UserId);
            Assert.Equal("Test User", log.UserName);
            Assert.NotEqual(default, log.TimeStamp);

            var from = AuditOf(log.From);
            var to = AuditOf(log.To);
            Assert.Empty(from.Leagues);
            Assert.Equal((Football, "Football"), (to.SportId, to.SportName));
            Assert.Equal(new[] { 100, 101 }, to.Leagues.Select(l => l.LeagueId));
            Assert.Null(to.SourceClientCode);
        }

        [Fact]
        public async Task Save_SameLeaguesInAnotherOrder_WritesAReorderLog_WithBothOrders()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2));
            await context.SaveChangesAsync();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 101, 1), Item(10, 100, 2)));

            var log = Assert.Single(logs);
            Assert.Equal("Reorder", log.Activity);
            Assert.Equal(new[] { 100, 101 }, AuditOf(log.From).Leagues.Select(l => l.LeagueId));
            Assert.Equal(new[] { 101, 100 }, AuditOf(log.To).Leagues.Select(l => l.LeagueId));
        }

        [Fact]
        public async Task Save_LeagueAddedOrRemoved_WritesAnEditLog()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2));
            await context.SaveChangesAsync();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(10, 102, 2)));

            var log = Assert.Single(logs);
            Assert.Equal("Edit", log.Activity);
            Assert.Equal(new[] { 100, 101 }, AuditOf(log.From).Leagues.Select(l => l.LeagueId));
            Assert.Equal(new[] { 100, 102 }, AuditOf(log.To).Leagues.Select(l => l.LeagueId));
        }

        [Fact]
        public async Task Save_NothingChanged_WritesNoLog()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2));
            await context.SaveChangesAsync();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(10, 101, 2)));

            Assert.Equal(StatusCode.Success, response.Result.StatusCode);
            Assert.Empty(logs);
        }

        [Fact]
        public async Task Save_Rejected_WritesNoLog()
        {
            using var context = BuildContext();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("NOPE", Football, Item(10, 100, 1)));
            await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 100, 1), Item(10, 100, 2)));

            Assert.Empty(logs);
        }

        [Fact]
        public async Task Save_WhenTheAuditWriteFails_StillSaves_AndWarns()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published, auditFailure: new InvalidOperationException("audit store down"));

            var response = await sut.AddOrUpdateClientOddstableLeagueSettings(SaveRequest("QTE-A", Football, Item(10, 100, 1)));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(StatusCode.Success, response.Result.StatusCode);
            Assert.Contains("audit log could not be written", response.Result.Message);
            Assert.Equal(new[] { (10, 100, 1) }, await Order(context, ClientAId, Football));
            Assert.Single(published);
        }

        [Fact]
        public async Task Copy_WritesACopyLogPerTarget_WithSourcePreviousAndNewOrder()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2),
                Row(ClientBId, Football, 10, 109, 1));
            await context.SaveChangesAsync();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-B", "QTE-C"));

            Assert.Equal(2, logs.Count);
            Assert.All(logs, l =>
            {
                Assert.Equal("SBC.OE.LeagueSorting", l.Page);
                Assert.Equal("Copy", l.Activity);
                Assert.Equal("tester", l.UserId);
                Assert.Equal("QTE-A", AuditOf(l.To).SourceClientCode);
                Assert.Equal(new[] { 100, 101 }, AuditOf(l.To).Leagues.Select(x => x.LeagueId));
            });

            var forB = logs.Single(l => l.Payload == "QTE-B");
            var forC = logs.Single(l => l.Payload == "QTE-C");
            Assert.Equal(new[] { 109 }, AuditOf(forB.From).Leagues.Select(l => l.LeagueId));
            Assert.Empty(AuditOf(forC.From).Leagues);
            Assert.Null(AuditOf(forB.From).SourceClientCode);
        }

        [Fact]
        public async Task GetLogs_ReadsTheLeagueSortingPage_ForTheRequestedClientAndPaging()
        {
            using var context = BuildContext();
            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            var expected = new BaseResponse<ActivityLogResponse> { StatusCode = StatusCode.Success, Result = new ActivityLogResponse() };
            var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var metaData = new MetaData { CurrentPage = 2, PageSize = 10 };
            activityLog
                .Setup(a => a.GetPagedActivityLogs("SBC.OE.LeagueSorting", "QTE-A", from, metaData))
                .ReturnsAsync(expected);
            var sut = BuildSut(context, new List<MessageRequest>(), activityLogRepository: activityLog.Object);

            var response = await sut.GetOddstableLeagueSortingLogs(new OddstableLeagueSortingLogRequest { ClientCode = "QTE-A", DateFrom = from, MetaData = metaData });

            Assert.Same(expected, response);
        }

        [Fact]
        public async Task GetLogs_WithoutADate_LooksBackThirtyDays()
        {
            using var context = BuildContext();
            DateTime requestedFrom = default;
            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            activityLog
                .Setup(a => a.GetPagedActivityLogs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<MetaData>()))
                .Callback<string, string, DateTime, MetaData>((_, _, dateFrom, _) => requestedFrom = dateFrom)
                .ReturnsAsync(new BaseResponse<ActivityLogResponse> { StatusCode = StatusCode.Success });
            var sut = BuildSut(context, new List<MessageRequest>(), activityLogRepository: activityLog.Object);

            await sut.GetOddstableLeagueSortingLogs(new OddstableLeagueSortingLogRequest());

            Assert.InRange(requestedFrom, DateTime.UtcNow.AddDays(-30).AddMinutes(-1), DateTime.UtcNow.AddDays(-30).AddMinutes(1));
        }

        [Fact]
        public async Task Copy_Rejected_WritesNoLog()
        {
            using var context = BuildContext();
            var logs = new List<OnActivityLogMessage>();
            var sut = BuildSut(context, new List<MessageRequest>(), logs);

            await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-B"));

            Assert.Empty(logs);
        }

        // ---------------------------------------------------------------
        // CopyClientOddstableLeagueSettings
        // ---------------------------------------------------------------

        [Fact]
        public async Task Copy_ReplacesEveryTargetsOrderForThatSport_AndPublishesOnePerTarget()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientAId, Football, 10, 101, 2),
                Row(ClientAId, Basketball, 20, 200, 1),
                Row(ClientBId, Football, 10, 101, 1),
                Row(ClientBId, Football, 10, 109, 2),
                Row(ClientBId, Basketball, 20, 201, 1));
            await context.SaveChangesAsync();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            var response = await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-B", "QTE-C"));

            Assert.Equal(StatusCode.Success, response.Result.StatusCode);
            var expected = new[] { (10, 100, 1), (10, 101, 2) };
            Assert.Equal(expected, await Order(context, ClientAId, Football));
            Assert.Equal(expected, await Order(context, ClientBId, Football));
            Assert.Equal(expected, await Order(context, ClientCId, Football));
            Assert.Equal(new[] { (20, 201, 1) }, await Order(context, ClientBId, Basketball));

            Assert.Equal(2, published.Count);
            Assert.All(published, m => Assert.Equal(OddstableRoutingKey, m.RoutingKey));
            Assert.Equal(
                new[] { "QTE-B", "QTE-C" },
                published.Select(m => JsonSerializer.Deserialize<OnDeleteCacheMessage>(m.Payload).ClientCode).OrderBy(c => c));
        }

        [Fact]
        public async Task Copy_Rejected_WhenSourceIsAlsoATarget()
        {
            using var context = BuildContext();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-A", "QTE-B")), "Source client cannot be a copy target");
            Assert.Empty(published);
        }

        [Fact]
        public async Task Copy_Rejected_WhenNoTargetSelected()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, new List<MessageRequest>());

            AssertRejected(await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football)), "No target client selected");
        }

        [Fact]
        public async Task Copy_Rejected_WhenATargetClientIsUnknown_AndNothingIsChanged()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.AddRange(
                Row(ClientAId, Football, 10, 100, 1),
                Row(ClientBId, Football, 10, 109, 1));
            await context.SaveChangesAsync();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-B", "NOPE")), "No client found");
            Assert.Equal(new[] { (10, 109, 1) }, await Order(context, ClientBId, Football));
            Assert.Empty(published);
        }

        [Fact]
        public async Task Copy_Rejected_WhenSourceHasNothingToCopy_AndTargetKeepsItsOrder()
        {
            using var context = BuildContext();
            context.ClientOddstableLeagueOrder.Add(Row(ClientBId, Football, 10, 109, 1));
            await context.SaveChangesAsync();
            var published = new List<MessageRequest>();
            var sut = BuildSut(context, published);

            AssertRejected(await sut.CopyClientOddstableLeagueSettings(CopyRequest("QTE-A", Football, "QTE-B")), "Source client has no league sorting to copy");
            Assert.Equal(new[] { (10, 109, 1) }, await Order(context, ClientBId, Football));
            Assert.Empty(published);
        }
    }
}
