namespace Backoffice.Shared.Models.Requests
{
    public class AddOrUpdateSportsbookTranslationRequest
    {
        public string LanguageCode { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
