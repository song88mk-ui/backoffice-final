using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MonitoringCallbackModel
    {
        public int SportId { get; set; }
        public string SearchText { get; set; }
        public string SelectedMarketTemplateId { get; set; }

    }
}
