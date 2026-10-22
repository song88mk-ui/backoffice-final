using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Trading;
using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.PriceBoost
{
    // BO-42: the price boost and boosted settings routes of the Trading controller now read and write the Trading
    // DB here instead of going through the Trading API. The list route also reads Mongo, so what it builds is
    // covered by PriceBoostListBuilderTests.
    public class TradingRepositoryPriceBoostTests
    {
        private static readonly Guid QteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime Stamp = new DateTime(2026, 10, 22, 8, 0, 0, DateTimeKind.Utc);

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private sealed class Harness
        {
            public TradingContext Trading { get; }
            public Mock<IRabbitMqRepository> Rabbit { get; } = new Mock<IRabbitMqRepository>();
            public List<MessageRequest> Published { get; } = new List<MessageRequest>();
            public TradingRepository Sut { get; }

            public Harness()
            {
                Trading = new TradingContext(new DbContextOptionsBuilder<TradingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

                Rabbit
                    .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                    .Callback<MessageRequest, string>((request, _) => Published.Add(request))
                    .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

                var configuration = new Mock<IConfiguration>();
                configuration.Setup(c => c["Custom:MongoDB-Sportsbook"]).Returns("mongodb://localhost:27017");

                Sut = new TradingRepository(
                    configuration.Object,
                    new Mock<IBackofficeActivityLogRepository>().Object,
                    new Mock<IMemCache>().Object,
                    NullLogger<TradingRepository>.Instance,
                    Rabbit.Object,
                    Trading,
                    new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
            }

            public List<PriceBoostConfig> SavedBoosts() => Trading.PriceBoostConfig.AsNoTracking().ToList();
        }

        private static PriceBoostConfigModel ABoost(string client = "QTE", string template = "1", string specifier = "", string odds = "2.50", string marketTemplate = "1", string fixture = "pf1", string marketSpecifier = "") =>
            new PriceBoostConfigModel
            {
                ClientCode = client,
                SportId = 10,
                SegmentId = 1,
                LeagueId = 100,
                FixtureTypeId = 0,
                ProviderFixtureId = fixture,
                MarketTemplateId = marketTemplate,
                MarketSpecifier = marketSpecifier,
                SelectionTemplateId = template,
                SelectionSpecifier = specifier,
                SelectionName = "Home",
                BoostedSelectionName = "Home Boost",
                BoostedOddsStr = odds,
                UserId = "u1",
                UserName = "User One",
                TimeStamp = Stamp
            };

        private static PriceBoostConfig ASavedRow(string client = "QTE", string template = "1", string specifier = "", decimal odds = 2.00m, string marketTemplate = "1", string fixture = "pf1") =>
            new PriceBoostConfig
            {
                ClientCode = client,
                SportId = 10,
                SegmentId = 1,
                LeagueId = 100,
                FixtureTypeId = 0,
                ProviderFixtureId = fixture,
                MarketTemplateId = marketTemplate,
                MarketSpecifier = string.Empty,
                SelectionTemplateId = template,
                SelectionSpecifier = specifier,
                BoostedOdds = odds,
                UserId = "old-user",
                UserName = "Old User",
                TimeStamp = Stamp.AddDays(-1),
                BoostedSelectionName = "Home Boost",
                BoostedSelectionTranslation = "[]"
            };

        private static async Task<Harness> WithSavedRows(params PriceBoostConfig[] rows)
        {
            var harness = new Harness();
            harness.Trading.PriceBoostConfig.AddRange(rows);
            await harness.Trading.SaveChangesAsync();
            return harness;
        }

        // ---------------------------------------------------------------
        // Saving boosts
        // ---------------------------------------------------------------

        [Fact]
        public async Task Save_WithoutAnyBoost_SaysThereIsNothingToUpdate()
        {
            var harness = new Harness();

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel>(), "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(StatusCode.Error, result.Result.StatusCode);
            Assert.Equal("No price boost config to be updated", result.Result.Message);
            Assert.False(result.Result.IsBoosted);
        }

        [Fact]
        public async Task Save_AddsANewBoost_CuttingTheOddsToTwoDecimals()
        {
            var harness = new Harness();

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(odds: "2.999") }, "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            var saved = Assert.Single(harness.SavedBoosts());
            Assert.Equal((2.99m, "QTE", "pf1", "1"), (saved.BoostedOdds, saved.ClientCode, saved.ProviderFixtureId, saved.SelectionTemplateId));
            Assert.Equal(("u1", "User One", "Home Boost", Stamp), (saved.UserId, saved.UserName, saved.BoostedSelectionName, saved.TimeStamp));
        }

        [Fact]
        public async Task Save_TellsWhichClientsNowBoostTheFixtureAndTheMarket()
        {
            var harness = await WithSavedRows(
                ASavedRow(client: "KRJ"),
                ASavedRow(client: "GSA", marketTemplate: "2"));

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(client: "QTE") }, "HK");

            Assert.True(result.Result.IsBoosted);
            Assert.Equal(new[] { "GSA", "KRJ", "QTE" }, result.Result.ClientBoosted.Split('|').OrderBy(c => c));
            Assert.Equal(new[] { "KRJ", "QTE" }, result.Result.ClientMarketBoosted.Split('|').OrderBy(c => c));
        }

        [Fact]
        public async Task Save_UpdatesTheOddsOfABoostThatIsAlreadySaved()
        {
            var harness = await WithSavedRows(ASavedRow(odds: 2.00m));

            await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(odds: "3.50") }, "HK");

            var saved = Assert.Single(harness.SavedBoosts());
            Assert.Equal((3.50m, "u1", "User One", Stamp), (saved.BoostedOdds, saved.UserId, saved.UserName, saved.TimeStamp));
        }

        [Fact]
        public async Task Save_LeavesABoostAloneWhenNothingAboutItChanged()
        {
            var harness = await WithSavedRows(ASavedRow(odds: 2.50m));
            var same = ABoost(odds: "2.50");
            same.SelectionName = "Home Boost";
            same.BoostedSelectionTranslations = new List<BoostedSelectionTranslation>();

            await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { same }, "HK");

            var saved = Assert.Single(harness.SavedBoosts());
            Assert.Equal(("old-user", Stamp.AddDays(-1)), (saved.UserId, saved.TimeStamp));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("0.00")]
        public async Task Save_DeletesASavedBoost_WhenTheOddsAreEmptyOrZero(string odds)
        {
            var harness = await WithSavedRows(ASavedRow(), ASavedRow(client: "KRJ"));

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(odds: odds) }, "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(new[] { "KRJ" }, harness.SavedBoosts().Select(b => b.ClientCode));
        }

        [Fact]
        public async Task Save_DoesNotAddABoost_WhenTheOddsAreEmpty()
        {
            var harness = new Harness();

            await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(odds: "") }, "HK");

            Assert.Empty(harness.SavedBoosts());
        }

        [Fact]
        public async Task Save_ReplacesTheSelectionTheClientBoostedInTheMarket_AndReturnsIt()
        {
            var harness = await WithSavedRows(ASavedRow(template: "1", specifier: "a", odds: 2.00m));

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(template: "2", specifier: "b", odds: "3.00") }, "HK");

            var saved = Assert.Single(harness.SavedBoosts());
            Assert.Equal(("2", "b", 3.00m), (saved.SelectionTemplateId, saved.SelectionSpecifier, saved.BoostedOdds));
            var replaced = Assert.Single(result.Result.ExistingPriceBoostConfigs);
            Assert.Equal(("1", "a", 2.00m), (replaced.SelectionTemplateId, replaced.SelectionSpecifier, replaced.BoostedOdds));
        }

        // The Trading API only replaced when both differed, which left two selections in one market.
        [Theory]
        [InlineData("2", "")]
        [InlineData("1", "other")]
        public async Task Save_ReplacesTheSelectionEvenWhenOnlyTheTemplateOrOnlyTheSpecifierDiffers(string template, string specifier)
        {
            var harness = await WithSavedRows(ASavedRow(template: "1", specifier: "", odds: 2.00m));

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(template: template, specifier: specifier, odds: "3.00") }, "HK");

            var saved = Assert.Single(harness.SavedBoosts());
            Assert.Equal((template, specifier, 3.00m), (saved.SelectionTemplateId, saved.SelectionSpecifier, saved.BoostedOdds));
            Assert.Single(result.Result.ExistingPriceBoostConfigs);
        }

        [Fact]
        public async Task Save_KeepsTheBoostsOfOtherClientsAndOtherMarkets()
        {
            var harness = await WithSavedRows(
                ASavedRow(client: "KRJ", template: "1"),
                ASavedRow(client: "QTE", template: "1", marketTemplate: "2"),
                ASavedRow(client: "QTE", template: "1", fixture: "pf2"));

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(client: "QTE", template: "2") }, "HK");

            Assert.Equal(4, harness.SavedBoosts().Count);
            Assert.Empty(result.Result.ExistingPriceBoostConfigs);
        }

        [Fact]
        public async Task Save_SavesTheBoostsOfEveryClientInOneRequest()
        {
            var harness = new Harness();

            await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(client: "QTE"), ABoost(client: "KRJ"), ABoost(client: "GSA", odds: "") }, "HK");

            Assert.Equal(new[] { "KRJ", "QTE" }, harness.SavedBoosts().Select(b => b.ClientCode).OrderBy(c => c));
        }

        [Fact]
        public async Task Save_PublishesNothing_BecauseTheWebWritesTheActivityLog()
        {
            var harness = new Harness();

            await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost() }, "HK");

            Assert.Empty(harness.Published);
        }

        [Fact]
        public async Task Save_ReturnsAnError_AndSavesNothing_WhenTheOddsAreNotANumber()
        {
            var harness = new Harness();

            var result = await harness.Sut.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel> { ABoost(client: "KRJ", odds: "2.50"), ABoost(client: "QTE", odds: "abc") }, "HK");

            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Empty(harness.SavedBoosts());
        }

        // ---------------------------------------------------------------
        // Boosted settings: list
        // ---------------------------------------------------------------

        private static async Task<Harness> WithClients()
        {
            var harness = new Harness();
            harness.Trading.Client.AddRange(
                new Client { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports" },
                new Client { ClientId = Guid.NewGuid(), ClientCode = "KRJ", ClientName = "Krj Games" },
                new Client { ClientId = Guid.NewGuid(), ClientCode = "GSA", ClientName = "Gsa Bets" });
            harness.Trading.BoostedSettings.Add(new BoostedSettings { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports", Cap = 0.05m });
            await harness.Trading.SaveChangesAsync();
            return harness;
        }

        [Fact]
        public async Task Settings_ListEveryRequestedClientWithItsCap_ZeroWhenNoneIsSaved()
        {
            var harness = await WithClients();

            var result = await harness.Sut.GetBoostedSettings(new BoostedSettingsRequest { ClientCodes = new List<string> { "QTE", "KRJ", "NOPE" } }, "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(new[] { ("KRJ", 0m), ("QTE", 0.05m) }, result.Result.OrderBy(c => c.ClientCode).Select(c => (c.ClientCode, c.Cap)));
            Assert.Equal(QteId, result.Result.Single(c => c.ClientCode == "QTE").ClientId);
        }

        [Fact]
        public async Task Settings_CanBeFilteredByClientName_IgnoringCase()
        {
            var harness = await WithClients();

            var result = await harness.Sut.GetBoostedSettings(new BoostedSettingsRequest { ClientCodes = new List<string> { "QTE", "KRJ", "GSA" }, Search = "GAMES" }, "HK");

            Assert.Equal(new[] { "KRJ" }, result.Result.Select(c => c.ClientCode));
        }

        [Fact]
        public async Task Settings_ListNothing_WhenNoClientIsRequested()
        {
            var harness = await WithClients();

            var result = await harness.Sut.GetBoostedSettings(new BoostedSettingsRequest { ClientCodes = null }, "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Empty(result.Result);
        }

        // ---------------------------------------------------------------
        // Boosted settings: save
        // ---------------------------------------------------------------

        [Fact]
        public async Task SavingTheCap_CreatesItForAClientWithoutOne_CutToFourDecimals()
        {
            var harness = await WithClients();
            var krjId = harness.Trading.Client.Single(c => c.ClientCode == "KRJ").ClientId;

            var result = await harness.Sut.AddOrUpdateBoostedSettings(new BoostedSettingsModel { ClientId = krjId, ClientCode = "KRJ", ClientName = "Krj Games", Cap = 0.123456m }, UserId.ToString(), "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.True(result.Result);
            var saved = harness.Trading.BoostedSettings.AsNoTracking().Single(s => s.ClientId == krjId);
            Assert.Equal((0.1234m, "KRJ"), (saved.Cap, saved.ClientCode));
        }

        [Fact]
        public async Task SavingTheCap_UpdatesTheCapOfAClientThatHasOne()
        {
            var harness = await WithClients();

            await harness.Sut.AddOrUpdateBoostedSettings(new BoostedSettingsModel { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports Renamed", Cap = 0.10m }, UserId.ToString(), "HK");

            var saved = Assert.Single(harness.Trading.BoostedSettings.AsNoTracking().ToList());
            Assert.Equal((0.10m, "Qte Sports Renamed"), (saved.Cap, saved.ClientName));
        }

        [Fact]
        public async Task SavingTheCap_TellsTheUserWhoSavedItWithANotification()
        {
            var harness = await WithClients();

            await harness.Sut.AddOrUpdateBoostedSettings(new BoostedSettingsModel { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports", Cap = 0.05m }, UserId.ToString(), "HK");

            var message = Assert.Single(harness.Published);
            Assert.Equal(("OnNewNotification", "r.oe.bm.engine.notifications"), (message.MessageType, message.RoutingKey));

            var notification = JsonSerializer.Deserialize<NotificationsDetailsModel>(message.Payload);
            Assert.Equal(UserId, notification.UserId);
            Assert.Equal(("Boosted Settings", "QTE"), (notification.Page, notification.ClientCode));
            Assert.Equal("<b>New Boosted Settings</b> for <b>Qte Sports</b> - 5.00%.", notification.Details);
            Assert.Equal(((int)NotificationType.BoostedSettings, "SBC.OE.PriceBoost.Settings"), (notification.Type, notification.ClaimType));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task SavingTheCap_SendsNoNotification_WithoutAValidUserId_BecauseItWouldReachEveryone(string userId)
        {
            var harness = await WithClients();

            var result = await harness.Sut.AddOrUpdateBoostedSettings(new BoostedSettingsModel { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports", Cap = 0.07m }, userId, "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Empty(harness.Published);
            Assert.Equal(0.07m, harness.Trading.BoostedSettings.AsNoTracking().Single().Cap);
        }

        [Fact]
        public async Task SavingTheCap_StillSucceeds_WhenTheNotificationCannotBePublished()
        {
            var harness = await WithClients();
            harness.Rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("broker is down"));

            var result = await harness.Sut.AddOrUpdateBoostedSettings(new BoostedSettingsModel { ClientId = QteId, ClientCode = "QTE", ClientName = "Qte Sports", Cap = 0.08m }, UserId.ToString(), "HK");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(0.08m, harness.Trading.BoostedSettings.AsNoTracking().Single().Cap);
        }

        // ---------------------------------------------------------------
        // Publish
        // ---------------------------------------------------------------

        [Fact]
        public async Task Publish_GoesThroughTheBrokerApiOfThisService_WithTheSameRegion()
        {
            var harness = new Harness();
            var request = new MessageRequest { MessageType = "OnPriceBoost", RoutingKey = "r.uof.onpriceboost", Payload = "{}" };
            var expected = new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success };
            harness.Rabbit.Setup(r => r.Publish(request, "SA")).ReturnsAsync(expected);

            var result = await harness.Sut.Publish(request, "SA");

            Assert.Same(expected, result);
            harness.Rabbit.Verify(r => r.Publish(request, "SA"), Times.Once);
        }
    }
}
