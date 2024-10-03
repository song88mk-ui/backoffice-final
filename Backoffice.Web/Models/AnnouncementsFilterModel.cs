namespace Backoffice.Web.Models
{
    public class AnnouncementsFilterModel
    {
        public AnnouncementsFilterModel()
        {
            UserClientCodes = new List<string>();
        }
        public string Search { get; set; }
        public string ClientCodes { get; set; }
        public string LanguageCode { get; set; }
        public bool IsAddAnnouncement { get; set; }
        public List<string> UserClientCodes { get; set; }
    }
}
