using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientMarketTemplateGroupResponse
    {
        public ClientMarketTemplateGroupResponse()
        {
            ClientMarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplateGroupDTO> ClientMarketTemplateGroups { get; set; }
    }

    public class ClientMarketTemplateGroupDTO
    {
        public int MarketTemplateGroupId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplateIds { get; set; }
        public bool IsActive { get; set; }
        public int FixtureStatusId { get; set; }
    }
}
