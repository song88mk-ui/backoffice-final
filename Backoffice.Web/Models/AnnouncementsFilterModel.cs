namespace Backoffice.Web.Models
{
    public class AnnouncementsFilterModel
    {
        public AnnouncementsFilterModel()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();
        }
        public string Search { get; set; }
        public string ClientCodes { get; set; }
        public string LanguageCode { get; set; }
        public bool IsAddAnnouncement { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
    }
}
