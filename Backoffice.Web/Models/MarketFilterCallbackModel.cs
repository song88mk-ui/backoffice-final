using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MarketFilterCallbackModel
    {
        public MarketFilterCallbackModel()
        {
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();
        }
        public SearchFormModel SearchForm { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
    }
}
