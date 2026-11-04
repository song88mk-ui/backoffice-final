using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using System.Globalization;

namespace Backoffice.Web.Helpers
{
    public static class ClientUpdateCsvParser
    {
        private static readonly string[] ExpectedColumns = new[]
        {
            "ClientId", "ClientCode", "ClientName", "Website", "Region", "Email", "MonthlyFee",
            "RevenueShareSportsbook", "RevenueShareJackpot", "LimitId", "Endpoint",
            "CurrencyCode", "CurrencyAction",
            "SoccerMinPerTicket", "SoccerMaxPerTicket", "SoccerMaxPerGame",
            "AllSportsMinPerTicket", "AllSportsMaxPerTicket", "AllSportsMaxPerGame",
            "ParlayMinPerTicket", "ParlayMaxPerTicket", "ParlayMaxPayoutPerTicket", "ParlayMaxWinningsBonus"
        };

        public class ParseResult
        {
            public List<ClientUpdateRowRequest> Rows { get; set; } = new();
            public List<string> Errors { get; set; } = new();
        }

        public static ParseResult Parse(string csvText)
        {
            var result = new ParseResult();

            // Export CSVs are written with a UTF-8 BOM; strip it or the first header becomes "﻿ClientCode".
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
            if (missing.Count == 1 && missing[0] == "ClientId")
            {
                result.Errors.Add("Missing the ClientId column. Clients are now identified by Client ID — download a fresh Export CSV or the update template.");
                return result;
            }
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

                // Null = blank cell, distinct from an explicit 0.
                decimal? GetNullableDecimal(string column)
                {
                    var raw = Get(column);

                    if (string.IsNullOrWhiteSpace(raw))
                        return null;

                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                        return value;

                    result.Errors.Add($"Row {rowNumber}: \"{raw}\" in column {column} is not a valid number.");
                    return null;
                }

                // Format only here; whether the Client ID exists and matches the Client Code is checked server-side.
                var clientIdRaw = Get("ClientId");
                if (string.IsNullOrWhiteSpace(clientIdRaw))
                {
                    result.Errors.Add($"Row {rowNumber}: Client ID is required.");
                }
                else if (!Guid.TryParse(clientIdRaw, out _))
                {
                    result.Errors.Add($"Row {rowNumber}: \"{clientIdRaw}\" is not a valid Client ID. It should look like 6989B427-4234-4AC9-4791-08DEFEC92A2C.");
                }

                // Unlike the create-flow CSV, CurrencyCode is optional here — a row with no
                // currency just updates the client's top-level fields and touches no currency.
                var currencyRaw = Get("CurrencyCode");
                if (!string.IsNullOrWhiteSpace(currencyRaw) && !Enum.TryParse<Currency>(currencyRaw, true, out _))
                {
                    result.Errors.Add($"Row {rowNumber}: \"{currencyRaw}\" is not a recognized currency code.");
                }

                var actionRaw = Get("CurrencyAction");
                if (!string.IsNullOrWhiteSpace(actionRaw) && !string.Equals(actionRaw, ClientCurrencyAction.Remove, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"Row {rowNumber}: CurrencyAction must be blank or \"Remove\".");
                }

                result.Rows.Add(new ClientUpdateRowRequest
                {
                    RowNumber = rowNumber,
                    ClientId = clientIdRaw,
                    ClientCode = Get("ClientCode"),
                    ClientName = Get("ClientName"),
                    Website = Get("Website"),
                    Region = Get("Region"),
                    Email = Get("Email"),
                    MonthlyFee = GetNullableDecimal("MonthlyFee"),
                    RevenueShareSportsbook = GetNullableDecimal("RevenueShareSportsbook"),
                    RevenueShareJackpot = GetNullableDecimal("RevenueShareJackpot"),
                    LimitId = Get("LimitId"),
                    Endpoint = Get("Endpoint"),
                    CurrencyCode = currencyRaw,
                    CurrencyAction = actionRaw,
                    SoccerMinPerTicket = GetNullableDecimal("SoccerMinPerTicket"),
                    SoccerMaxPerTicket = GetNullableDecimal("SoccerMaxPerTicket"),
                    SoccerMaxPerGame = GetNullableDecimal("SoccerMaxPerGame"),
                    AllSportsMinPerTicket = GetNullableDecimal("AllSportsMinPerTicket"),
                    AllSportsMaxPerTicket = GetNullableDecimal("AllSportsMaxPerTicket"),
                    AllSportsMaxPerGame = GetNullableDecimal("AllSportsMaxPerGame"),
                    ParlayMinPerTicket = GetNullableDecimal("ParlayMinPerTicket"),
                    ParlayMaxPerTicket = GetNullableDecimal("ParlayMaxPerTicket"),
                    ParlayMaxPayoutPerTicket = GetNullableDecimal("ParlayMaxPayoutPerTicket"),
                    ParlayMaxWinningsBonus = GetNullableDecimal("ParlayMaxWinningsBonus")
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
