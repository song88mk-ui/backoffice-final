
using Backoffice.Shared.Models.Response;

namespace Backoffice.Shared.Models
{
    public class MarketGroupNavigationModel
    {
        public bool IsSelected { get; set; }
        public MarketTemplateGroupResponse.MarketTemplateGroup MarketTemplateGroup { get; set; }
    }
}
