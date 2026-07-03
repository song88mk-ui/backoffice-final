namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateGlossaryTranslationRequest
    {
        public string StringId { get; set; }
        public string TargetLanguage { get; set; }
        public string TargetText { get; set; }
    }
}
