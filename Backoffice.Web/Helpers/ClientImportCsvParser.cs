using Backoffice.Shared.Models.Requests;
using System.Globalization;

namespace Backoffice.Web.Helpers
{
    public static class ClientImportCsvParser
    {
        private static readonly string[] ExpectedColumns = new[]
        {
            "ClientCode", "ClientName", "Website", "Region", "Email", "MonthlyFee",
            "RevenueShareSportsbook", "RevenueShareJackpot", "LimitId", "Endpoint", "ReferenceClientCode",
            "CurrencyCode", "SoccerMinPerTicket", "SoccerMaxPerTicket", "SoccerMaxPerGame",
            "AllSportsMinPerTicket", "AllSportsMaxPerTicket", "AllSportsMaxPerGame",
            "ParlayMinPerTicket", "ParlayMaxPerTicket", "ParlayMaxPayoutPerTicket", "ParlayMaxWinningsBonus"
        };

        // A blank limit would be saved as 0, not as the standard limits, so a new client must have every one.
        // The template (Download CSV) carries the standard limits to keep unless the client asked for others.
        private static readonly string[] LimitColumns = new[]
        {
            "SoccerMinPerTicket", "SoccerMaxPerTicket", "SoccerMaxPerGame",
            "AllSportsMinPerTicket", "AllSportsMaxPerTicket", "AllSportsMaxPerGame",
            "ParlayMinPerTicket", "ParlayMaxPerTicket", "ParlayMaxPayoutPerTicket", "ParlayMaxWinningsBonus"
        };

        public class ParseResult
        {
            public List<ClientImportRowRequest> Rows { get; set; } = new();
            public List<string> Errors { get; set; } = new();
        }

        public static ParseResult Parse(string csvText)
        {
            var result = new ParseResult();

            // A file saved by Excel or our own export may start with a UTF-8 BOM.
            var lines = SplitLines((csvText ?? string.Empty).TrimStart('﻿'));
            if (lines.Count == 0)
            {
                result.Errors.Add("The file is empty.");
                return result;
            }

            var header = SplitLine(lines[0]);
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Count; i++)
                columnIndex[header[i].Trim()] = i;

            var missing = ExpectedColumns.Where(c => !columnIndex.ContainsKey(c)).ToList();
            if (missing.Any())
            {
                result.Errors.Add($"Missing required column(s): {string.Join(", ", missing)}. Download the template to see the expected format.");
                return result;
            }

            for (var lineNumber = 1; lineNumber < lines.Count; lineNumber++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineNumber]))
                    continue;

                var fields = SplitLine(lines[lineNumber]);
                var rowNumber = lineNumber + 1;

                string Get(string column)
                {
                    var idx = columnIndex[column];
                    return idx < fields.Count ? CsvField.RemoveFormulaMarker(fields[idx].Trim()) : string.Empty;
                }

                decimal GetDecimal(string column)
                {
                    var raw = Get(column);

                    if (string.IsNullOrWhiteSpace(raw))
                        return 0m;

                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                        return value;

                    result.Errors.Add($"Row {rowNumber}: \"{raw}\" in column {column} is not a valid number.");
                    return 0m;
                }

                // ClientId is optional on create (blank = Platform generates one), and older files have no such column.
                var clientIdRaw = columnIndex.ContainsKey("ClientId") ? Get("ClientId") : string.Empty;
                if (!string.IsNullOrWhiteSpace(clientIdRaw) && (!Guid.TryParse(clientIdRaw, out var parsedClientId) || parsedClientId == Guid.Empty))
                {
                    result.Errors.Add($"Row {rowNumber}: \"{clientIdRaw}\" is not a valid Client ID. Leave it blank to have one generated, or use the format 6989B427-4234-4AC9-4791-08DEFEC92A2C.");
                }

                var currencyRaw = Get("CurrencyCode");
                if (string.IsNullOrWhiteSpace(currencyRaw))
                {
                    result.Errors.Add($"Row {rowNumber}: CurrencyCode is required.");
                }
                else if (!Enum.TryParse<Backoffice.Shared.Enums.Currency>(currencyRaw, true, out _))
                {
                    result.Errors.Add($"Row {rowNumber}: \"{currencyRaw}\" is not a recognized currency code.");
                }

                var blankLimits = LimitColumns.Where(c => string.IsNullOrWhiteSpace(Get(c))).ToList();
                if (blankLimits.Count > 0)
                {
                    result.Errors.Add($"Row {rowNumber}: every limit column is required. Blank: {string.Join(", ", blankLimits)}. Use the standard limits from Download CSV unless the client asked for different ones.");
                }

                result.Rows.Add(new ClientImportRowRequest
                {
                    ClientId = clientIdRaw,
                    RowNumber = rowNumber,
                    ClientCode = Get("ClientCode"),
                    ClientName = Get("ClientName"),
                    Website = Get("Website"),
                    Region = Get("Region"),
                    Email = Get("Email"),
                    MonthlyFee = GetDecimal("MonthlyFee"),
                    RevenueShareSportsbook = GetDecimal("RevenueShareSportsbook"),
                    RevenueShareJackpot = GetDecimal("RevenueShareJackpot"),
                    LimitId = Get("LimitId"),
                    Endpoint = Get("Endpoint"),
                    ReferenceClientCode = Get("ReferenceClientCode"),
                    CurrencyCode = Get("CurrencyCode"),
                    SoccerMinPerTicket = GetDecimal("SoccerMinPerTicket"),
                    SoccerMaxPerTicket = GetDecimal("SoccerMaxPerTicket"),
                    SoccerMaxPerGame = GetDecimal("SoccerMaxPerGame"),
                    AllSportsMinPerTicket = GetDecimal("AllSportsMinPerTicket"),
                    AllSportsMaxPerTicket = GetDecimal("AllSportsMaxPerTicket"),
                    AllSportsMaxPerGame = GetDecimal("AllSportsMaxPerGame"),
                    ParlayMinPerTicket = GetDecimal("ParlayMinPerTicket"),
                    ParlayMaxPerTicket = GetDecimal("ParlayMaxPerTicket"),
                    ParlayMaxPayoutPerTicket = GetDecimal("ParlayMaxPayoutPerTicket"),
                    ParlayMaxWinningsBonus = GetDecimal("ParlayMaxWinningsBonus")
                });
            }

            return result;
        }

        private static List<string> SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n")
                .Split('\n')
                .ToList();
        }

        private static List<string> SplitLine(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                        inQuotes = true;
                    else if (c == ',')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}
