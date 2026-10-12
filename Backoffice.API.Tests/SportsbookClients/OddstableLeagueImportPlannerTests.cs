using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backoffice.API.Tests.SportsbookClients
{
    public class OddstableLeagueImportPlannerTests
    {
        private const int Football = 1;

        private static LeagueLookupResponseV2.LeagueLookupModelV2 Available(int segmentId, int leagueId) =>
            new LeagueLookupResponseV2.LeagueLookupModelV2
            {
                SportId = Football,
                SportName = "Football",
                SegmentId = segmentId,
                SegmentName = $"Segment {segmentId}",
                LeagueId = leagueId,
                LeagueName = $"League {leagueId}"
            };

        private static OddstableLeagueSortOrder Existing(int segmentId, int leagueId, int sortOrder) =>
            new OddstableLeagueSortOrder
            {
                SportId = Football,
                SportName = "Football",
                SegmentId = segmentId,
                SegmentName = $"Segment {segmentId}",
                LeagueId = leagueId,
                LeagueName = $"League {leagueId}",
                SortOrder = sortOrder
            };

        private static ImportOddstableLeagueRow[] Rows(params (int SegmentId, int LeagueId, int SortOrder)[] rows) =>
            rows.Select((r, index) => new ImportOddstableLeagueRow { RowNo = index + 2, SegmentId = r.SegmentId, LeagueId = r.LeagueId, SortOrder = r.SortOrder }).ToArray();

        private static List<LeagueLookupResponseV2.LeagueLookupModelV2> Catalog(int segmentId, int fromLeague, int toLeague) =>
            Enumerable.Range(fromLeague, toLeague - fromLeague + 1).Select(l => Available(segmentId, l)).ToList();

        private static OddstableLeagueImportPlanner.ImportPlan Plan(
            IReadOnlyCollection<OddstableLeagueSortOrder> current,
            IReadOnlyCollection<LeagueLookupResponseV2.LeagueLookupModelV2> catalog,
            params (int SegmentId, int LeagueId, int SortOrder)[] rows) =>
            OddstableLeagueImportPlanner.Build(Football, "Football", current, catalog, Rows(rows));

        private static List<(int SegmentId, int LeagueId, int SortOrder)> ResultOf(OddstableLeagueImportPlanner.ImportPlan plan) =>
            plan.Result.Select(l => (l.SegmentId, l.LeagueId, l.SortOrder)).ToList();

        [Fact]
        public void MatchedLeagues_TakeTheFirstPositions_InTheFilesSortOrder()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 103), (10, 102, 5), (10, 100, 9), (10, 101, 7));

            Assert.Empty(plan.Errors);
            Assert.Equal(new[] { (10, 102, 1), (10, 101, 2), (10, 100, 3) }, ResultOf(plan));
        }

        [Fact]
        public void LeaguesNotInTheFile_FollowTheImportedOnes_InTheirCurrentOrder()
        {
            var current = new[] { Existing(10, 100, 1), Existing(10, 101, 2), Existing(10, 102, 3) };

            var plan = Plan(current, Catalog(10, 100, 103), (10, 103, 1), (10, 102, 2));

            Assert.Empty(plan.Errors);
            Assert.Equal(new[] { (10, 103, 1), (10, 102, 2), (10, 100, 3), (10, 101, 4) }, ResultOf(plan));
            Assert.Equal(2, plan.RetainedCount);
        }

        [Fact]
        public void RetainedLeague_ThatIsNoLongerInTheLookup_IsStillKept()
        {
            var current = new[] { Existing(10, 100, 1), Existing(10, 999, 2) };

            var plan = Plan(current, Catalog(10, 100, 101), (10, 101, 1));

            Assert.Equal(new[] { (10, 101, 1), (10, 100, 2), (10, 999, 3) }, ResultOf(plan));
        }

        [Fact]
        public void TwoRowsWithTheSameSortOrder_AreOrderedByTheirRowInTheFile()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 102), (10, 102, 1), (10, 100, 1), (10, 101, 1));

            Assert.Equal(new[] { 102, 100, 101 }, plan.Result.Select(l => l.LeagueId));
        }

        [Fact]
        public void ImportedLeagues_GetTheirNamesFromTheLookup()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 100), (10, 100, 1));

            var league = Assert.Single(plan.Result);
            Assert.Equal(("Football", "Segment 10", "League 100"), (league.SportName, league.SegmentName, league.LeagueName));
            Assert.Equal(Football, league.SportId);
        }

        [Fact]
        public void TheCurrentOrderObjects_AreNotChanged()
        {
            var current = new[] { Existing(10, 100, 1), Existing(10, 101, 2) };

            Plan(current, Catalog(10, 100, 101), (10, 101, 1));

            Assert.Equal(new[] { 1, 2 }, current.Select(c => c.SortOrder));
        }

        [Fact]
        public void UnknownLeague_IsSkippedAndReported_WithTheRowNumber()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 101), (10, 100, 1), (10, 999, 2));

            Assert.Empty(plan.Errors);
            Assert.Equal((1, 1), (plan.MatchedCount, plan.UnmatchedCount));
            var unmatched = Assert.Single(plan.Rows, r => !r.Matched);
            Assert.Equal((3, 999), (unmatched.RowNo, unmatched.LeagueId));
            Assert.Equal("The league is not available for this sport.", unmatched.Reason);
            Assert.Equal(new[] { 100 }, plan.Result.Select(l => l.LeagueId));
        }

        [Fact]
        public void LeagueWithTheWrongSegment_IsSkipped_WithAReasonThatSaysSo()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 101), (10, 100, 1), (11, 101, 2));

            var unmatched = Assert.Single(plan.Rows, r => !r.Matched);
            Assert.Equal("The league belongs to a different segment.", unmatched.Reason);
        }

        [Fact]
        public void NoRows_IsAnError()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 101));

            Assert.Equal(new[] { "The file has no league rows." }, plan.Errors);
        }

        [Fact]
        public void NothingMatching_IsAnError_SoItIsNotReportedAsSuccess()
        {
            var plan = Plan(new[] { Existing(10, 100, 1) }, Catalog(10, 100, 101), (10, 999, 1));

            Assert.Single(plan.Errors);
            Assert.Equal(1, plan.UnmatchedCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void SortOrderBelowOne_IsAnError_NamingTheRow(int sortOrder)
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 101), (10, 100, 1), (10, 101, sortOrder));

            Assert.Equal(new[] { "Row 3: Sort Order must be a whole number greater than 0." }, plan.Errors);
        }

        [Fact]
        public void ALeagueListedTwice_IsAnError_PointingAtTheFirstRow()
        {
            var plan = Plan(new List<OddstableLeagueSortOrder>(), Catalog(10, 100, 101), (10, 100, 1), (10, 101, 2), (10, 100, 3));

            Assert.Equal(new[] { "Row 4: league 100 is already listed in row 2." }, plan.Errors);
        }

        [Fact]
        public void FiftyLeagues_AreAllowed_FiftyOneAreNot()
        {
            var catalog = Catalog(10, 1, 60);
            var fifty = Enumerable.Range(1, 50).Select(l => (10, l, l)).ToArray();
            var fiftyOne = Enumerable.Range(1, 51).Select(l => (10, l, l)).ToArray();

            Assert.Empty(Plan(new List<OddstableLeagueSortOrder>(), catalog, fifty).Errors);

            var plan = Plan(new List<OddstableLeagueSortOrder>(), catalog, fiftyOne);
            Assert.Equal(new[] { "The result would have 51 leagues. A client can have at most 50." }, plan.Errors);
        }

        [Fact]
        public void TheCapCountsTheLeaguesTheClientKeeps()
        {
            var current = Enumerable.Range(1, 49).Select(l => Existing(10, l, l)).ToList();

            var plan = Plan(current, Catalog(10, 1, 60), (10, 50, 1), (10, 51, 2));

            Assert.Equal(new[] { "The result would have 51 leagues. A client can have at most 50." }, plan.Errors);
        }
    }
}
