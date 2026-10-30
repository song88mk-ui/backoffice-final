using Backoffice.API.Repositories;
using OE.Shared.Data.Sportsbook.Collections;
using System.Linq;
using Xunit;

namespace Backoffice.API.Tests.Config
{
    // BO-144: the rules that shape the sportsbook Mongo lookups into the league list of the OE Config page.
    public class LeagueLookupBuilderTests
    {
        private static LeagueLookup ALeague(int sport, int segment, int league, string name) =>
            new LeagueLookup { LeLoId = $"{sport}:{segment}:{league}", SpId = sport, SegId = segment, LeId = league, LeNa = name };

        private static SportLookup ASport(int sport, string name) => new SportLookup { SpId = sport, SpNa = name };

        private static SegmentLookup ASegment(int sport, int segment, string name) =>
            new SegmentLookup { SeLoId = $"{sport}:{segment}", SpId = sport, SegId = segment, SegNa = name };

        [Fact]
        public void Build_JoinsTheSportAndSegmentNamesOntoEachLeague()
        {
            var result = LeagueLookupBuilder.Build(
                new[] { ALeague(1, 10, 100, "Premier League") },
                new[] { ASport(1, "Soccer") },
                new[] { ASegment(1, 10, "England") });

            var league = Assert.Single(result);
            Assert.Equal(1, league.SportId);
            Assert.Equal("Soccer", league.SportName);
            Assert.Equal(10, league.SegmentId);
            Assert.Equal("England", league.SegmentName);
            Assert.Equal(100, league.LeagueId);
            Assert.Equal("Premier League", league.LeagueName);
        }

        [Fact]
        public void Build_AMissingSportOrSegmentKeepsTheLeagueWithAnEmptyName()
        {
            var result = LeagueLookupBuilder.Build(new[] { ALeague(1, 10, 100, "Premier League") }, new SportLookup[0], new SegmentLookup[0]);

            var league = Assert.Single(result);
            Assert.Equal(string.Empty, league.SportName);
            Assert.Equal(string.Empty, league.SegmentName);
        }

        [Fact]
        public void Build_ASegmentNameIsLookedUpWithinItsOwnSport()
        {
            // the same segment id in two sports must not borrow the other sport's name
            var result = LeagueLookupBuilder.Build(
                new[] { ALeague(1, 5, 100, "A"), ALeague(2, 5, 200, "B") },
                new[] { ASport(1, "Soccer"), ASport(2, "Tennis") },
                new[] { ASegment(1, 5, "England"), ASegment(2, 5, "ATP") });

            Assert.Equal("England", result.Single(l => l.SportId == 1).SegmentName);
            Assert.Equal("ATP", result.Single(l => l.SportId == 2).SegmentName);
        }

        [Fact]
        public void Build_AMissingLeagueNameBecomesEmpty_SoTheSearchOfTheConfigPageDoesNotFail()
        {
            var result = LeagueLookupBuilder.Build(new[] { ALeague(1, 10, 100, null) }, new[] { ASport(1, "Soccer") }, new[] { ASegment(1, 10, "England") });

            Assert.Equal(string.Empty, Assert.Single(result).LeagueName);
        }

        [Fact]
        public void Build_OrdersBySportThenSegmentThenLeagueName()
        {
            var result = LeagueLookupBuilder.Build(
                new[] { ALeague(2, 1, 5, "Zeta"), ALeague(1, 20, 4, "Beta"), ALeague(1, 10, 3, "Beta"), ALeague(1, 10, 2, "Alpha") },
                new[] { ASport(1, "Soccer"), ASport(2, "Tennis") },
                new SegmentLookup[0]);

            Assert.Equal(new[] { 2, 3, 4, 5 }, result.Select(l => l.LeagueId));
        }

        [Fact]
        public void Build_NoLeaguesGivesAnEmptyList()
        {
            Assert.Empty(LeagueLookupBuilder.Build(new LeagueLookup[0], new[] { ASport(1, "Soccer") }, new SegmentLookup[0]));
        }

        [Theory]
        [InlineData("", new int[0])]
        [InlineData(null, new int[0])]
        [InlineData("1", new[] { 1 })]
        [InlineData("1,2", new[] { 1, 2 })]
        [InlineData(" 1 , 2 ,, x, 2", new[] { 1, 2 })]
        public void ParseSportIds_GivesTheSportIds_AndEmptyForEverySport(string text, int[] expected)
        {
            Assert.Equal(expected, LeagueLookupBuilder.ParseSportIds(text));
        }
    }
}
