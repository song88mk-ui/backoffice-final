namespace Backoffice.Shared.Utilities
{
    public static class TranslationTextHelper
    {
        /// <summary>
        /// A glossary term whose source text contains no letters (e.g. "5+", "0-1", "50%")
        /// reads the same in every language, so it is never considered untranslated.
        /// Checks a-z/A-Z only, mirroring the EF-translatable filter in BackofficeRepository.
        /// </summary>
        public static bool RequiresTranslation(string sourceText)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                return false;
            }

            foreach (var c in sourceText)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
