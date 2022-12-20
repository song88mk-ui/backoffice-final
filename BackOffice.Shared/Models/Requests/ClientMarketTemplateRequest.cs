using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientMarketTemplateRequest
    {
        public ClientMarketTemplateRequest()
        {
            MarketTemplates = new List<ClientMarketTemplateModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplateModel> MarketTemplates { get; set; }
    }
}
