using Backoffice.Web.Models;
using Backoffice.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static Backoffice.Shared.Models.ManualBetDetailsModel;

namespace Backoffice.Web.Tests.Services
{
    // BO-136: the history pages' "Export To Excel" builds this CSV from the rows the table shows: the table's
    // headers, rows numbered from 1, multi-line cells kept as one quoted cell, and the BO-137 formula escape.
    public class HistoryExportCsvHelperTests
    {
        private const char Bom = (char)0xFEFF;

        private static string[] Lines(string csv)
        {
            Assert.Equal(Bom, csv[0]);
            return csv.Substring(1).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        }

        private static CancelBetHistoryModel.BetDetails ACancelledBet(string reason = "Duplicate bet") => new()
        {
            BetId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SettlementDate = new DateTime(2026, 11, 4, 13, 5, 6),
            BetDate = new DateTime(2026, 11, 4, 9, 0, 0),
            UserId = "member01",
            ClientName = "Client A",
            CancelledBy = "operator1",
            Reason = reason,
            OddsTypeName = "Decimal",
            Selections = new List<CancelBetHistoryModel.BetSelectionDetails>
            {
                new()
                {
                    SportName = "Soccer",
                    LeagueName = "Premier League",
                    FixtureName = "Arsenal vs. Chelsea",
                    FixtureStartDate = new DateTime(2026, 11, 4, 20, 0, 0),
                    SelectionName = "Arsenal",
                    MarketName = "1x2",
                    Odds = 1.5m,
                    BetScore = "0-0",
                    SettlementScore = "2-1"
                }
            }
        };

