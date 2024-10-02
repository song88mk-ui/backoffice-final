namespace Backoffice.Web.Models
{
    public class AnnouncementsFilterModel
    {
        public string Search { get; set; }
        public string ClientIds { get; set; }
        public string LanguageCode { get; set; }
        public bool IsAddAnnouncement { get; set; }
    }
}
