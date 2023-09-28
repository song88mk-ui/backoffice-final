using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class LanguagesModel
    {
        public LanguagesModel()
        {
            Languages = new List<Language>
            {
                new Language
                {
                    LanguageCode = "TH",
                    LanguageName = "Thai"
                },
                new Language
                {
                    LanguageCode = "KO",
                    LanguageName = "Korean"
                },
                new Language
                {
                    LanguageCode = "ZH",
                    LanguageName = "Simplified Chinese"
                },
                new Language
                {
                    LanguageCode = "VI",
                    LanguageName = "Vietnamese"
                },
                new Language
                {
                    LanguageCode = "ID",
                    LanguageName = "Bahasa Indonesia"
                },
                new Language
                {
                    LanguageCode = "JA",
                    LanguageName = "Japanese"
                },
                new Language
                {
                    LanguageCode = "PT",
                    LanguageName = "Portuguese"
                },
                new Language
                {
                    LanguageCode = "ES",
                    LanguageName = "Spanish"
                }
            };
        }
        public List<Language> Languages { get; set; }
    }
}
