using Backoffice.Shared.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class TranslationResponse
    {
        public string LanguageCode { get; set; }
        public Dictionary<string, string> Translations { get; set; }
    }
}
