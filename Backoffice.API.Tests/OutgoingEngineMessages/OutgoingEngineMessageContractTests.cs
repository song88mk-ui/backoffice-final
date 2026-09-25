using Backoffice.Shared.OutgoingEngineMessages;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Backoffice.API.Tests.OutgoingEngineMessages
{
    // Companion guard to IncomingSbEngineMessageContractTests, for the other direction: this repo
    // is one of three hand-maintained copies of OnTranslationChangeMessage (platform-final,
    // odds-engine-2's OE.Shared, and this one), with no shared package between them. GM-852 was
    // exactly this: OE.Shared's TranslationChangeItem.SrcTx (added in GM-646) was never mirrored
    // here, so every announce this repo ever published had SrcTx null, silently disabling the
    // market-template wording scoping and content-addressed cache index on the odds-engine-2 side.
    //
    // These tests pin the wire names PublishTranslationChange actually serializes, so a rename or a
    // dropped property here is caught locally instead of discovered as a silent no-op three repos
    // downstream.
    public class OutgoingEngineMessageContractTests
    {
        [Fact]
        public void TranslationChangeItem_SerialisesAllFourFields()
        {
            var item = new TranslationChangeItem
            {
                StId = "MaTe:16",
                LaCo = "ZH",
                Tx = "独赢",
                SrcTx = "Match Winner"
            };

            var json = JsonSerializer.Serialize(item);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal("MaTe:16", root.GetProperty("StId").GetString());
            Assert.Equal("ZH", root.GetProperty("LaCo").GetString());
            Assert.Equal("独赢", root.GetProperty("Tx").GetString());
            Assert.Equal("Match Winner", root.GetProperty("SrcTx").GetString());
        }

        [Fact]
        public void TranslationChangeItem_SrcTx_RoundTripsThroughSystemTextJson()
        {
            var item = new TranslationChangeItem { StId = "Te:H1", LaCo = "ZH", Tx = "阿森纳", SrcTx = "Arsenal" };

            var restored = JsonSerializer.Deserialize<TranslationChangeItem>(JsonSerializer.Serialize(item));

            Assert.NotNull(restored);
            Assert.Equal("Arsenal", restored!.SrcTx);
        }

        [Fact]
        public void TranslationChangeItem_SrcTx_DefaultsToNull_SoOlderConsumersSeeNothingNew()
        {
            // Consumers written before GM-646/GM-852 must keep tolerating an absent field; this
            // guards that an unset SrcTx does not accidentally start serialising as "".
            var item = new TranslationChangeItem { StId = "SpId:1", LaCo = "ZH", Tx = "足球" };

            Assert.Null(item.SrcTx);
        }

        [Fact]
        public void OnTranslationChangeMessage_Tr_SerialisesAsAList()
        {
            var message = new OnTranslationChangeMessage
            {
                Tr = { new TranslationChangeItem { StId = "MaTe:16", LaCo = "ZH", Tx = "独赢", SrcTx = "Match Winner" } }
            };

            var json = JsonSerializer.Serialize(message);
            using var document = JsonDocument.Parse(json);

            var items = document.RootElement.GetProperty("Tr").EnumerateArray().ToList();
            Assert.Single(items);
            Assert.Equal("Match Winner", items[0].GetProperty("SrcTx").GetString());
        }
    }
}
