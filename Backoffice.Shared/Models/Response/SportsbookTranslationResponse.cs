using Backoffice.Shared.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class SportsbookTranslationResponse
    {
        public string LanguageCode { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
