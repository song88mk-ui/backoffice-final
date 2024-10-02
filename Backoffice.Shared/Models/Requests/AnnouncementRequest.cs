using Backoffice.Shared.Enums;

namespace Backoffice.Shared.Models.Requests
{
    public class AnnouncementRequest
    {
        public LanguageCode LanguageCode { get; set; }
        public string ClientIds { get; set; }
        public string SearchText { get; set; }
    }
}
