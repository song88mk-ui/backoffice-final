namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateSportsbookGlossaryRequest
    {
        public long? Id { get; set; }
        public string StringId { get; set; }
        public string SourceLanguage { get; set; }
        public string SourceText { get; set; }
        public string TargetLanguage { get; set; }
        public string TargetText { get; set; }
    }
}
