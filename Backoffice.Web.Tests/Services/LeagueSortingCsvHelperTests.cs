using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class LeagueSortingCsvHelperTests
    {
        private const string Header = "ClientCode,SportId,SegmentId,SegmentName,LeagueId,LeagueName,SortOrder";

        private static LeagueSortingLeague League(int segmentId, string segmentName, int leagueId, string leagueName, int sortOrder) =>
            new LeagueSortingLeague { SegmentId = segmentId, SegmentName = segmentName, LeagueId = leagueId, LeagueName = leagueName, SortOrder = sortOrder };

        private static string File(params string[] dataRows) =>
            string.Join("\r\n", new[] { Header }.Concat(dataRows));

        // ---------------------------------------------------------------
        // BuildCsv
        // ---------------------------------------------------------------

        [Fact]
        public void BuildCsv_WritesTheHeaderAndTheLeaguesInSortOrder_WithABom()
        {
            var csv = LeagueSortingCsvHelper.BuildCsv("QTE-A", 1, new[]
            {
                League(20, "Spain", 200, "La Liga", 2),
                League(10, "England", 100, "Premier League", 1)
            });

            var lines = csv.TrimStart('﻿').Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal('﻿', csv[0]);
            Assert.Equal(new[]
            {
                Header,
                "QTE-A,1,10,England,100,Premier League,1",
                "QTE-A,1,20,Spain,200,La Liga,2"
            }, lines);
        }

        [Fact]
        public void BuildCsv_ForAClientWithoutLeagues_IsJustTheHeader()
        {
            var csv = LeagueSortingCsvHelper.BuildCsv("QTE-A", 1, new List<LeagueSortingLeague>());

            Assert.Equal(new[] { Header }, csv.TrimStart('﻿').Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public void BuildCsv_QuotesNamesThatContainACommaOrAQuote()
        {
            var csv = LeagueSortingCsvHelper.BuildCsv("QTE-A", 1, new[] { League(10, "Korea, South", 100, "K \"League\" 1", 1) });

            Assert.Contains("\"Korea, South\"", csv);
            Assert.Contains("\"K \"\"League\"\" 1\"", csv);
        }

        [Fact]
        public void BuildCsv_ThenParseCsv_GivesBackTheSameRows()
        {
            var csv = LeagueSortingCsvHelper.BuildCsv("QTE-A", 1, new[]
            {
                League(10, "Korea, South", 100, "K \"League\" 1", 1),
                League(20, "Spain", 200, "La Liga", 2)
            });

            var (success, errors, rows) = LeagueSortingCsvHelper.ParseCsv(csv, "QTE-A", 1);

            Assert.True(success);
            Assert.Empty(errors);
            Assert.Equal(new[] { (2, 10, 100, 1), (3, 20, 200, 2) }, rows.Select(r => (r.RowNo, r.SegmentId, r.LeagueId, r.SortOrder)));
        }

        [Fact]
        public void BuildFileName_KeepsOnlyLettersAndDigits_AndStampsTheTime()
        {
            var name = LeagueSortingCsvHelper.BuildFileName("QTE-A", "Table Tennis", new DateTime(2026, 10, 12, 8, 5, 9));

            Assert.Equal("LeagueSorting-QTE-A-Table-Tennis-20261012-080509.csv", name);
        }

        // ---------------------------------------------------------------
        // ParseCsv
        // ---------------------------------------------------------------

        [Fact]
        public void ParseCsv_ReadsIdsAndSortOrder_NumberingRowsLikeASpreadsheet()
        {
            var (success, _, rows) = LeagueSortingCsvHelper.ParseCsv(
                File("QTE-A,1,10,England,100,Premier League,3", "QTE-A,1,20,Spain,200,La Liga,1"), "QTE-A", 1);

            Assert.True(success);
            Assert.Equal(new[] { (2, 10, 100, 3), (3, 20, 200, 1) }, rows.Select(r => (r.RowNo, r.SegmentId, r.LeagueId, r.SortOrder)));
        }

        [Fact]
        public void ParseCsv_IgnoresTheNamesColumns_SoEditedOrBrokenNamesDoNotMatter()
        {
            var (success, _, rows) = LeagueSortingCsvHelper.ParseCsv(File("QTE-A,1,10,,100,,1"), "QTE-A", 1);

            Assert.True(success);
            Assert.Single(rows);
        }

        [Fact]
        public void ParseCsv_AcceptsALowerCaseClientCode_AndAHeaderWithTrailingBlankColumns()
        {
            var text = Header + ",,\r\nqte-a,1,10,England,100,Premier League,1,,";

            var (success, _, rows) = LeagueSortingCsvHelper.ParseCsv(text, "QTE-A", 1);

            Assert.True(success);
            Assert.Single(rows);
        }

        [Fact]
        public void ParseCsv_SkipsBlankLines_AndRowsThatAreOnlyCommas()
        {
            var text = File("QTE-A,1,10,England,100,Premier League,1", "", ",,,,,,", "QTE-A,1,20,Spain,200,La Liga,2");

            var (success, _, rows) = LeagueSortingCsvHelper.ParseCsv(text, "QTE-A", 1);

            Assert.True(success);
            Assert.Equal(new[] { 2, 5 }, rows.Select(r => r.RowNo));
        }

        [Fact]
        public void ParseCsv_RefusesAnEmptyFile()
        {
            var (success, errors, rows) = LeagueSortingCsvHelper.ParseCsv(string.Empty, "QTE-A", 1);

            Assert.False(success);
            Assert.Equal(new[] { "The uploaded file has no data." }, errors);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_RefusesAFileWithoutLeagueRows()
        {
            var (success, errors, _) = LeagueSortingCsvHelper.ParseCsv(Header + "\r\n", "QTE-A", 1);

            Assert.False(success);
            Assert.Equal(new[] { "The uploaded file has no league rows." }, errors);
        }

        [Fact]
        public void ParseCsv_RefusesAFileWithAnotherHeader_NamingTheExpectedColumns()
        {
            var (success, errors, _) = LeagueSortingCsvHelper.ParseCsv("StringId,EN\r\n1,a", "QTE-A", 1);

            Assert.False(success);
            var error = Assert.Single(errors);
            Assert.Contains("Unexpected header row", error);
            Assert.Contains(Header.Replace(",", ", "), error);
            Assert.Contains("StringId, EN", error);
        }

        [Fact]
        public void ParseCsv_RefusesAFileMadeForAnotherClientOrSport_BeforeAnythingIsApplied()
        {
            var (success, errors, rows) = LeagueSortingCsvHelper.ParseCsv(
                File("QTE-B,1,10,England,100,Premier League,1", "QTE-A,2,20,Spain,200,La Liga,2"), "QTE-A", 1);

            Assert.False(success);
            Assert.Empty(rows);
            Assert.Equal(new[]
            {
                "Row 2: ClientCode 'QTE-B' is not the selected client (QTE-A).",
                "Row 3: SportId '2' is not the selected sport (1)."
            }, errors);
        }

        [Theory]
        [InlineData("QTE-A,1,x,England,100,Premier League,1", "Row 2: SegmentId must be a whole number.")]
        [InlineData("QTE-A,1,10,England,,Premier League,1", "Row 2: LeagueId must be a whole number.")]
        [InlineData("QTE-A,1,10,England,100,Premier League,first", "Row 2: SortOrder must be a whole number.")]
        [InlineData("QTE-A,1,10,England,100,Premier League,1.5", "Row 2: SortOrder must be a whole number.")]
        public void ParseCsv_RefusesNumbersThatAreNotWholeNumbers_NamingTheRow(string row, string expected)
        {
            var (success, errors, rows) = LeagueSortingCsvHelper.ParseCsv(File(row), "QTE-A", 1);

            Assert.False(success);
            Assert.Empty(rows);
            Assert.Equal(new[] { expected }, errors);
        }

        [Fact]
        public void ParseCsv_ListsAllProblemsOfARow_AndOfSeveralRows()
        {
            var (_, errors, _) = LeagueSortingCsvHelper.ParseCsv(File("QTE-A,1,x,England,y,Premier League,z", "QTE-A,1,10,England,100,Premier League,w"), "QTE-A", 1);

            Assert.Equal(4, errors.Count);
        }

        [Fact]
        public void ParseCsv_ReportsTheFirstTwentyProblems_AndCountsTheRest()
        {
            var badRows = Enumerable.Range(0, 30).Select(_ => "QTE-A,1,10,England,100,Premier League,x").ToArray();

            var (success, errors, _) = LeagueSortingCsvHelper.ParseCsv(File(badRows), "QTE-A", 1);

            Assert.False(success);
            Assert.Equal(21, errors.Count);
            Assert.Equal("And 10 more problems.", errors[^1]);
        }
    }
}
