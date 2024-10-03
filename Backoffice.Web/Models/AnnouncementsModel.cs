using static Backoffice.Shared.Models.Response.AnnouncementResponse;

namespace Backoffice.Web.Models
{
    public class AnnouncementsModel
    {
        public AnnouncementsModel()
        {
            Announcements = new List<Announcements>();
        }
        public List<Announcements> Announcements { get; set; }

    }

    public class Announcements : AnnouncementDTO
    {
        public int Row { get; set; }
    }
}
