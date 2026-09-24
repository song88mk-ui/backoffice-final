using Backoffice.Shared.Models.Response;
using Backoffice.Web.Services;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class GlossaryCsvHelperTests
    {
        [Fact]
        public void BuildCsv_ThenParseCsv_RoundTripsPlainValues()
        {
            var csv = GlossaryCsvHelper.BuildCsv(new[]
            {
                new SportsbookGlossaryExportRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "足球", TargetTextEs = "Fútbol", TargetTextFr = "Football" }
            });

            var (success, error, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Null(error);
            var row = Assert.Single(rows);
            Assert.Equal("SpId:1", row.StringId);
            Assert.Equal("Soccer", row.SourceText);
            Assert.Equal("足球", row.TargetTextZh);
            Assert.Equal("Fútbol", row.TargetTextEs);
            Assert.Equal("Football", row.TargetTextFr);
        }

        [Fact]
        public void BuildCsv_PrependsUtf8Bom()
        {
            var csv = GlossaryCsvHelper.BuildCsv(new SportsbookGlossaryExportRow[0]);

            Assert.Equal((char)0xFEFF, csv[0]);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedComma_InQuotedField()
        {
            var csv = "StringId,EN,ZH,ES,FR\r\nSpId:1,\"Hello, World\",,,\r\n";

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Hello, World", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedQuote_ViaDoubling()
        {
            var csv = "StringId,EN,ZH,ES,FR\r\nSpId:1,\"Say \"\"Hi\"\"\",,,\r\n";

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Say \"Hi\"", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_HandlesEmbeddedNewline_InQuotedField()
        {
            var csv = "StringId,EN,ZH,ES,FR\r\nSpId:1,\"Line1\nLine2\",,,\r\n";

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Line1\nLine2", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_RoundTrips_ValueWithCommaQuoteAndNewlineTogether()
        {
            var csv = GlossaryCsvHelper.BuildCsv(new[]
            {
                new SportsbookGlossaryExportRow { StringId = "SpId:1", SourceText = "Hello, \"World\"\nFoo", TargetTextZh = "", TargetTextEs = "", TargetTextFr = "" }
            });

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("Hello, \"World\"\nFoo", Assert.Single(rows).SourceText);
        }

        [Fact]
        public void ParseCsv_RejectsMismatchedHeader()
        {
            var csv = "StringId,English,ZH,ES,FR\r\nSpId:1,Soccer,,,\r\n";

            var (success, error, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.False(success);
            Assert.NotNull(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_RejectsMismatchedHeader_AndReportsWhatWasFound()
        {
            var csv = "StringId,English,ZH,ES,FR\r\nSpId:1,Soccer,,,\r\n";

            var (success, error, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.False(success);
            Assert.Contains("Found: StringId, English, ZH, ES, FR", error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_AcceptsHeader_WithTrailingBlankColumn()
        {
            // Excel often appends a trailing empty column (a trailing comma) when re-saving --
            // the exported template should still be re-importable after that round trip.
            var csv = "StringId,EN,ZH,ES,FR,\r\nSpId:1,Soccer,,,,\r\n";

            var (success, error, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("SpId:1", Assert.Single(rows).StringId);
        }

        [Fact]
        public void ParseCsv_ReturnsError_WhenFileEmpty()
        {
            var (success, error, rows) = GlossaryCsvHelper.ParseCsv(string.Empty);

            Assert.False(success);
            Assert.NotNull(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_ReturnsEmptyRows_WhenOnlyHeaderPresent()
        {
            var (success, error, rows) = GlossaryCsvHelper.ParseCsv("StringId,EN,ZH,ES,FR\r\n");

            Assert.True(success);
            Assert.Null(error);
            Assert.Empty(rows);
        }

        [Fact]
        public void ParseCsv_SkipsBlankLines()
        {
            var csv = "StringId,EN,ZH,ES,FR\r\nSpId:1,Soccer,,,\r\n\r\nSpId:2,Basketball,,,\r\n";

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal(2, rows.Count);
            Assert.Equal("SpId:1", rows[0].StringId);
            Assert.Equal("SpId:2", rows[1].StringId);
        }

        [Fact]
        public void ParseCsv_IgnoresLeadingBom()
        {
            var csv = (char)0xFEFF + "StringId,EN,ZH,ES,FR\r\nSpId:1,Soccer,,,\r\n";

            var (success, _, rows) = GlossaryCsvHelper.ParseCsv(csv);

            Assert.True(success);
            Assert.Equal("SpId:1", Assert.Single(rows).StringId);
        }
    }
}
