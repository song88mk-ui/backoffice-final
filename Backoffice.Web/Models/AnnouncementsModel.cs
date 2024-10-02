using static Backoffice.Shared.Models.Response.AnnouncementResponse;

namespace Backoffice.Web.Models
{
    public class AnnouncementsModel
    {
        public AnnouncementsModel()
        {
            Announcements = new List<AnnouncementDTO>();
        }
        public List<AnnouncementDTO> Announcements { get; set; }

    }
}
