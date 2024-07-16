using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientPostMarketTemplateGroupResponse
    {
        public ClientPostMarketTemplateGroupResponse()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientMarketTemplateGroupDTO> MarketTemplateGroups { get; set; }
    }
}
