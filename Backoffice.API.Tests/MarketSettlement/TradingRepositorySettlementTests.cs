using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Trading;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
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

        private static TradingContext BuildInMemoryContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<TradingContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new TradingContext(opts);
        }

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

        private TradingRepository BuildSut(TradingContext ctx, IRabbitMqRepository rabbitMq)
        {
            return new TradingRepository(
                ctx,
                BuildConfig(),
                new Mock<IBackofficeActivityLogRepository>().Object,
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

            var sut = BuildSut(BuildInMemoryContext("test_one_per_fixture"), rabbitMock.Object);

            // Act
            var result = await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            // Assert: call succeeded
            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(StatusCode.Success, result.Result.StatusCode);

            // One activity-log publish + two OnBetSettlement publishes
            Assert.Equal(3, published.Count);

            var settlements = published.Where(p => p.MessageType == "OnBetSettlement").ToList();
            Assert.Equal(2, settlements.Count);
            Assert.All(settlements, s => Assert.Equal("r.uof.onbetsettlement", s.RoutingKey));

            var pf1 = settlements
                .Select(s => JsonSerializer.Deserialize<OnBetSettlementMessage>(s.Payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Single(m => m.ProviderFixtureId == "pf1");

            var pf2 = settlements
                .Select(s => JsonSerializer.Deserialize<OnBetSettlementMessage>(s.Payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Single(m => m.ProviderFixtureId == "pf2");

            Assert.Equal(2, pf1.Selections.Count);
            Assert.Equal(1, pf2.Selections.Count);
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

            var sut = BuildSut(BuildInMemoryContext("test_result_mapping"), rabbitMock.Object);

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

            var sut = BuildSut(BuildInMemoryContext("test_void_factor"), rabbitMock.Object);

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
            var sut = BuildSut(BuildInMemoryContext("test_empty"), rabbitMock.Object);

            var result = await sut.AddOrUpdateManualMarketSettlement(
                new List<ManualMarketSettlementRequest>(), "tester", "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);       // outer envelope is always Success
            Assert.Equal(StatusCode.Error, result.Result.StatusCode);  // inner status signals the validation failure
            rabbitMock.Verify(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()), Times.Never);
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
            var sut = BuildSut(BuildInMemoryContext("test_duplicate"), rabbitMock.Object);

            var result = await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(StatusCode.Error, result.Result.StatusCode);
            rabbitMock.Verify(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ActivityLogPublishedToCorrectRoutingKey()
        {
            var requests = new List<ManualMarketSettlementRequest>
            {
                MakeRequest("pf1", "1", "101", "1001", BetStatus.Won),
            };

            var published = new List<MessageRequest>();
            var rabbitMock = new Mock<IRabbitMqRepository>();
            rabbitMock
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((req, _) => published.Add(req))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            var sut = BuildSut(BuildInMemoryContext("test_activity_log"), rabbitMock.Object);

            await sut.AddOrUpdateManualMarketSettlement(requests, "tester", "HK");

            var activityLog = published.Single(p => p.MessageType == "LogActivityList");
            Assert.Equal("r.oe.util.logs", activityLog.RoutingKey);
        }
    }
}
