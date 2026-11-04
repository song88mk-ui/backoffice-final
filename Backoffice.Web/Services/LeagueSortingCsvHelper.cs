using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;

namespace Backoffice.Web.Services
{
    // Builds and parses the League Sorting import/export file (BO-122): one CSV per client and sport, in the
    // same dependency-free style as GlossaryCsvHelper (BOM, RFC4180, downloadFileFromText). Only the ids and
    // the sort order are read back on import; the names are in the file so people can read and edit it.
    public static class LeagueSortingCsvHelper
    {
        public static readonly string[] ExpectedHeaders = { "ClientCode", "SportId", "SegmentId", "SegmentName", "LeagueId", "LeagueName", "SortOrder" };

        // A file that is wrong everywhere would otherwise list thousands of rows.
        private const int MaxReportedErrors = 20;

        // The file of one client, its leagues in sort order. A client without leagues gets just the header, so
        // the file works as a template for its first import.
        public static string BuildCsv(string clientCode, int sportId, IEnumerable<LeagueSortingLeague> leagues)
        {
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", ExpectedHeaders));

            foreach (var league in leagues.OrderBy(l => l.SortOrder))
            {
                csv.AppendLine(string.Join(",",
                    CsvField.Escape(clientCode),
                    sportId.ToString(CultureInfo.InvariantCulture),
                    league.SegmentId.ToString(CultureInfo.InvariantCulture),
                    CsvField.Escape(league.SegmentName),
                    league.LeagueId.ToString(CultureInfo.InvariantCulture),
                    CsvField.Escape(league.LeagueName),
                    league.SortOrder.ToString(CultureInfo.InvariantCulture)));
            }

            return GlossaryCsvHelper.Bom + csv.ToString();
        }

        public static string BuildFileName(string clientCode, string sportName, DateTime utcNow)
        {
            return $"LeagueSorting-{ForFileName(clientCode)}-{ForFileName(sportName)}-{utcNow:yyyyMMdd-HHmmss}.csv";
        }

        // Reads the file for the selected client and sport. Rows are only returned when the whole file is
        // readable; a file made for another client or sport is refused rather than applied to the wrong one.
        // Row numbers are the ones a spreadsheet shows (the header is row 1).
        public static (bool Success, List<string> Errors, List<ImportOddstableLeagueRow> Rows) ParseCsv(string csvText, string clientCode, int sportId)
        {
            var errors = new List<string>();
            var rows = new List<ImportOddstableLeagueRow>();
            var lines = GlossaryCsvHelper.ParseCsvLines(csvText);

            if (lines.Count == 0)
            {
                errors.Add("The uploaded file has no data.");
                return (false, errors, rows);
            }

            // A re-saved Excel file often ends the header with empty columns.
            var header = lines[0].Select(h => h.Trim()).ToList();
            while (header.Count > 0 && string.IsNullOrEmpty(header[^1]))
                header.RemoveAt(header.Count - 1);

            if (!header.SequenceEqual(ExpectedHeaders, StringComparer.OrdinalIgnoreCase))
            {
                var found = header.Count == 0 ? "(blank)" : string.Join(", ", header);
                errors.Add($"Unexpected header row. Expected columns: {string.Join(", ", ExpectedHeaders)}. Found: {found}. Use the exported file.");
                return (false, errors, rows);
            }

            var totalErrors = 0;

            void Report(string message)
            {
                totalErrors++;

                if (errors.Count < MaxReportedErrors)
                    errors.Add(message);
            }

            for (var i = 1; i < lines.Count; i++)
            {
                var cells = lines[i];

                if (cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var rowNo = i + 1;
                var rowIsValid = true;

                var fileClientCode = Cell(cells, 0);

                if (!string.Equals(fileClientCode, clientCode, StringComparison.OrdinalIgnoreCase))
                {
                    Report($"Row {rowNo}: ClientCode '{fileClientCode}' is not the selected client ({clientCode}).");
                    rowIsValid = false;
                }

                if (!TryWholeNumber(Cell(cells, 1), out var fileSportId) || fileSportId != sportId)
                {
                    Report($"Row {rowNo}: SportId '{Cell(cells, 1)}' is not the selected sport ({sportId}).");
                    rowIsValid = false;
                }

                if (!TryWholeNumber(Cell(cells, 2), out var segmentId))
                {
                    Report($"Row {rowNo}: SegmentId must be a whole number.");
                    rowIsValid = false;
                }

                if (!TryWholeNumber(Cell(cells, 4), out var leagueId))
                {
                    Report($"Row {rowNo}: LeagueId must be a whole number.");
                    rowIsValid = false;
                }

                if (!TryWholeNumber(Cell(cells, 6), out var sortOrder))
                {
                    Report($"Row {rowNo}: SortOrder must be a whole number.");
                    rowIsValid = false;
                }

                if (rowIsValid)
                    rows.Add(new ImportOddstableLeagueRow { RowNo = rowNo, SegmentId = segmentId, LeagueId = leagueId, SortOrder = sortOrder });
            }

            if (totalErrors > errors.Count)
                errors.Add($"And {totalErrors - errors.Count} more problems.");

            if (totalErrors == 0 && rows.Count == 0)
                errors.Add("The uploaded file has no league rows.");

            return errors.Count == 0 ? (true, errors, rows) : (false, errors, new List<ImportOddstableLeagueRow>());
        }

        private static string Cell(List<string> cells, int index) => index < cells.Count ? cells[index].Trim() : string.Empty;

        private static bool TryWholeNumber(string value, out int number) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

        private static string ForFileName(string value) => Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9]+", "-").Trim('-');
    }
}
