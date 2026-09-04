using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ImportSportsbookTranslationRequest
    {
        public List<ImportSportsbookTranslationRow> Rows { get; set; } = new List<ImportSportsbookTranslationRow>();
    }
}
