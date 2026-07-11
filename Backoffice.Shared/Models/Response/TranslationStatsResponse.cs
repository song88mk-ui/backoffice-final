using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class TranslationStatsResponse
    {
        public List<LanguageTranslationStat> Languages { get; set; } = new List<LanguageTranslationStat>();
    }

    public class LanguageTranslationStat
    {
        public string LanguageCode { get; set; }
        public int TotalCount { get; set; }
        public int TranslatedCount { get; set; }
        public int UntranslatedCount => TotalCount - TranslatedCount;
    }
}
