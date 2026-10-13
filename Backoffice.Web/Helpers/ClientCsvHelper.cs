using Backoffice.Shared.Models.Response;
using System.Globalization;
using System.Text;

namespace Backoffice.Web.Helpers
{
    // Builds the client export CSV in the same column shape as the downloadable template
    // (client-import-template.csv), which both CSV parsers accept, so an unedited export can be
    // re-imported as a no-op update. Same dependency-free
    // StringBuilder + BOM approach as GlossaryCsvHelper.
    public static class ClientCsvHelper
    {
        private const char Bom = (char)0xFEFF;

        public static readonly string[] ExportHeaders = new[]
        {
            "ClientId", "ClientCode", "ClientName", "Website", "Region", "Email", "MonthlyFee",
            "RevenueShareSportsbook", "RevenueShareJackpot", "LimitId", "Endpoint",
            // Only used when creating a client; blank in an export, ignored by the update parser.
            "ReferenceClientCode",
            "CurrencyCode", "CurrencyAction",
            "SoccerMinPerTicket", "SoccerMaxPerTicket", "SoccerMaxPerGame",
            "AllSportsMinPerTicket", "AllSportsMaxPerTicket", "AllSportsMaxPerGame",
            "ParlayMinPerTicket", "ParlayMaxPerTicket", "ParlayMaxPayoutPerTicket", "ParlayMaxWinningsBonus"
        };

        public static string BuildExportCsv(IEnumerable<ClientExportRow> rows)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(string.Join(",", ExportHeaders));

            foreach (var row in rows)
            {
                csvBuilder.AppendLine(string.Join(",",
                    EscapeCsv(row.ClientId),
                    EscapeCsv(row.ClientCode),
                    EscapeCsv(row.ClientName),
                    EscapeCsv(row.Website),
                    EscapeCsv(row.Region),
                    EscapeCsv(row.Email),
                    Num(row.MonthlyFee),
                    Num(row.RevenueShareSportsbook),
                    Num(row.RevenueShareJackpot),
                    EscapeCsv(row.LimitId),
                    EscapeCsv(row.Endpoint),
                    string.Empty,
                    EscapeCsv(row.CurrencyCode),
                    string.Empty,
                    Num(row.SoccerMinPerTicket),
                    Num(row.SoccerMaxPerTicket),
                    Num(row.SoccerMaxPerGame),
                    Num(row.AllSportsMinPerTicket),
                    Num(row.AllSportsMaxPerTicket),
                    Num(row.AllSportsMaxPerGame),
                    Num(row.ParlayMinPerTicket),
                    Num(row.ParlayMaxPerTicket),
                    Num(row.ParlayMaxPayoutPerTicket),
                    Num(row.ParlayMaxWinningsBonus)));
            }

            return Bom + csvBuilder.ToString();
        }

        public static string BuildErrorReportCsv(IEnumerable<(int RowNumber, string ClientCode, string Message)> errors)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("RowNumber,ClientCode,Error");

            foreach (var error in errors)
            {
                csvBuilder.AppendLine(string.Join(",", error.RowNumber.ToString(CultureInfo.InvariantCulture), EscapeCsv(error.ClientCode), EscapeCsv(error.Message)));
            }

            return Bom + csvBuilder.ToString();
        }

        private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
            {
                escaped = $"\"{escaped}\"";
            }

            return escaped;
        }
    }
}
