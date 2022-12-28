using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MonitoringCallbackModel
    {
        public MonitoringCallbackModel()
        {
            TopMarketTemplates = new List<MainMarketTemplateResponse>();
            Clients = new ClientModel();
        }

        public string SearchText { get; set; }
        public string SelectedMarketTemplateId { get; set; }
        public List<MainMarketTemplateResponse> TopMarketTemplates { get; set; }
        public ClientModel Clients { get; set; }

    }
}
