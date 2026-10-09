using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using System.Text.Json;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class LeagueSortingHelperTests
    {
        private static ClientOddstableLeagueOrderListResponse Order(string clientCode, int segmentId, string segmentName, int leagueId, string leagueName, int sortOrder) =>
            new ClientOddstableLeagueOrderListResponse
            {
                ClientCode = clientCode,
                SegmentId = segmentId,
                SegmentName = segmentName,
                LeagueId = leagueId,
                LeagueName = leagueName,
                SortOrder = sortOrder
            };

        private static LeagueSortingEditRow Filled(int segmentId, int leagueId) =>
            new LeagueSortingEditRow { SegmentId = segmentId, SegmentName = $"Segment {segmentId}", LeagueId = leagueId, LeagueName = $"League {leagueId}" };

        // ---------------------------------------------------------------
        // BuildClientRows
        // ---------------------------------------------------------------

        [Fact]
        public void BuildClientRows_IncludesClientsWithoutAnOrder_SortedByClientName()
        {
            var rows = LeagueSortingHelper.BuildClientRows(
                new[] { ("QTE-B", "Beta"), ("QTE-A", "Alpha") },
                new[] { Order("QTE-B", 1, "England", 10, "Premier League", 1) });

            Assert.Equal(new[] { "Alpha", "Beta" }, rows.Select(r => r.ClientName));
            Assert.Empty(rows[0].Leagues);
            Assert.Single(rows[1].Leagues);
        }

        [Fact]
        public void BuildClientRows_OrdersLeaguesBySortOrder_AndMatchesClientCodeIgnoringCase()
        {
            var rows = LeagueSortingHelper.BuildClientRows(
                new[] { ("QTE-A", "Alpha") },
                new[]
                {
                    Order("qte-a", 1, "England", 10, "Premier League", 2),
                    Order("qte-a", 2, "Spain", 20, "La Liga", 1)
                });

            Assert.Equal(new[] { 20, 10 }, rows.Single().Leagues.Select(l => l.LeagueId));
        }

        [Fact]
        public void BuildClientRows_IgnoresOrdersOfClientsTheUserCannotManage()
        {
            var rows = LeagueSortingHelper.BuildClientRows(
                new[] { ("QTE-A", "Alpha") },
                new[] { Order("OTHER", 1, "England", 10, "Premier League", 1) });

            Assert.Empty(rows.Single().Leagues);
        }

        // ---------------------------------------------------------------
        // Filter
        // ---------------------------------------------------------------

        private static List<LeagueSortingClient> SampleClients() => LeagueSortingHelper.BuildClientRows(
            new[] { ("QTE-A", "Alpha"), ("QTE-B", "Beta"), ("QTE-C", "Gamma") },
            new[]
            {
                Order("QTE-A", 1, "England", 10, "Premier League", 1),
                Order("QTE-B", 2, "Spain", 20, "La Liga", 1)
            });

        [Fact]
        public void Filter_KeepsOnlyTickedClients_AndNumbersRowsFromOne()
        {
            var result = LeagueSortingHelper.Filter(SampleClients(), new HashSet<string> { "QTE-B", "QTE-C" }, string.Empty);

            Assert.Equal(new[] { "Beta", "Gamma" }, result.Select(r => r.ClientName));
            Assert.Equal(new[] { 1, 2 }, result.Select(r => r.RowNo));
        }

        [Theory]
        [InlineData("alph", "Alpha")]
        [InlineData("SPAIN", "Beta")]
        [InlineData("premier", "Alpha")]
        public void Filter_Search_MatchesClientSegmentOrLeagueIgnoringCase(string search, string expectedClient)
        {
            var result = LeagueSortingHelper.Filter(SampleClients(), new HashSet<string> { "QTE-A", "QTE-B", "QTE-C" }, search);

            Assert.Equal(expectedClient, Assert.Single(result).ClientName);
        }

        [Fact]
        public void Filter_NoClientTicked_ReturnsNothing()
        {
            Assert.Empty(LeagueSortingHelper.Filter(SampleClients(), new HashSet<string>(), string.Empty));
        }

        // ---------------------------------------------------------------
        // ToEditRows / ValidateRows
        // ---------------------------------------------------------------

        [Fact]
        public void ToEditRows_OrdersBySortOrder()
        {
            var rows = LeagueSortingHelper.ToEditRows(new[]
            {
                new LeagueSortingLeague { SegmentId = 2, LeagueId = 20, SortOrder = 2 },
                new LeagueSortingLeague { SegmentId = 1, LeagueId = 10, SortOrder = 1 }
            });

            Assert.Equal(new[] { 10, 20 }, rows.Select(r => r.LeagueId));
        }

        [Fact]
        public void ToEditRows_WithoutLeagues_StartsWithOneEmptyRow()
        {
            var row = Assert.Single(LeagueSortingHelper.ToEditRows(Array.Empty<LeagueSortingLeague>()));

            Assert.True(row.IsEmpty);
        }

        [Fact]
        public void ValidateRows_RequiresAtLeastOneLeague()
        {
            Assert.Equal("Select at least 1 League", LeagueSortingHelper.ValidateRows(new[] { new LeagueSortingEditRow() }));
        }

        [Fact]
        public void ValidateRows_RejectsAnEmptyRowAmongFilledOnes()
        {
            Assert.Equal("Please remove empty row", LeagueSortingHelper.ValidateRows(new[] { Filled(1, 10), new LeagueSortingEditRow() }));
        }

        [Fact]
        public void ValidateRows_AcceptsFilledRows()
        {
            Assert.Null(LeagueSortingHelper.ValidateRows(new[] { Filled(1, 10), Filled(2, 20) }));
        }

        // ---------------------------------------------------------------
        // Reorder
        // ---------------------------------------------------------------

        [Fact]
        public void Reorder_FollowsTheKeysTheBrowserReports()
        {
            var a = Filled(1, 10);
            var b = Filled(2, 20);
            var c = Filled(3, 30);

            var ordered = LeagueSortingHelper.Reorder(new[] { a, b, c }, new[] { c.Key.ToString(), a.Key.ToString(), b.Key.ToString() });

            Assert.Equal(new[] { c, a, b }, ordered);
        }

        [Fact]
        public void Reorder_ReturnsNull_WhenAKeyIsMissingOrUnknown()
        {
            var a = Filled(1, 10);
            var b = Filled(2, 20);

            Assert.Null(LeagueSortingHelper.Reorder(new[] { a, b }, new[] { a.Key.ToString() }));
            Assert.Null(LeagueSortingHelper.Reorder(new[] { a, b }, new[] { a.Key.ToString(), Guid.NewGuid().ToString() }));
            Assert.Null(LeagueSortingHelper.Reorder(new[] { a, b }, null));
        }

        [Fact]
        public void Reorder_ReturnsNull_WhenAKeyIsRepeated()
        {
            var a = Filled(1, 10);
            var b = Filled(2, 20);

            Assert.Null(LeagueSortingHelper.Reorder(new[] { a, b }, new[] { a.Key.ToString(), a.Key.ToString() }));
        }

        // ---------------------------------------------------------------
        // ToLogRow
        // ---------------------------------------------------------------

        private static readonly (string ClientCode, string ClientName)[] LogClients =
        {
            ("QTE-A", "Alpha"),
            ("QTE-B", "Beta")
        };

        private static string AuditJson(string sportName, string sourceClientCode, params (int SortOrder, string Segment, string League)[] leagues) =>
            JsonSerializer.Serialize(new OddstableLeagueSortingAudit
            {
                SportId = 1,
                SportName = sportName,
                SourceClientCode = sourceClientCode,
                Leagues = leagues.Select(l => new OddstableLeagueSortingAuditLeague
                {
                    SegmentName = l.Segment,
                    LeagueName = l.League,
                    SortOrder = l.SortOrder
                }).ToList()
            });

        [Fact]
        public void ToLogRow_ShapesACopyEntryForDisplay_InTheUsersTimezone()
        {
            var log = new ActivityLogModel
            {
                Page = "SBC.OE.LeagueSorting",
                Payload = "QTE-B",
                Activity = "Copy",
                From = AuditJson("Football", null, (1, "Italy", "Serie A")),
                To = AuditJson("Football", "QTE-A", (2, "Spain", "La Liga"), (1, "England", "Premier League")),
                UserName = "Test User",
                TimeStamp = new DateTime(2026, 10, 1, 10, 0, 0)
            };

            var row = LeagueSortingHelper.ToLogRow(log, LogClients, TimeSpan.FromHours(8));

            Assert.Equal(new DateTime(2026, 10, 1, 18, 0, 0), row.Timestamp);
            Assert.Equal("Beta", row.ClientName);
            Assert.Equal("Copy", row.Action);
            Assert.Equal("Football", row.SportName);
            Assert.Equal("Alpha", row.CopiedFromClientName);
            Assert.Equal(new[] { "Italy > Serie A" }, row.PreviousLeagues);
            Assert.Equal(new[] { "England > Premier League", "Spain > La Liga" }, row.NewLeagues);
            Assert.Equal("Test User", row.UpdatedBy);
        }

        [Fact]
        public void ToLogRow_AnEditEntry_HasNoCopySource_AndFallsBackToTheCodeForUnknownClients()
        {
            var log = new ActivityLogModel
            {
                Payload = "GONE",
                Activity = "Edit",
                From = AuditJson("Football", null, (1, "Italy", "Serie A")),
                To = AuditJson("Football", null, (1, "Spain", "La Liga")),
                TimeStamp = DateTime.UtcNow
            };

            var row = LeagueSortingHelper.ToLogRow(log, LogClients, TimeSpan.Zero);

            Assert.Equal("GONE", row.ClientName);
            Assert.Null(row.CopiedFromClientName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json")]
        public void ToLogRow_AnUnreadableSide_StillShowsTheEntry_WithoutThatSidesLeagues(string brokenFrom)
        {
            var log = new ActivityLogModel
            {
                Payload = "QTE-A",
                Activity = "Add",
                From = brokenFrom,
                To = AuditJson("Football", null, (1, "England", "Premier League")),
                TimeStamp = DateTime.UtcNow
            };

            var row = LeagueSortingHelper.ToLogRow(log, LogClients, TimeSpan.Zero);

            Assert.Empty(row.PreviousLeagues);
            Assert.Equal(new[] { "England > Premier League" }, row.NewLeagues);
        }

        // ---------------------------------------------------------------
        // ToSortOrders
        // ---------------------------------------------------------------

        [Fact]
        public void ToSortOrders_NumbersRowsFromOneInVisualOrder_AndCarriesTheSport()
        {
            var sortOrders = LeagueSortingHelper.ToSortOrders(1, "Football", new[] { Filled(2, 20), Filled(1, 10) });

            Assert.Equal(new[] { (20, 1), (10, 2) }, sortOrders.Select(s => (s.LeagueId, s.SortOrder)));
            Assert.All(sortOrders, s =>
            {
                Assert.Equal(1, s.SportId);
                Assert.Equal("Football", s.SportName);
            });
        }
    }
}
