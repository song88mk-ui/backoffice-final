using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientPostMarketTemplateGroupRequest
    {
        public ClientPostMarketTemplateGroupRequest()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
        }
        public bool IsAdd { get; set; }
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientMarketTemplateGroupDTO> MarketTemplateGroups { get; set; }
    }
}
