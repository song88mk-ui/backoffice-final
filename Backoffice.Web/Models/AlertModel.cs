
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class AlertModel
    {
        public string Status { get; set; }
        public decimal Opacity { get; set; } = 0.0m;
        public string Message { get; set; }
        public string Control { get; set; }
        public bool IsAlwaysShow { get; set; } = false;
    }
}
