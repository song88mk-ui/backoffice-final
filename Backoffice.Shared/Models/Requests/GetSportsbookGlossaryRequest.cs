namespace Backoffice.Shared.Models.Requests
{
    public class GetSportsbookGlossaryRequest
    {
        public string SearchText { get; set; }
        public bool ExactMatch { get; set; }
        public string TargetLanguage { get; set; }
        public MetaData MetaData { get; set; }
        public bool UntranslatedOnly { get; set; }
    }
}
