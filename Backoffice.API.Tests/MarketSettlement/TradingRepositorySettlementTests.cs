using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.MarketSettlement
{
    public class TradingRepositorySettlementTests
    {
        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static IConfiguration BuildConfig()
        {
            // MongoClient is lazy — passing a dummy URI keeps the constructor from throwing
            // while ensuring it never actually dials out during the test.
            var mock = new Mock<IConfiguration>();
            mock.Setup(c => c["Custom:MongoDB-Sportsbook"]).Returns("mongodb://localhost:27017");
            mock.Setup(c => c[It.Is<string>(k => k != "Custom:MongoDB-Sportsbook")]).Returns((string)null);
            return mock.Object;
        }

        private static ManualMarketSettlementRequest MakeRequest(
            string providerFixtureId,
            string fixtureId,
            string marketId,
            string selectionId,
            BetStatus status,
            string marketTemplateId = "mt1",
            string selectionTemplateId = "st1",
            string marketSpecifier = "")
        {
            return new ManualMarketSettlementRequest
            {
                ProviderFixtureId = providerFixtureId,
                FixtureId = fixtureId,
                MarketId = marketId,
                SelectionId = selectionId,
                SelectionStatus = status,
                MarketTemplateId = marketTemplateId,
                SelectionTemplateId = selectionTemplateId,
                MarketSpecifier = marketSpecifier,
                FixtureSpecifier = "",
                Reason = "test settlement",
                SportId = 10,
                SportName = "Football",
                SegmentId = 1,
                SegmentName = "Europe",
                LeagueId = 1,
                LeagueName = "Test League",
                FixtureName = "Team A vs Team B",
                MarketName = "Match Winner",
                SelectionName = "Team A",
                ManualSettledBy = "test-user",
                DeadHeatFactor = 1m,
            };
        }

        private static TradingRepository BuildSut(IRabbitMqRepository rabbitMq, IBackofficeActivityLogRepository activityLog = null)
        {
            return new TradingRepository(
                BuildConfig(),
                activityLog ?? new Mock<IBackofficeActivityLogRepository>().Object,
                new Mock<IMemCache>().Object,
                NullLogger<TradingRepository>.Instance,
                rabbitMq);
        }

        // ---------------------------------------------------------------
        // Tests
        // ---------------------------------------------------------------

        [Fact]
        public async Task PublishesOneOnBetSettlementPerProviderFixture()
        {
            // Arrange: 2 selections for fixture "pf1", 1 selection for "pf2"
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won),
                MakeRequest("pf1", "1", "102", "1002", BetStatus.Lost),
                MakeRequest("pf2", "2", "201", "2001", BetStatus.Void),
            };

            var published = new List<MessageRequest>();
            var rabbitMock = new Mock<IRabbitMqRepository>();
            rabbitMock
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((req, _) => published.Add(req))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            var sut = BuildSut(rabbitMock.Object);

            // Act
            var result = await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            // Assert: call succeeded
            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(StatusCode.Success, result.Result.StatusCode);

            // The activity log is now written directly (not published), so the broker only
            // sees the two OnBetSettlement messages — one per distinct provider fixture.
            Assert.Equal(2, published.Count);
            Assert.All(published, p => Assert.Equal("OnBetSettlement", p.MessageType));
            Assert.All(published, p => Assert.Equal("r.uof.onbetsettlement", p.RoutingKey));

            var pf1 = published
                .Select(s => JsonSerializer.Deserialize<OnBetSettlementMessage>(s.Payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Single(m => m.ProviderFixtureId == "pf1");

            var pf2 = published
                .Select(s => JsonSerializer.Deserialize<OnBetSettlementMessage>(s.Payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Single(m => m.ProviderFixtureId == "pf2");

            Assert.Equal(2, pf1.Selections.Count);
            Assert.Single(pf2.Selections);
            Assert.Equal(10, pf1.SportId);
        }

        [Fact]
        public async Task MapsSelectionResultCorrectly()
        {
            // Won/HalfWon → Result=true; others → false
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won),
                MakeRequest("pf1", "1", "102", "1002", BetStatus.HalfWon),
                MakeRequest("pf1", "1", "103", "1003", BetStatus.Lost),
                MakeRequest("pf1", "1", "104", "1004", BetStatus.HalfLost),
                MakeRequest("pf1", "1", "105", "1005", BetStatus.Void),
            };

            var published = new List<MessageRequest>();
            var rabbitMock = new Mock<IRabbitMqRepository>();
            rabbitMock
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((req, _) => published.Add(req))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            var sut = BuildSut(rabbitMock.Object);

            await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            var payload = published.Single(p => p.MessageType == "OnBetSettlement").Payload;
            var msg = JsonSerializer.Deserialize<OnBetSettlementMessage>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(5, msg.Selections.Count);
            Assert.Equal(2, msg.Selections.Count(s => s.Result));    // Won + HalfWon
            Assert.Equal(3, msg.Selections.Count(s => !s.Result));   // Lost + HalfLost + Void
        }

        [Fact]
        public async Task MapsVoidFactorCorrectly()
        {
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won,      "mt_won",      "st_won"),
                MakeRequest("pf1", "1", "102", "1002", BetStatus.HalfWon,  "mt_halfwon",  "st_halfwon"),
                MakeRequest("pf1", "1", "103", "1003", BetStatus.Lost,     "mt_lost",     "st_lost"),
                MakeRequest("pf1", "1", "104", "1004", BetStatus.HalfLost, "mt_halflost", "st_halflost"),
                MakeRequest("pf1", "1", "105", "1005", BetStatus.Void,     "mt_void",     "st_void"),
            };

            var published = new List<MessageRequest>();
            var rabbitMock = new Mock<IRabbitMqRepository>();
            rabbitMock
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((req, _) => published.Add(req))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            var sut = BuildSut(rabbitMock.Object);

            await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            var payload = published.Single(p => p.MessageType == "OnBetSettlement").Payload;
            var msg = JsonSerializer.Deserialize<OnBetSettlementMessage>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(VoidFactor.Zero, msg.Selections.Single(s => s.SelectionTemplateId == "st_won").VoidFactor);
            Assert.Equal(VoidFactor.Half, msg.Selections.Single(s => s.SelectionTemplateId == "st_halfwon").VoidFactor);
            Assert.Equal(VoidFactor.Zero, msg.Selections.Single(s => s.SelectionTemplateId == "st_lost").VoidFactor);
            Assert.Equal(VoidFactor.Half, msg.Selections.Single(s => s.SelectionTemplateId == "st_halflost").VoidFactor);
            Assert.Equal(VoidFactor.One,  msg.Selections.Single(s => s.SelectionTemplateId == "st_void").VoidFactor);
        }

        [Fact]
        public async Task ReturnsErrorAndPublishesNothingForEmptyRequest()
        {
            var rabbitMock = new Mock<IRabbitMqRepository>();
            var activityMock = new Mock<IBackofficeActivityLogRepository>();
            var sut = BuildSut(rabbitMock.Object, activityMock.Object);

            var result = await sut.AddOrUpdateManualMarketSettlement(
                new List<ManualMarketSettlementRequest>(), "tester", "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);       // outer envelope is always Success
            Assert.Equal(StatusCode.Error, result.Result.StatusCode);  // inner status signals the validation failure
            rabbitMock.Verify(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()), Times.Never);
            activityMock.Verify(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReturnsErrorAndPublishesNothingForDuplicateSelection()
        {
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won),
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Lost),  // duplicate fixture+market+selection
            };

            var rabbitMock = new Mock<IRabbitMqRepository>();
            var activityMock = new Mock<IBackofficeActivityLogRepository>();
            var sut = BuildSut(rabbitMock.Object, activityMock.Object);

            var result = await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(StatusCode.Error, result.Result.StatusCode);
            rabbitMock.Verify(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()), Times.Never);
            activityMock.Verify(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ActivityLogWrittenViaRepositoryWithPageMatchingHistoryRead()
        {
            // The history list and Excel export read activity logs keyed by the page's
            // AdminPermission name. The settlement write must use the SAME key, otherwise
            // the row is invisible to the list. Verify the repository is invoked with it.
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won),
                MakeRequest("pf2", "2", "201", "2001", BetStatus.Lost),
            };

            var rabbitMock = new Mock<IRabbitMqRepository>();
            rabbitMock
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            OnActivityLogListMessage captured = null;
            var activityMock = new Mock<IBackofficeActivityLogRepository>();
            activityMock
                .Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()))
                .Callback<OnActivityLogListMessage, string>((m, _) => captured = m)
                .Returns(Task.CompletedTask);

            var sut = BuildSut(rabbitMock.Object, activityMock.Object);

            await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            var expectedPage = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", ".");

            // Written once, directly through the repository (not via the broker).
            activityMock.Verify(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()), Times.Once);
            Assert.NotNull(captured);
            Assert.Equal(2, captured.OnActivityLogMessages.Count);
            Assert.All(captured.OnActivityLogMessages, m => Assert.Equal(expectedPage, m.Page));
            Assert.All(captured.OnActivityLogMessages, m => Assert.Equal("tester", m.UserId));
            Assert.All(captured.OnActivityLogMessages, m => Assert.Equal(Activity.ManualSettlement.ToString(), m.Activity));

            // The full detail blob is stored in Payload (nvarchar(max)), NOT To (nvarchar(255)).
            Assert.All(captured.OnActivityLogMessages, m => Assert.Contains("Match Winner", m.Payload));
            Assert.All(captured.OnActivityLogMessages, m => Assert.Contains("\"SportId\":10", m.Payload));
            // To carries the resulting selection status (a short "main" value).
            Assert.Contains(captured.OnActivityLogMessages, m => m.To == BetStatus.Won.ToString());
            Assert.Contains(captured.OnActivityLogMessages, m => m.To == BetStatus.Lost.ToString());

            // No activity-log message should be published to the broker anymore.
            rabbitMock.Verify(r => r.Publish(It.Is<MessageRequest>(m => m.MessageType == "LogActivityList"), It.IsAny<string>()), Times.Never);
        }
    }
}
