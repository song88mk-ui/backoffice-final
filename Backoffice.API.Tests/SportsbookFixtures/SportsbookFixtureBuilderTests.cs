using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Sportsbook.Collections;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backoffice.API.Tests.SportsbookFixtures
{
    // BO-42: the rules that shape the sportsbook Mongo documents into the fixture lists of the pickers.
    public class SportsbookFixtureBuilderTests
    {
        private static readonly DateTime Kickoff = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static Fixture AFixture(long id, string providerId = null, string name = "Arsenal vs. Chelsea", int type = 0, int segment = 1, int league = 100, string segmentName = "England", string leagueName = "Premier League", DateTime? start = null) =>
            new Fixture
            {
                FiId = id,
                PrFiId = providerId ?? $"pf{id}",
                FiNa = name,
                FiTyId = type,
                FiStId = 0,
                FiStNa = "Prematch",
                FiStDa = start ?? Kickoff,
                SpId = 10,
                SpNa = "Football",
                SegId = segment,
                SegNa = segmentName,
                LeId = league,
                LeNa = leagueName,
                CuPe = 1,
                CuMi = 12,
                Sc = "1-0"
            };

        private static MarketFilter AMarket(long fixtureId, long id, string template = "1", string name = "Match Winner", string line = "") =>
            new MarketFilter { FiId = fixtureId, MaId = id, MaTeId = template, MaNa = name, Li = line, MaStId = 1, MaStNa = "Active" };

        private static SelectionFilter ASelection(long fixtureId, long marketId, long id, string template, string name, decimal odds = 2.00m, string specifier = "", int order = 0) =>
            new SelectionFilter { FiId = fixtureId, MaId = marketId, SeId = id, SeTeId = template, SeNa = name, Od = odds, IsAc = true, SeSp = specifier, SeSoOr = order };

        private static List<TierLeagueOrder> Tiers(params (int Sport, int Segment, int League, int Order)[] rows) =>
            rows.Select(r => new TierLeagueOrder { SportId = r.Sport, SegmentId = r.Segment, LeagueId = r.League, SortOrder = r.Order }).ToList();

        // ---------------------------------------------------------------
        // Match fixtures
        // ---------------------------------------------------------------

        [Fact]
        public void AMatchFixture_CarriesWhatThePickersShow()
        {
            var result = SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { AFixture(1) }, new List<MarketFilter>(), new List<SelectionFilter>());

            var fixture = Assert.Single(result);
            Assert.Equal(("1", "pf1", "Arsenal vs. Chelsea", "Arsenal", "Chelsea"), (fixture.FixtureId, fixture.ProviderFixtureId, fixture.FixtureName, fixture.HomeName, fixture.AwayName));
            Assert.Equal((10, "Football", 1, "England", 100, "Premier League"), (fixture.SportId, fixture.SportName, fixture.SegmentId, fixture.SegmentName, fixture.LeagueId, fixture.LeagueName));
            Assert.Equal((0, "Prematch", 0, Kickoff), (fixture.FixtureStatusId, fixture.FixtureStatusName, fixture.FixtureTypeId, fixture.FixtureStartDate));
            Assert.Equal(("1-0", "1P", "12"), (fixture.Score, fixture.CurrentPeriod, fixture.CurrentMinute));
            Assert.Empty(fixture.Markets);
        }

        [Fact]
        public void TheEnglishTranslationIsUsedWhenThereIsOne_AndTheBaseNameWhenThereIsNot()
        {
            var translated = AFixture(1);
            translated.SpNaTr = new List<SportTranslationMDB> { new SportTranslationMDB { LaCo = "ES", SpNa = "Futbol" }, new SportTranslationMDB { LaCo = "EN", SpNa = "Soccer" } };
            translated.SegNaTr = new List<SegmentTranslationMDB> { new SegmentTranslationMDB { LaCo = "EN", SegNa = "Britain" } };
            translated.LeNaTr = new List<LeagueTranslationMDB> { new LeagueTranslationMDB { LaCo = "ES", LeNa = "Liga" } };
            translated.FiNaTr = new List<FixtureTranslationMDB> { new FixtureTranslationMDB { LaCo = "EN", FiNa = "Gunners vs. Blues" } };

            var fixture = Assert.Single(SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { translated }, new List<MarketFilter>(), new List<SelectionFilter>()));

            Assert.Equal(("Soccer", "Britain", "Premier League", "Gunners vs. Blues"), (fixture.SportName, fixture.SegmentName, fixture.LeagueName, fixture.FixtureName));
            Assert.Equal(("Gunners", "Blues"), (fixture.HomeName, fixture.AwayName));
        }

        [Fact]
        public void TheFixtures_AreListedEarliestFirst_ThenByName()
        {
            var fixtures = new List<Fixture>
            {
                AFixture(1, name: "Zulte vs. Anderlecht", start: Kickoff.AddHours(2)),
                AFixture(2, name: "Brugge vs. Genk", start: Kickoff),
                AFixture(3, name: "Aalst vs. Mechelen", start: Kickoff)
            };

            var result = SportsbookFixtureBuilder.BuildMatchFixtures(fixtures, new List<MarketFilter>(), new List<SelectionFilter>());

            Assert.Equal(new[] { "3", "2", "1" }, result.Select(f => f.FixtureId));
        }

        [Fact]
        public void EachFixtureGetsOnlyItsOwnMarketsAndSelections()
        {
            var fixtures = new List<Fixture> { AFixture(1), AFixture(2) };
            var markets = new List<MarketFilter> { AMarket(1, 10), AMarket(2, 20, name: "Over/Under") };
            var selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "1", "Home"),
                ASelection(2, 20, 200, "12", "Over")
            };

            var result = SportsbookFixtureBuilder.BuildMatchFixtures(fixtures, markets, selections);

            var first = result.Single(f => f.FixtureId == "1");
            var second = result.Single(f => f.FixtureId == "2");
            Assert.Equal(("Match Winner", "Home"), (Assert.Single(first.Markets).MarketName, Assert.Single(first.Markets[0].Selections).SelectionName));
            Assert.Equal(("Over/Under", "Over"), (Assert.Single(second.Markets).MarketName, Assert.Single(second.Markets[0].Selections).SelectionName));
        }

        // ---------------------------------------------------------------
        // Markets and selections
        // ---------------------------------------------------------------

        [Fact]
        public void AMarket_CarriesItsTemplateLineStatusAndTranslatedName()
        {
            var market = AMarket(1, 10, template: "18", name: "Total", line: "2.5");
            market.IsBo = true;
            market.MaNaTr = new List<MarketTranslationMDB> { new MarketTranslationMDB { LaCo = "EN", MaNa = "Total Goals" } };

            var result = Assert.Single(SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { AFixture(1) }, new List<MarketFilter> { market }, new List<SelectionFilter>()));

            var built = Assert.Single(result.Markets);
            Assert.Equal(("10", "18", "Total Goals", "2.5", 1, "Active", true), (built.MarketId, built.MarketTemplateId, built.MarketName, built.Line, built.MarketStatusId, built.MarketStatusName, built.IsBoosted));
        }

        [Theory]
        [InlineData("sr:market:18?total=2.5", "total=2.5")]
        [InlineData("a?b?c", "c")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void TheMarketSpecifier_IsTheSelectionSpecifierAfterTheLastQuestionMark(string selectionSpecifier, string expected)
        {
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "12", "Over", specifier: selectionSpecifier) };

            var result = Assert.Single(SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { AFixture(1) }, new List<MarketFilter> { AMarket(1, 10) }, selections));

            Assert.Equal(expected, Assert.Single(result.Markets).MarketSpecifier);
        }

        [Fact]
        public void TheMarkets_AreOrderedByTemplateNumber_ThenLine_ThenName()
        {
            var markets = new List<MarketFilter>
            {
                AMarket(1, 1, template: "10", name: "B"),
                AMarket(1, 2, template: "2", name: "Z"),
                AMarket(1, 3, template: "2", name: "A", line: "3.5"),
                AMarket(1, 4, template: "2", name: "A", line: "1.5"),
                AMarket(1, 5, template: "custom", name: "C")
            };

            var result = Assert.Single(SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { AFixture(1) }, markets, new List<SelectionFilter>()));

            // template 2 first (no line, then 1.5, then 3.5), then template 10, then the template that is not a number
            Assert.Equal(new[] { "2", "4", "3", "1", "5" }, result.Markets.Select(m => m.MarketId));
        }

        [Fact]
        public void TheSelections_AreInTheirSortOrder_WithTranslatedNamesAndInvariantOdds()
        {
            var away = ASelection(1, 10, 101, "2", "Away", 3.25m, order: 1);
            away.SeNaTr = new List<SelectionTranslationMDB> { new SelectionTranslationMDB { LaCo = "EN", SeNa = "Visitors" } };
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "1", "Home", 2.5m, order: 2), away };

            var result = Assert.Single(SportsbookFixtureBuilder.BuildMatchFixtures(new List<Fixture> { AFixture(1) }, new List<MarketFilter> { AMarket(1, 10) }, selections));

            var built = Assert.Single(result.Markets).Selections;
            Assert.Equal(new[] { ("101", "2", "Visitors", "3.25"), ("100", "1", "Home", "2.5") }, built.Select(s => (s.SelectionId, s.SelectionTemplateId, s.SelectionName, s.DecimalOdds)));
            Assert.All(built, s => Assert.True(s.IsActive));
        }

        // ---------------------------------------------------------------
        // Outright fixtures
        // ---------------------------------------------------------------

        [Fact]
        public void AnOutrightFixture_HasNoTeams_AndItsYesNoSelectionsAreNumberedLikeAMatch()
        {
            var fixtures = new List<Fixture> { AFixture(1, name: "World Cup Winner", type: 1) };
            var markets = new List<MarketFilter> { AMarket(1, 10, name: "Qualifies") };
            var selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "x", "Yes", order: 1),
                ASelection(1, 10, 101, "y", "NO", order: 2),
                ASelection(1, 10, 102, "z", "Maybe", order: 3)
            };

            var result = Assert.Single(SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, markets, selections, Tiers(), 10));

            Assert.Equal((string.Empty, string.Empty, 1), (result.HomeName, result.AwayName, result.FixtureTypeId));
            Assert.Equal(new[] { "1", "2", "z" }, Assert.Single(result.Markets).Selections.Select(s => s.SelectionTemplateId));
        }

        [Fact]
        public void AnOutrightMarketWithMoreThanThreeRunners_ListsThemByOdds_AndAShortOneBySortOrder()
        {
            var fixtures = new List<Fixture> { AFixture(1, type: 1), AFixture(2, type: 1) };
            var markets = new List<MarketFilter> { AMarket(1, 10), AMarket(2, 20) };
            var selections = new List<SelectionFilter>
            {
                ASelection(1, 10, 100, "a", "A", 9.0m, order: 1),
                ASelection(1, 10, 101, "b", "B", 2.0m, order: 2),
                ASelection(1, 10, 102, "c", "C", 5.0m, order: 3),
                ASelection(1, 10, 103, "d", "D", 3.0m, order: 4),
                ASelection(2, 20, 200, "a", "A", 9.0m, order: 2),
                ASelection(2, 20, 201, "b", "B", 2.0m, order: 1)
            };

            var result = SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, markets, selections, Tiers(), 10);

            Assert.Equal(new[] { "B", "D", "C", "A" }, result.Single(f => f.FixtureId == "1").Markets[0].Selections.Select(s => s.SelectionName));
            Assert.Equal(new[] { "B", "A" }, result.Single(f => f.FixtureId == "2").Markets[0].Selections.Select(s => s.SelectionName));
        }

        [Fact]
        public void TheOutrights_FollowTheTierOrderOfTheRequestedSport_AndUnrankedLeaguesGoLast()
        {
            var fixtures = new List<Fixture>
            {
                AFixture(1, league: 300, leagueName: "Unranked"),
                AFixture(2, league: 200, leagueName: "Second"),
                AFixture(3, league: 100, leagueName: "First")
            };
            var tiers = Tiers((10, 1, 100, 1), (10, 1, 200, 2), (99, 1, 300, 1));

            var result = SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, new List<MarketFilter>(), new List<SelectionFilter>(), tiers, 10);

            Assert.Equal(new[] { ("First", 1), ("Second", 2), ("Unranked", SportsbookFixtureBuilder.UnrankedLeagueSortOrder) }, result.Select(f => (f.LeagueName, f.SortOrder)));
        }

        [Fact]
        public void WhenAllSportsAreRequested_NoLeagueIsRanked_AndTheOutrightsFollowSegmentLeagueDateAndName()
        {
            var fixtures = new List<Fixture>
            {
                AFixture(1, segmentName: "Spain", leagueName: "La Liga"),
                AFixture(2, segmentName: "England", leagueName: "Premier League", start: Kickoff.AddDays(1)),
                AFixture(3, segmentName: "England", leagueName: "Premier League", start: Kickoff)
            };
            var tiers = Tiers((10, 1, 100, 1));

            var result = SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, new List<MarketFilter>(), new List<SelectionFilter>(), tiers, 0);

            Assert.All(result, f => Assert.Equal(SportsbookFixtureBuilder.UnrankedLeagueSortOrder, f.SortOrder));
            Assert.Equal(new[] { "3", "2", "1" }, result.Select(f => f.FixtureId));
        }

        [Fact]
        public void AnOutrightFixtureThatAppearsTwice_IsListedOnce()
        {
            var fixtures = new List<Fixture> { AFixture(1, type: 1), AFixture(1, type: 1) };
            var markets = new List<MarketFilter> { AMarket(1, 10) };
            var selections = new List<SelectionFilter> { ASelection(1, 10, 100, "a", "A") };

            var result = SportsbookFixtureBuilder.BuildOutrightFixtures(fixtures, markets, selections, Tiers(), 10);

            var fixture = Assert.Single(result);
            Assert.Single(fixture.Markets);
        }
    }
}
