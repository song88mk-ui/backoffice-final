using System.Globalization;
using System.Text;
using Backoffice.Shared.Enums;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using static Backoffice.Shared.Models.ManualBetDetailsModel;

namespace Backoffice.Web.Services
{
    // BO-136: "Export To Excel" on Cancel Bet History, Manual Bet Settlement, Market Settlement and Message
    // Settlement. The pages used to publish r.bo.util.export for the Utility service to build an .xlsx and send a
    // link; that publish was switched off in 2024 and never replaced. The pages now load every row for the
    // current filters and download a CSV straight away, the same way the other Back Office exports do
    // (BOM + CsvField.Escape + downloadFileFromText). The columns are the table's columns; a cell that shows
    // several lines on screen keeps them as lines inside one cell.
    public static class HistoryExportCsvHelper
    {
        private const string DateTimeFormat = "yyyy/MM/dd hh:mm:ss tt";

        public static string BuildCancelBetHistoryCsv(IEnumerable<CancelBetHistoryModel.BetDetails> bets)
        {
            return Build(
                new[] { "No.", "Date Cancelled", "Reference ID", "Bet Date", "Selection Cancelled", "Member Name", "Partner/SB Client Name", "Cancelled By", "Reason" },
                bets.Select(bet =>
                {
                    var selection = bet.Selections?.FirstOrDefault();
                    var selectionCancelled = selection == null
                        ? string.Empty
                        : Lines(
                            selection.SportName,
                            selection.LeagueName,
                            selection.FixtureName,
                            Format(selection.FixtureStartDate),
                            selection.SelectionName,
                            $"{selection.MarketName} @ {selection.Odds.ToString("N2", CultureInfo.InvariantCulture)} ({bet.OddsTypeName})",
                            $"Bet Result: {selection.BetScore}",
                            $"Final Result: {selection.SettlementScore}");

                    return new[]
                    {
                        Format(bet.SettlementDate),
                        bet.BetId.ToString(),
                        Format(bet.BetDate),
                        selectionCancelled,
                        bet.UserId,
                        bet.ClientName,
                        bet.CancelledBy,
                        bet.Reason
                    };
                }));
        }

        public static string BuildManualBetSettlementCsv(IEnumerable<(ManualBetDetails Bet, string SettledBy)> rows)
        {
            return Build(
                new[] { "#", "Date Settled", "Reference ID", "Sports", "Bet Date", "Event Details", "From Status", "To Status", "Member Name", "Settled By", "Reason" },
                rows.Select(row =>
                {
                    var selection = row.Bet.Selections?.FirstOrDefault();
                    var eventDetails = selection == null
                        ? string.Empty
                        : Lines(
                            selection.LeagueName,
                            selection.FixtureName,
                            Format(selection.FixtureStartDate),
                            selection.SelectionName,
                            $"{selection.MarketName} @ {selection.Odds} ({row.Bet.OddsTypeName})",
                            $"{selection.BetScore}, Final Score: {selection.SettlementScore}");

                    return new[]
                    {
                        Format(row.Bet.SettlementDate),
                        row.Bet.BetId,
                        selection?.SportName,
                        Format(row.Bet.BetDate),
                        eventDetails,
                        row.Bet.BetStatusName,
                        ToStatusName(row.Bet),
                        row.Bet.UserId,
                        row.SettledBy,
                        !string.IsNullOrEmpty(row.Bet.NewReason) ? row.Bet.NewReason : row.Bet.Reason
                    };
                }));
        }

        public static string BuildMarketSettlementCsv(IEnumerable<ManualMarketSettlementAuditModel> settlements)
        {
            return Build(
                new[] { "#", "Fixture", "Market Name", "Sport", "Event Details", "Selection", "Status", "Settled By", "Date Settled", "Reason" },
                settlements.Select(item => new[]
                {
                    item.FixtureName,
                    item.MarketName,
                    item.SportName,
                    Lines(
                        item.SegmentName,
                        item.LeagueName,
                        item.FixtureName,
                        Format(item.FixtureStartDate.ToLocalTime()),
                        $"{item.MarketName} {MarketLine(item.MarketName, item.Line)}".TrimEnd()),
                    item.SelectionName,
                    item.ToStatusName,
                    item.ManualSettledBy,
                    Format(item.TimeStamp.ToLocalTime()),
                    item.Reason
                }));
        }

        public static string BuildMessageSettlementCsv(IEnumerable<MessageSettlementModel> settlements)
        {
            return Build(
                new[] { "#", "Date and Time", "Bet ID", "Sports", "Message Settled", "Settled By", "Date Settled", "Reason" },
                settlements.Select(item => new[]
                {
                    Format(item.BetDate),
                    item.BetId,
                    item.Sports,
                    item.MessagesSettled,
                    item.SettledBy,
                    Format(item.DateSettled),
                    item.Reason
                }));
        }

        // The "To Status" column of Manual Bet Settlement: the stored name, or the BetStatus name of the id.
        public static string ToStatusName(ManualBetDetails bet)
        {
            if (!string.IsNullOrEmpty(bet.NewBetStatusName))
                return bet.NewBetStatusName;

            return int.TryParse(bet.NewBetStatusId, out var statusId) ? ((BetStatus)statusId).ToString() : string.Empty;
        }

        // The "o 2.5 u" / "h -1 a" line shown after the market name on Market Settlement.
        public static string MarketLine(string marketName, string line)
        {
            if (string.IsNullOrEmpty(marketName) || string.IsNullOrEmpty(line)
                || !decimal.TryParse(line, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return string.Empty;

            if (marketName.ToUpper().Contains("TOTAL"))
                return $"o {value} u";

            if (marketName.ToUpper().Contains("HANDICAP"))
                return $"h {value} a";

            return string.Empty;
        }

        public static string FileName(string page, DateTime utcNow) => $"{page}-{utcNow:yyyyMMdd-HHmmss}.csv";

        // Header row, then one row per item numbered from 1 like the table's first column.
        private static string Build(string[] headers, IEnumerable<string?[]> rows)
        {
            var csv = new StringBuilder();
            csv.Append(string.Join(",", headers.Select(CsvField.Escape))).Append("\r\n");

            var number = 0;
            foreach (var cells in rows)
            {
                number++;
                csv.Append(number.ToString(CultureInfo.InvariantCulture));
                foreach (var cell in cells)
                    csv.Append(',').Append(CsvField.Escape(cell));
                csv.Append("\r\n");
            }

            return GlossaryCsvHelper.Bom + csv.ToString();
        }

        private static string Lines(params string?[] lines) => string.Join("\n", lines);

        private static string Format(DateTime value) => value.ToString(DateTimeFormat, CultureInfo.InvariantCulture).ToUpper();
    }
}
