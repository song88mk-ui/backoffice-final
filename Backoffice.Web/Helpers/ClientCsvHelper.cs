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
                    CsvField.Escape(row.ClientId),
                    CsvField.Escape(row.ClientCode),
                    CsvField.Escape(row.ClientName),
                    CsvField.Escape(row.Website),
                    CsvField.Escape(row.Region),
                    CsvField.Escape(row.Email),
                    Num(row.MonthlyFee),
                    Num(row.RevenueShareSportsbook),
                    Num(row.RevenueShareJackpot),
                    CsvField.Escape(row.LimitId),
                    CsvField.Escape(row.Endpoint),
                    string.Empty,
                    CsvField.Escape(row.CurrencyCode),
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
                csvBuilder.AppendLine(string.Join(",", error.RowNumber.ToString(CultureInfo.InvariantCulture), CsvField.Escape(error.ClientCode), CsvField.Escape(error.Message)));
            }

            return Bom + csvBuilder.ToString();
        }

        private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