        [Fact]
        public void CancelBetHistory_HasTheTableColumnsAndOneNumberedRowPerBet()
        {
            var lines = Lines(HistoryExportCsvHelper.BuildCancelBetHistoryCsv(new[] { ACancelledBet(), ACancelledBet("Second") }));

            Assert.Equal("No.,Date Cancelled,Reference ID,Bet Date,Selection Cancelled,Member Name,Partner/SB Client Name,Cancelled By,Reason", lines[0]);
            Assert.StartsWith("1,2026/11/04 01:05:06 PM,11111111-2222-3333-4444-555555555555,2026/11/04 09:00:00 AM,\"Soccer\nPremier League\nArsenal vs. Chelsea\n2026/11/04 08:00:00 PM\nArsenal\n1x2 @ 1.50 (Decimal)\nBet Result: 0-0\nFinal Result: 2-1\",member01,Client A,operator1,Duplicate bet", lines[1]);
            Assert.StartsWith("2,", lines[2]);
            Assert.EndsWith(",Second", lines[2]);
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void CancelBetHistory_ABetWithoutSelectionsStillExports()
        {
            var bet = ACancelledBet();
            bet.Selections = null;

            var lines = Lines(HistoryExportCsvHelper.BuildCancelBetHistoryCsv(new[] { bet }));

            Assert.Equal("1,2026/11/04 01:05:06 PM,11111111-2222-3333-4444-555555555555,2026/11/04 09:00:00 AM,,member01,Client A,operator1,Duplicate bet", lines[1]);
        }

        [Fact]
        public void TextThatExcelWouldRunAsAFormula_IsMarkedAsText()
        {
            var lines = Lines(HistoryExportCsvHelper.BuildCancelBetHistoryCsv(new[] { ACancelledBet("=HYPERLINK(\"http://x\")") }));

            Assert.EndsWith(",\"'=HYPERLINK(\"\"http://x\"\")\"", lines[1]);
        }

        [Fact]
        public void ManualBetSettlement_UsesTheNewReasonAndTheToStatusName()
        {
            var bet = new ManualBetDetails
            {
                BetId = "bet-1",
                UserId = "member01",
                BetStatusName = "Running",
                NewBetStatusId = "3000",
                Reason = "Old reason",
                NewReason = "New reason",
                OddsTypeName = "Decimal",
                SettlementDate = new DateTime(2026, 11, 4, 13, 0, 0),
                BetDate = new DateTime(2026, 11, 4, 9, 0, 0),
                Selections = new List<Backoffice.Shared.Models.BetSelectionDetails>
                {
                    new() { SportName = "Soccer", LeagueName = "League", FixtureName = "A vs. B", MarketName = "1x2", SelectionName = "A", Odds = 2m, BetScore = "0-0", SettlementScore = "1-0", FixtureStartDate = new DateTime(2026, 11, 4, 20, 0, 0) }
                }
            };

            var lines = Lines(HistoryExportCsvHelper.BuildManualBetSettlementCsv(new[] { (bet, "operator1") }));

            Assert.Equal("#,Date Settled,Reference ID,Sports,Bet Date,Event Details,From Status,To Status,Member Name,Settled By,Reason", lines[0]);
            Assert.StartsWith("1,2026/11/04 01:00:00 PM,bet-1,Soccer,2026/11/04 09:00:00 AM,\"League\nA vs. B\n2026/11/04 08:00:00 PM\nA\n1x2 @ 2 (Decimal)\n0-0, Final Score: 1-0\",Running,Won,member01,operator1,New reason", lines[1]);
        }

        [Theory]
        [InlineData("Settled", "3000", "Settled")]
        [InlineData(null, "3000", "Won")]
        [InlineData("", "4000", "Lost")]
        [InlineData(null, "not a number", "")]
        public void ToStatusName_PrefersTheStoredNameThenTheStatusId(string? name, string id, string expected)
        {
            Assert.Equal(expected, HistoryExportCsvHelper.ToStatusName(new ManualBetDetails { NewBetStatusName = name, NewBetStatusId = id }));
        }

        [Fact]
        public void MarketSettlement_HasTheTableColumnsAndTheMarketLine()
        {
            var settlement = new ManualMarketSettlementAuditModel
            {
                FixtureName = "A vs. B",
                MarketName = "Total Goals",
                Line = "2.5",
                SportName = "Soccer",
                SegmentName = "England",
                LeagueName = "League",
                SelectionName = "Over",
                ToStatusName = "Won",
                ManualSettledBy = "operator1",
                Reason = "Feed missing",
                FixtureStartDate = new DateTime(2026, 11, 4, 20, 0, 0, DateTimeKind.Local),
                TimeStamp = new DateTime(2026, 11, 4, 22, 0, 0, DateTimeKind.Local)
            };

            var lines = Lines(HistoryExportCsvHelper.BuildMarketSettlementCsv(new[] { settlement }));

            Assert.Equal("#,Fixture,Market Name,Sport,Event Details,Selection,Status,Settled By,Date Settled,Reason", lines[0]);
            Assert.Equal("1,A vs. B,Total Goals,Soccer,\"England\nLeague\nA vs. B\n2026/11/04 08:00:00 PM\nTotal Goals o 2.5 u\",Over,Won,operator1,2026/11/04 10:00:00 PM,Feed missing", lines[1]);
        }

        [Theory]
        [InlineData("Total Goals", "2.5", "o 2.5 u")]
        [InlineData("Asian Handicap", "-1", "h -1 a")]
        [InlineData("1x2", "1", "")]
        [InlineData("Total Goals", "", "")]
        [InlineData("Total Goals", "abc", "")]
        public void MarketLine_ShowsTotalsAndHandicaps(string market, string line, string expected)
        {
            Assert.Equal(expected, HistoryExportCsvHelper.MarketLine(market, line));
        }

        [Fact]
        public void MessageSettlement_HasTheTableColumns()
        {
            var settlement = new MessageSettlementModel
            {
                BetDate = new DateTime(2026, 11, 4, 9, 30, 0),
                BetId = "bet-9",
                Sports = "Tennis",
                MessagesSettled = "Player A vs. Player B",
                SettledBy = "operator2",
                DateSettled = new DateTime(2026, 11, 4, 10, 0, 0),
                Reason = "Resent"
            };

            var lines = Lines(HistoryExportCsvHelper.BuildMessageSettlementCsv(new[] { settlement }));

            Assert.Equal("#,Date and Time,Bet ID,Sports,Message Settled,Settled By,Date Settled,Reason", lines[0]);
            Assert.Equal("1,2026/11/04 09:30:00 AM,bet-9,Tennis,Player A vs. Player B,operator2,2026/11/04 10:00:00 AM,Resent", lines[1]);
        }

        [Fact]
        public void NoRows_GivesOnlyTheHeader()
        {
            Assert.Single(Lines(HistoryExportCsvHelper.BuildMessageSettlementCsv(Enumerable.Empty<MessageSettlementModel>())));
        }

        [Fact]
        public void FileName_IsThePageAndTheUtcTime()
        {
            Assert.Equal("cancel-bet-history-20261105-140102.csv", HistoryExportCsvHelper.FileName("cancel-bet-history", new DateTime(2026, 11, 5, 14, 1, 2)));
        }
    }
}
