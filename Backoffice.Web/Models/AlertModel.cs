
using System.Security.Claims;

namespace Backoffice.Web.Models
{
    public class AlertModel
    {
        public string ErrorMessage { get; set; }
        public string AlertStatus { get; set; }
        public decimal Opacity { get; set; } = 0.0m;
        public string AltertMessage { get; set; }
        public bool ShowErrorMessage { get; set; } = false;
    }
}
