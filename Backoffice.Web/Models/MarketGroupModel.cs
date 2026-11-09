using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class MarketGroupModel
    {
        public MarketGroupModel()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
        }
        public string ClientCode { get; set; }
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int FixtureStatusId { get; set; }
        public bool IsUpdate { get; set; }
        public List<ClientMarketTemplateGroupDTO> MarketTemplateGroups { get; set; }
        public List<ClientMarketTemplateGroupDTO> OldMarketTemplateGroups { get; set; }
    }

    public class MarketGroupOrder
    {
        public int MarketTemplateGroupId { get; set; }
        public int SortOrder { get; set; }
    }
}
