using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backoffice.Web.Tests.SBC
{
    public class ClientCsvRoundTripTests
    {
        private const string NwsClientId = "6989B427-4234-4AC9-4791-08DEFEC92A2C";

        private static ClientExportRow MakeExportRow() => new ClientExportRow
        {
            ClientId = NwsClientId,
            ClientCode = "NWS",
            ClientName = "Northwind Sports",
            Website = "https://northwindsports.com",
            Region = "HK",
            Email = "ops@northwindsports.com",
            MonthlyFee = 500m,
            RevenueShareSportsbook = 0.1m,
            RevenueShareJackpot = 0.08m,
            LimitId = "2535",
            Endpoint = "https://operator.example.com",
            CurrencyCode = "USD",
            SoccerMinPerTicket = 1,
            SoccerMaxPerTicket = 5000,
            SoccerMaxPerGame = 10000,
            AllSportsMinPerTicket = 1,
            AllSportsMaxPerTicket = 5000,
            AllSportsMaxPerGame = 10000,
            ParlayMinPerTicket = 1,
            ParlayMaxPerTicket = 2000,
            ParlayMaxPayoutPerTicket = 50000,
            ParlayMaxWinningsBonus = 0.02m
        };

        [Fact]
        public void UneditedExport_ReimportsWithoutErrors()
        {
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow> { MakeExportRow() });

            // The export is BOM-prefixed; this is what made re-upload fail with "Missing required column(s): ClientCode".
            Assert.Equal('﻿', csv[0]);

            var result = ClientUpdateCsvParser.Parse(csv);

            Assert.Empty(result.Errors);
            var row = Assert.Single(result.Rows);
            Assert.Equal(NwsClientId, row.ClientId);
            Assert.Equal("NWS", row.ClientCode);
            Assert.Equal(500m, row.MonthlyFee);
            Assert.Equal(50000m, row.ParlayMaxPayoutPerTicket);
        }

        [Fact]
        public void BlankBetCell_ParsesAsNullNotZero()
        {
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow> { MakeExportRow() });
            // AppendLine writes Environment.NewLine — "\r\n" on Windows, "\n" on the Linux CI runner.
            var lines = csv.TrimStart('﻿').Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
            var headers = lines[0].Split(',').ToList();
            var cells = lines[1].Split(',');
            cells[headers.IndexOf("ParlayMaxPayoutPerTicket")] = string.Empty;

            var result = ClientUpdateCsvParser.Parse(lines[0] + "\n" + string.Join(",", cells));

            Assert.Empty(result.Errors);
            Assert.Null(Assert.Single(result.Rows).ParlayMaxPayoutPerTicket);
        }

        [Fact]
        public void ExportPutsClientIdFirst()
        {
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow> { MakeExportRow() });
            var lines = csv.TrimStart('﻿').Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            Assert.StartsWith("ClientId,ClientCode,", lines[0]);
            Assert.StartsWith(NwsClientId + ",NWS,", lines[1]);
        }

        [Fact]
        public void FileWithoutClientIdColumn_GetsAClearMessage()
        {
            // An export made before Client ID existed.
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow> { MakeExportRow() });
            var lines = csv.TrimStart('﻿').Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0)
                .Select(l => string.Join(",", l.Split(',').Skip(1)));

            var result = ClientUpdateCsvParser.Parse(string.Join("\n", lines));

            Assert.Contains(result.Errors, e => e.Contains("Missing the ClientId column"));
        }

        [Theory]
        [InlineData("", "Client ID is required")]
        [InlineData("NWS", "is not a valid Client ID")]
        [InlineData("6989B427-4234-4AC9", "is not a valid Client ID")]
        public void BadClientIdFormat_IsCaughtWhenParsing(string clientId, string expectedError)
        {
            var exportRow = MakeExportRow();
            exportRow.ClientId = clientId;
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow> { exportRow });

            var result = ClientUpdateCsvParser.Parse(csv);

            Assert.Contains(result.Errors, e => e.Contains(expectedError));
        }

        // The one downloadable template must work for both Import using CSV and Update via CSV.
        // Located relative to this source file, so it works whatever the build output folder is.
        private static string ReadShippedTemplate([System.Runtime.CompilerServices.CallerFilePath] string thisFile = "")
        {
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(thisFile)!, "..", ".."));
            return System.IO.File.ReadAllText(System.IO.Path.Combine(repoRoot, "Backoffice.Web", "wwwroot", "templates", "client-import-template.csv"));
        }

        [Fact]
        public void ShippedTemplate_IsAcceptedByTheCreateParser()
        {
            var result = ClientImportCsvParser.Parse(ReadShippedTemplate());

            Assert.Empty(result.Errors);
            Assert.Single(result.Rows);
        }

        [Fact]
        public void ShippedTemplate_HasEveryColumnTheUpdateParserNeeds()
        {
            var result = ClientUpdateCsvParser.Parse(ReadShippedTemplate());

            // The example row is a new client, so its Client ID is blank — that's the only expected complaint.
            Assert.DoesNotContain(result.Errors, e => e.Contains("Missing"));
            Assert.All(result.Errors, e => Assert.Contains("Client ID is required", e));
        }

        [Fact]
        public void Export_UsesExactlyTheTemplateColumns()
        {
            var templateHeader = ReadShippedTemplate().TrimStart('﻿').Split('\n')[0].TrimEnd('\r');

            Assert.Equal(templateHeader, string.Join(",", ClientCsvHelper.ExportHeaders));
        }

        [Fact]
        public void CreateParser_AlsoAcceptsBomPrefixedFile()
        {
            var csv = "﻿ClientCode,ClientName,Website,Region,Email,MonthlyFee,RevenueShareSportsbook,RevenueShareJackpot,LimitId,Endpoint,ReferenceClientCode,CurrencyCode,SoccerMinPerTicket,SoccerMaxPerTicket,SoccerMaxPerGame,AllSportsMinPerTicket,AllSportsMaxPerTicket,AllSportsMaxPerGame,ParlayMinPerTicket,ParlayMaxPerTicket,ParlayMaxPayoutPerTicket,ParlayMaxWinningsBonus\n"
                    + "NWS,Northwind Sports,https://northwindsports.com,HK,ops@northwindsports.com,500,0.1,0.08,2535,https://operator.example.com,QTE,USD,1,5000,10000,1,5000,10000,1,2000,50000,0.02";

            var result = ClientImportCsvParser.Parse(csv);

            Assert.Empty(result.Errors);
            Assert.Equal("NWS", Assert.Single(result.Rows).ClientCode);
        }
    }
}
