using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using System.Collections.Generic;
using Xunit;

namespace Backoffice.Web.Tests.Common
{
    // BO-137: exported cells that Excel would run as a formula get a leading ', and every parser removes
    // exactly that marker again so an unedited export imports back unchanged.
    public class CsvFieldTests
    {
        public static IEnumerable<object[]> RoundTripValues => new[]
        {
            new object[] { "=1+1" },
            new object[] { "-5" },
            new object[] { "+3" },
            new object[] { "@x" },
            new object[] { "\tTabbed" },
            new object[] { "\r\nStarts with a line break" },
            new object[] { "=HYPERLINK(\"http://example.com\",\"Click, here\")" },
            new object[] { "'=already marked by the user" },
            new object[] { "''-5" },
            new object[] { "'5" },
            new object[] { "'" },
            new object[] { "Plain text" },
            new object[] { "Text with, a comma and \"quotes\"" },
            new object[] { "Line one\nline two" },
            new object[] { "1-2 draw" },
            new object[] { "a=b" }
        };

        [Theory]
        [InlineData("=1+1", "'=1+1")]
        [InlineData("-5", "'-5")]
        [InlineData("+3", "'+3")]
        [InlineData("@x", "'@x")]
        [InlineData("\tx", "'\tx")]
        [InlineData("\rx", "\"'\rx\"")]
        [InlineData("=SUM(A1,A2)", "\"'=SUM(A1,A2)\"")]
        [InlineData("'=x", "''=x")]
        public void Escape_MarksCellsExcelWouldRunAsAFormula(string value, string expected)
        {
            Assert.Equal(expected, CsvField.Escape(value));
        }

        [Theory]
        [InlineData("Plain text", "Plain text")]
        [InlineData("Hello, World", "\"Hello, World\"")]
        [InlineData("Say \"hi\"", "\"Say \"\"hi\"\"\"")]
        [InlineData("Line one\nline two", "\"Line one\nline two\"")]
        [InlineData("1-2 draw", "1-2 draw")]
        [InlineData("'5", "'5")]
        [InlineData(" =1+1", " =1+1")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Escape_LeavesOrdinaryTextAsBefore(string? value, string expected)
        {
            Assert.Equal(expected, CsvField.Escape(value));
        }

        [Theory]
        [InlineData("'=1+1", "=1+1")]
        [InlineData("''=x", "'=x")]
        [InlineData("'5", "'5")]
        [InlineData("'", "'")]
        [InlineData("-5", "-5")]
        [InlineData("+63 912 345 6789", "+63 912 345 6789")]
        public void RemoveFormulaMarker_RemovesOnlyTheExportMarker(string cell, string expected)
        {
            Assert.Equal(expected, CsvField.RemoveFormulaMarker(cell));
        }

        [Theory]
        [MemberData(nameof(RoundTripValues))]
        public void StaticTranslation_ExportThenImport_GivesBackTheOriginalText(string value)
        {
            var csv = StaticTranslationCsvHelper.BuildCsv(new[]
            {
                new SportsbookTranslationExportRow { Key = "key", SourceText = value, TargetTextZh = value, TargetTextJa = "", TargetTextBr = value }
            });

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            var row = Assert.Single(rows);
            Assert.Equal("key", row.Key);
            Assert.Equal(value, row.SourceText);
            Assert.Equal(value, row.TargetTextZh);
            Assert.Equal(string.Empty, row.TargetTextJa);
            Assert.Equal(value, row.TargetTextBr);
        }

        [Theory]
        [MemberData(nameof(RoundTripValues))]
        public void DynamicTranslation_ExportThenImport_GivesBackTheOriginalText(string value)
        {
            var csv = GlossaryCsvHelper.BuildCsv(new[]
            {
                new SportsbookGlossaryExportRow { StringId = "42", SourceText = value, TargetTextZh = value, TargetTextEs = value, TargetTextFr = "" }
            });

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            var row = Assert.Single(rows);
            Assert.Equal("42", row.StringId);
            Assert.Equal(value, row.SourceText);
            Assert.Equal(value, row.TargetTextZh);
            Assert.Equal(value, row.TargetTextEs);
        }

        [Theory]
        [InlineData("=1+1")]
        [InlineData("-5")]
        [InlineData("+3")]
        [InlineData("@x")]
        [InlineData("'=x")]
        [InlineData("Northwind, \"Sports\"")]
        public void ClientExport_ThenUpdateImport_GivesBackTheOriginalName(string clientName)
        {
            var csv = ClientCsvHelper.BuildExportCsv(new List<ClientExportRow>
            {
                new ClientExportRow { ClientId = "6989B427-4234-4AC9-4791-08DEFEC92A2C", ClientCode = "NWS", ClientName = clientName, CurrencyCode = "USD", MonthlyFee = -5m }
            });

            Assert.Contains(CsvField.Escape(clientName), csv);

            var result = ClientUpdateCsvParser.Parse(csv);

            Assert.Empty(result.Errors);
            var row = Assert.Single(result.Rows);
            Assert.Equal(clientName, row.ClientName);
            Assert.Equal("NWS", row.ClientCode);
            // Number columns are written as numbers, not text, so a negative value is not marked.
            Assert.Equal(-5m, row.MonthlyFee);
        }

        [Fact]
        public void ClientUpdateErrorReport_MarksAFormulaInTheClientCode()
        {
            var csv = ClientCsvHelper.BuildErrorReportCsv(new[] { (2, "=cmd", "Unknown client") });

            Assert.Contains("2,'=cmd,Unknown client", csv);
        }

        [Fact]
        public void LeagueSortingExport_MarksAFormulaInTheNames_AndStillImports()
        {
            var csv = LeagueSortingCsvHelper.BuildCsv("QTE-A", 1, new[]
            {
                new LeagueSortingLeague { SegmentId = 10, SegmentName = "@England", LeagueId = 100, LeagueName = "=1+1", SortOrder = 1 }
            });

            Assert.Contains("QTE-A,1,10,'@England,100,'=1+1,1", csv);

            var (success, _, rows) = LeagueSortingCsvHelper.ParseCsv(csv, "QTE-A", 1);

            Assert.True(success);
            Assert.Single(rows);
        }
    }
}
