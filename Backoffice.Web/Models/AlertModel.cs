
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class AlertModel
    {
        public string AltertMessage { get; set; } = string.Empty;
        public string AlertStatus { get; set; } = string.Empty;
        public decimal Opacity { get; set; } = 0.0m;
    }
}
