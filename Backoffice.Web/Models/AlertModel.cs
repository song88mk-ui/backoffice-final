
using Backoffice.Shared.Enums;
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class AlertModel
    {
        public AlertStatus Status { get; set; }
        public decimal Opacity { get; set; } = 1m;
        public string Message { get; set; }
        public bool IsAlwaysShown { get; set; } = false;
    }

    public class CancelBetAlertModel : AlertModel
    {
        public string Control { get; set; } = "";
    }

    public class AnnouncementsAlertModel : AlertModel
    {
        public AnnouncementAlertType AnnouncementAlertType { get; set; }
    }
}
