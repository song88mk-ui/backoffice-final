using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ImportSportsbookTranslationResponse
    {
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int UnchangedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> RowMessages { get; set; } = new List<string>();
    }
}
