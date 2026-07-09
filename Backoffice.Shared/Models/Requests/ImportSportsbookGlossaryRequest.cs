using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ImportSportsbookGlossaryRequest
    {
        public List<ImportSportsbookGlossaryRow> Rows { get; set; } = new List<ImportSportsbookGlossaryRow>();
    }
}
