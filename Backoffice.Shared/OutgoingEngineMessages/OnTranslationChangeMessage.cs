using System.Collections.Generic;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    // Payload-carrying translation change published after a Dynamic Translation (glossary) edit.
    // Wire-compatible with Platform.Shared's OnTranslationChangeMessage (platform-final) and
    // OE.Shared's equivalent (odds-engine-2). Routed to r.oe.cachesync.ontranslationchange, where
    // Platform.CacheSync.Service purges the Sportsbook.API translation cache per changed key and
    // relays the deltas on r.oe.ontranslationchange to OE.Adapter.Service (in-memory enrichment
    // cache) and OE.Translation.Service (Mongo *NaTr persistence + translation-only odds pushes).
    public class OnTranslationChangeMessage
    {
        public List<TranslationChangeItem> Tr { get; set; } = new List<TranslationChangeItem>();
    }

    public class TranslationChangeItem
    {
        // TranslationCache2.STRING_ID — glossary key formats:
        // "SpId:{id}" | "SegId:{id}" | "LeId:{id}" | "Te:{teamId}" | "MaTe:{MaTeId}" | "SeTe:{MaTeId}:{SeTeId}"
        public string StId { get; set; }
        public string LaCo { get; set; } // target language code, e.g. "ZH" | "ES" | "FR"
        public string Tx { get; set; }   // translated text; empty string clears the translation

        // The row's SOURCE_TEXT (2026-09, GM-852; may be null from senders that have not been
        // updated yet). Lets OE.Adapter/OE.Translation.Service keep their content-addressed index
        // current without a restart, and lets the Mongo change path scope a market-template
        // wording update to exactly the markets whose current name carries that wording. Wire name
        // matches OE.Shared's TranslationChangeItem.SrcTx.
        public string SrcTx { get; set; }
    }
}
