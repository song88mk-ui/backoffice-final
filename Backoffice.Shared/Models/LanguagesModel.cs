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
                    LanguageCode = "MS",
                    LanguageName = "Malay"
                },
                new Language
                {
                    LanguageCode = "LO",
                    LanguageName = "Lao"
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
                    LanguageCode = "ZHT",
                    LanguageName = "Traditional Chinese"
                },
                new Language
                {
                    LanguageCode = "VI",
                    LanguageName = "Vietnamese"
                },
                new Language
                {
                    LanguageCode = "ID",
                    LanguageName = "Indonesian"
                },
                new Language
                {
                    LanguageCode = "KM",
                    LanguageName = "Khmer"
                },
                new Language
                {
                    LanguageCode = "JA",
                    LanguageName = "Japanese"
                },
                new Language
                {
                    LanguageCode = "SW",
                    LanguageName = "Swahili"
                }
            };
        }
        public List<Language> Languages { get; set; }
    }
}
