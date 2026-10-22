using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Sportsbook.Collections;
using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backoffice.API.Tests.PriceBoost
{
    // BO-42: the rules that turn the saved boosts and the sportsbook Mongo data into the Manage Boost list.
    public class PriceBoostListBuilderTests
    {
        private static readonly DateTime Kickoff = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid QteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly ICollection<string> NoPlayerProps = new HashSet<string>();

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static Fixture AFixture(long id = 1, string providerId = "pf1", int status = 0, int type = 0, DateTime? start = null) =>
            new Fixture
            {
                FiId = id,
                PrFiId = providerId,
                FiStId = status,
                FiTyId = type,
                SpId = 10,
                SpNa = "Football",
                SegNa = "England",
                LeNa = "Premier League",
                FiNa = "A vs B",
                CuPe = 1,
                CuMi = 12,
                Sc = "1-0",
                FiStDa = start ?? Kickoff
            };

        private static MarketFilter AMarket(long fixtureId, long id, string template, string name, string clientMargins) =>
            new MarketFilter { FiId = fixtureId, MaId = id, MaTeId = template, MaNa = name, Li = string.Empty, ClCoMa = clientMargins };

        private static SelectionFilter ASelection(long fixtureId, long marketId, long id, string template, string name, decimal odds, string specifier = "", bool active = true, string marketTemplate = "1") =>
            new SelectionFilter { FiId = fixtureId, MaId = marketId, SeId = id, SeTeId = template, SeNa = name, Od = odds, IsAc = active, SeSp = specifier, MaTeId = marketTemplate };

        private static Client AClient(string code, string name, Guid? id = null) =>
            new Client { ClientCode = code, ClientName = name, ClientId = id ?? Guid.NewGuid() };

        private static PriceBoostConfigModel ASavedBoost(string client = "QTE", string providerFixtureId = "pf1", string market = "1", string selection = "2", string specifier = "", decimal odds = 2.50m, string boostedName = null) =>
            new PriceBoostConfigModel
            {
                ClientCode = client,
                ProviderFixtureId = providerFixtureId,
                MarketTemplateId = market,
                SelectionTemplateId = selection,
                SelectionSpecifier = specifier,
                BoostedOdds = odds,
                BoostedOddsStr = odds.ToString("0.00"),
                BoostedSelectionName = boostedName,
                SportId = 10,
                SegmentId = 1,
                LeagueId = 100
            };

        // A fixture with a Match Winner market, a Home and an Away selection at 2.00 and the QTE margin at 0.
        private sealed class Scenario
        {
            public List<Client> Clients = new List<Client> { AClient("QTE", "Qte Sports", QteId) };
            public List<Fixture> Fixtures = new List<Fixture> { AFixture() };
            public List<MarketFilter> Markets = new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "QTE:0") };
            public List<SelectionFilter> Selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "1", "Home", 2.00m),
                ASelection(1, 10, 101, "2", "Away", 2.00m)
            };
        }

        private static List<PriceBoostConfigModel> Run(Scenario s, List<PriceBoostConfigModel> saved, out List<string> ended, PriceBoostConfigRequest request = null)
        {
            var markets = PriceBoostListBuilder.BuildMarkets(s.Markets, s.Selections, s.Clients.Select(c => c.ClientCode).ToList(), NoPlayerProps);

            return PriceBoostListBuilder.Build(saved, request ?? new PriceBoostConfigRequest(), s.Clients, s.Fixtures, s.Selections, markets, out ended);
        }

        private static PriceBoostConfigRequest AFullSelectionRequest(string clientCode = "") =>
            new PriceBoostConfigRequest
            {
                ClientCode = clientCode,
                SportId = "10",
                SegmentId = "1",
                LeagueId = "100",
                ProviderFixtureId = "pf1",
                MarketTemplateId = "1",
                SelectionTemplateId = "2",
                SelectionSpecifier = string.Empty,
                MarketSpecifier = string.Empty
            };

        private static List<decimal> ClientOddsOf(List<MarketFixtureResponse.MarketModel> markets, string clientCode) =>
            markets.SelectMany(m => m.Selections).SelectMany(s => s.ClientOdds).Where(o => o.ClientCode == clientCode).Select(o => o.Odds).ToList();

        // ---------------------------------------------------------------
        // Client odds
        // ---------------------------------------------------------------

        [Fact]
        public void ClientOdds_StayTheMarketOdds_WhenTheClientHasTheMarketMargin()
        {
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "1", "Home", 2.00m), ASelection(1, 10, 101, "2", "Away", 2.00m) };

            var markets = PriceBoostListBuilder.BuildMarkets(new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "QTE:0") }, selections, new List<string> { "QTE" }, NoPlayerProps);

            Assert.Equal(new[] { 2.00m, 2.00m }, ClientOddsOf(markets, "QTE"));
        }

        [Fact]
        public void ClientOdds_AreLowerThanTheMarketOdds_WhenTheClientHasAHigherMargin()
        {
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "1", "Home", 2.00m), ASelection(1, 10, 101, "2", "Away", 2.00m) };

            var markets = PriceBoostListBuilder.BuildMarkets(new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "QTE:0.05") }, selections, new List<string> { "QTE" }, NoPlayerProps);

            Assert.Equal(new[] { 1.90m, 1.90m }, ClientOddsOf(markets, "QTE"));
        }

        [Fact]
        public void ClientOdds_OfPlayerPropMarkets_TakeTheMarginOffTheOdds()
        {
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "1", "Home", 3.00m, marketTemplate: "999"), ASelection(1, 10, 101, "2", "Away", 1.50m, marketTemplate: "999") };
            var market = new List<MarketFilter> { AMarket(1, 10, "999", "Player Props", "QTE:0.10") };

            var plain = PriceBoostListBuilder.BuildMarkets(market, selections, new List<string> { "QTE" }, NoPlayerProps);
            var playerProps = PriceBoostListBuilder.BuildMarkets(market, selections, new List<string> { "QTE" }, new HashSet<string> { "999" });

            Assert.Equal(new[] { 2.61m, 1.40m }, ClientOddsOf(plain, "QTE"));
            Assert.Equal(new[] { 2.70m, 1.35m }, ClientOddsOf(playerProps, "QTE"));
        }

        [Fact]
        public void ClientOdds_OfAnInactiveSelection_AreItsMarketOdds()
        {
            var selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "1", "Home", 2.00m),
                ASelection(1, 10, 101, "2", "Away", 2.00m),
                ASelection(1, 10, 102, "3", "Draw", 5.00m, active: false)
            };

            var markets = PriceBoostListBuilder.BuildMarkets(new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "QTE:0.05") }, selections, new List<string> { "QTE" }, NoPlayerProps);

            var draw = markets.Single().Selections.Single(s => s.SelectionName == "Draw");
            Assert.Equal(5.00m, Assert.Single(draw.ClientOdds).Odds);
        }

        [Fact]
        public void ClientOdds_AreNotCalculated_ForAClientTheMarketHasNoMarginFor()
        {
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "1", "Home", 2.00m), ASelection(1, 10, 101, "2", "Away", 2.00m) };

            var markets = PriceBoostListBuilder.BuildMarkets(new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "KRJ:0.05") }, selections, new List<string> { "QTE" }, NoPlayerProps);

            Assert.Empty(ClientOddsOf(markets, "QTE"));
        }

        [Fact]
        public void TheMasterOddsWithAddedMargin_KeepOddsAtOrBelowOnePointZeroOne_AndAddTwoPointsOfMarginAboveIt()
        {
            Assert.Equal(1.01m, PriceBoostListBuilder.MasterOddsWithAddedMargin(1.01m));
            Assert.Equal(1.92m, PriceBoostListBuilder.MasterOddsWithAddedMargin(2.00m));
        }

        [Fact]
        public void TheSelectionsOfAMarket_AreListedInTheirSortOrder()
        {
            var first = ASelection(1, 10, 100, "1", "Home", 2.00m);
            var second = ASelection(1, 10, 101, "2", "Away", 2.00m);
            first.SeSoOr = 2;
            second.SeSoOr = 1;

            var markets = PriceBoostListBuilder.BuildMarkets(new List<MarketFilter> { AMarket(1, 10, "1", "Match Winner", "QTE:0") }, new List<SelectionFilter> { first, second }, new List<string> { "QTE" }, NoPlayerProps);

            Assert.Equal(new[] { "Away", "Home" }, markets.Single().Selections.Select(s => s.SelectionName));
        }

        // ---------------------------------------------------------------
        // Outright selections
        // ---------------------------------------------------------------

        [Fact]
        public void OutrightSelections_GetTheirTemplateIdFromTheYesNoName_ButMatchSelectionsAreLeftAlone()
        {
            var fixtures = new List<Fixture> { AFixture(1, "pf1", type: 1), AFixture(2, "pf2", type: 0) };
            var selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "x", "Yes", 3.00m),
                ASelection(1, 10, 101, "y", "no", 1.30m),
                ASelection(1, 10, 102, "z", "Maybe", 9.00m),
                ASelection(2, 20, 200, "x", "Yes", 2.00m)
            };

            PriceBoostListBuilder.NormalizeOutrightSelections(fixtures, selections);

            Assert.Equal(new[] { "1", "2", "z", "x" }, selections.Select(s => s.SeTeId));
        }

        // ---------------------------------------------------------------
        // The list
        // ---------------------------------------------------------------

        [Fact]
        public void TheList_ShowsTheSavedBoostWithTheLiveFixtureMarketAndOdds()
        {
            var rows = Run(new Scenario(), new List<PriceBoostConfigModel> { ASavedBoost() }, out var ended);

            var row = Assert.Single(rows);
            Assert.Empty(ended);
            Assert.Equal(("Qte Sports", QteId.ToString()), (row.ClientName, row.ClientId));
            Assert.Equal(("Football", "England", "Premier League", "A vs B"), (row.SportName, row.SegmentName, row.LeagueName, row.FixtureName));
            Assert.Equal((0, "1P", "12", "1-0", Kickoff), (row.FixtureStatusId, row.CurrentPeriod, row.CurrentMinute, row.Score, row.FixtureStartDate));
            Assert.Equal(("Match Winner", "Away"), (row.MarketName, row.SelectionName));
            Assert.Equal((2.00m, 2.00m, 2.50m), (row.BROdds, row.ClientOdds, row.BoostedOdds));
        }

        [Fact]
        public void TheList_ShowsTheSelectionNameAsTheBoostedName_AndItsTranslations_WhenNoneIsSaved()
        {
            var scenario = new Scenario();
            scenario.Selections[1].SeNaTr = new List<SelectionTranslationMDB> { new SelectionTranslationMDB { LaCo = "es", SeNa = "Visitante" } };

            var row = Assert.Single(Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost() }, out _));

            Assert.Equal("Away", row.BoostedSelectionName);
            var translation = Assert.Single(row.BoostedSelectionTranslations);
            Assert.Equal(("Visitante", "QTE", "es"), (translation.BoostedSelectionName, translation.ClientCode, translation.Language.LanguageCode));
        }

        [Fact]
        public void TheList_KeepsTheBoostedNameThatWasSaved()
        {
            var row = Assert.Single(Run(new Scenario(), new List<PriceBoostConfigModel> { ASavedBoost(boostedName: "Away at a boost") }, out _));

            Assert.Equal("Away at a boost", row.BoostedSelectionName);
            Assert.Equal("Away", row.SelectionName);
        }

        [Fact]
        public void TheList_LeavesOutABoostWhoseFixtureIsGone_AndReportsItAsEnded()
        {
            var rows = Run(new Scenario(), new List<PriceBoostConfigModel> { ASavedBoost(), ASavedBoost(providerFixtureId: "pf9") }, out var ended);

            Assert.Equal(new[] { "pf1" }, rows.Select(r => r.ProviderFixtureId));
            Assert.Equal(new[] { "pf9" }, ended);
        }

        [Fact]
        public void TheList_LeavesOutAFixtureThatIsOver_WithoutReportingItAsEnded()
        {
            var scenario = new Scenario();
            scenario.Fixtures[0].FiStId = 3;

            var rows = Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost() }, out var ended);

            Assert.Empty(rows);
            Assert.Empty(ended);
        }

        [Fact]
        public void TheList_LeavesOutABoostOfAClientThatDoesNotExist_InsteadOfFailing()
        {
            var rows = Run(new Scenario(), new List<PriceBoostConfigModel> { ASavedBoost(client: "ZZZ"), ASavedBoost() }, out _);

            Assert.Equal(new[] { "QTE" }, rows.Select(r => r.ClientCode));
        }

        [Fact]
        public void TheList_MatchesASelectionWhoseSpecifierWasSavedWithTheOtherUofVersion()
        {
            var scenario = new Scenario();
            scenario.Selections[1].SeSp = "uof:1";

            var row = Assert.Single(Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost(specifier: "uof:3") }, out _));

            Assert.Equal("Away", row.SelectionName);
        }

        [Fact]
        public void TheList_ShowsTheMarketOddsAsTheClientOdds_ForOutrights()
        {
            var scenario = new Scenario();
            scenario.Fixtures[0].FiTyId = 1;
            scenario.Markets[0].ClCoMa = "QTE:0.05";

            var row = Assert.Single(Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost() }, out _));

            Assert.Equal(2.00m, row.ClientOdds);
        }

        [Fact]
        public void TheList_ShowsTheClientOddsOfTheClientsOwnMargin()
        {
            var scenario = new Scenario();
            scenario.Markets[0].ClCoMa = "QTE:0.05";

            var row = Assert.Single(Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost() }, out _));

            Assert.Equal((2.00m, 1.90m), (row.BROdds, row.ClientOdds));
        }

        [Fact]
        public void TheList_IsOrderedByTheLatestKickoffFirst_ThenByClient()
        {
            var scenario = new Scenario();
            scenario.Clients.Add(AClient("KRJ", "Krj Sports"));
            scenario.Fixtures.Add(AFixture(2, "pf2", start: Kickoff.AddDays(1)));
            scenario.Markets.Add(AMarket(2, 20, "1", "Match Winner", "QTE:0|KRJ:0"));
            scenario.Selections.Add(ASelection(2, 20, 200, "2", "Away", 2.00m));

            var rows = Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost(), ASavedBoost(client: "KRJ"), ASavedBoost(providerFixtureId: "pf2") }, out _);

            Assert.Equal(new[] { "pf2:QTE", "pf1:KRJ", "pf1:QTE" }, rows.Select(r => $"{r.ProviderFixtureId}:{r.ClientCode}"));
        }

        // ---------------------------------------------------------------
        // Empty rows to fill in
        // ---------------------------------------------------------------

        [Fact]
        public void WhenOneSelectionIsRequestedAndNothingIsSaved_EveryClientGetsAnEmptyRowToFillIn()
        {
            var scenario = new Scenario();
            scenario.Clients.Add(AClient("KRJ", "Krj Sports"));
            scenario.Markets[0].ClCoMa = "QTE:0|KRJ:0";

            var rows = Run(scenario, new List<PriceBoostConfigModel>(), out _, AFullSelectionRequest());

            Assert.Equal(new[] { "KRJ", "QTE" }, rows.Select(r => r.ClientCode));
            Assert.All(rows, r =>
            {
                Assert.Equal((0m, string.Empty, "Away"), (r.BoostedOdds, r.BoostedOddsStr, r.SelectionName));
                Assert.NotEmpty(r.ClientName);
            });
        }

        [Fact]
        public void WhenOneClientIsRequestedAndNothingIsSaved_OnlyThatClientGetsAnEmptyRow()
        {
            var scenario = new Scenario();
            scenario.Clients.Add(AClient("KRJ", "Krj Sports"));

            var rows = Run(scenario, new List<PriceBoostConfigModel>(), out _, AFullSelectionRequest("KRJ"));

            Assert.Equal(new[] { "KRJ" }, rows.Select(r => r.ClientCode));
        }

        [Fact]
        public void WhenOneSelectionIsRequested_ClientsThatAlreadyHaveABoostKeepIt_AndTheRestGetAnEmptyRow()
        {
            var scenario = new Scenario();
            scenario.Clients.Add(AClient("KRJ", "Krj Sports"));

            var rows = Run(scenario, new List<PriceBoostConfigModel> { ASavedBoost() }, out _, AFullSelectionRequest());

            Assert.Equal((2.50m, 0m), (rows.Single(r => r.ClientCode == "QTE").BoostedOdds, rows.Single(r => r.ClientCode == "KRJ").BoostedOdds));
        }

        [Fact]
        public void WhenTheRequestDoesNotNameAFullSelection_NoEmptyRowsAreAdded()
        {
            var rows = Run(new Scenario(), new List<PriceBoostConfigModel>(), out _, new PriceBoostConfigRequest { SportId = "10" });

            Assert.Empty(rows);
        }

        // ---------------------------------------------------------------
        // Period names
        // ---------------------------------------------------------------

        [Theory]
        [InlineData(1, "1P")]
        [InlineData(6, "1H")]
        [InlineData(31, "Halftime")]
        [InlineData(71, "G1")]
        [InlineData(539, "9th inning")]
        [InlineData(12345, "")]
        public void ThePeriodNames_AreShort(int period, string expected)
        {
            Assert.Equal(expected, PriceBoostListBuilder.ToShortPeriodName(period));
        }
    }
}
