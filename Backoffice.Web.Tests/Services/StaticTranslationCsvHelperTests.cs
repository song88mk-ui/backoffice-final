using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class StaticTranslationCsvHelperTests
    {
        [Fact]
        public void BuildCsv_ThenParseCsv_RoundTripsPlainValues()
        {
            var csv = StaticTranslationCsvHelper.BuildCsv(new[]
            {
                new SportsbookTranslationExportRow { Key = "accept", SourceText = "Accept", TargetTextZh = "接受", TargetTextJa = "承認", TargetTextBr = "Aceitar" }
            });

            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Null(error);
            var row = Assert.Single(rows);
            Assert.Equal("accept", row.Key);
            Assert.Equal("Accept", row.SourceText);
            Assert.Equal("接受", row.TargetTextZh);
            Assert.Equal("承認", row.TargetTextJa);
            Assert.Equal("Aceitar", row.TargetTextBr);
        }

        [Fact]
        public void BuildCsv_PrependsUtf8Bom()
        {
            var csv = StaticTranslationCsvHelper.BuildCsv(new SportsbookTranslationExportRow[0]);

            Assert.Equal((char)0xFEFF, csv[0]);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedComma_InQuotedField()
        {
            var csv = "Key,EN,ZH,JA,BR\r\naccept,\"Hello, World\",,,\r\n";

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Hello, World", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedQuote_ViaDoubling()
        {
            var csv = "Key,EN,ZH,JA,BR\r\naccept,\"Say \"\"Hi\"\"\",,,\r\n";

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Say \"Hi\"", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedNewline_InQuotedField()
        {
            var csv = "Key,EN,ZH,JA,BR\r\naccept,\"Line1\nLine2\",,,\r\n";

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Line1\nLine2", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_RoundTrips_ValueWithCommaQuoteAndNewlineTogether()
        {
            var csv = StaticTranslationCsvHelper.BuildCsv(new[]
            {
                new SportsbookTranslationExportRow { Key = "accept", SourceText = "Hello, \"World\"\nFoo", TargetTextZh = "", TargetTextJa = "", TargetTextBr = "" }
            });

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Hello, \"World\"\nFoo", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_RejectsMismatchedHeader()
        {
            var csv = "Key,English,ZH,JA,BR\r\naccept,Accept,,,\r\n";

            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.False(success);
            Assert.NotNull(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_RejectsMismatchedHeader_AndReportsWhatWasFound()
        {
            var csv = "Key,English,ZH,JA,BR\r\naccept,Accept,,,\r\n";

            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.False(success);
            Assert.Contains("Found: Key, English, ZH, JA, BR", error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_AcceptsHeader_WithTrailingBlankColumn()
        {
            var csv = "Key,EN,ZH,JA,BR,\r\naccept,Accept,,,,\r\n";

            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("accept", Assert.Single(rows).Key);
        }

        [Fact]
        public void ParseCsv_ReturnsError_WhenFileEmpty()
        {
            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv(string.Empty);

            Assert.False(success);
            Assert.NotNull(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_ReturnsEmptyRows_WhenOnlyHeaderPresent()
        {
            var (success, error, rows) = StaticTranslationCsvHelper.ParseCsv("Key,EN,ZH,JA,BR\r\n");

            Assert.True(success);
            Assert.Null(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_SkipsBlankLines()
        {
            var csv = "Key,EN,ZH,JA,BR\r\naccept,Accept,,,\r\n\r\nreject,Reject,,,\r\n";

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal(2, rows.Count);
            Assert.Equal("accept", rows[0].Key);
            Assert.Equal("reject", rows[1].Key);
        }

        [Fact]
        public void ParseCsv_IgnoresLeadingBom()
        {
            var csv = (char)0xFEFF + "Key,EN,ZH,JA,BR\r\naccept,Accept,,,\r\n";

            var (success, _, rows) = StaticTranslationCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("accept", Assert.Single(rows).Key);
        }
    }
}
