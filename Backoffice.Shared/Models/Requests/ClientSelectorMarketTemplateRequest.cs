using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientSelectorMarketTemplateRequest
    {
        public ClientSelectorMarketTemplateRequest()
        {
            Templates = new List<ClientSelectorMarketTemplateModel>();
        }

        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string UserId { get; set; }
        public List<ClientSelectorMarketTemplateModel> Templates { get; set; }

        #region Nested Class

        public class ClientSelectorMarketTemplateModel
        {
            public int SportId { get; set; }
            public string MarketTemplateId { get; set; }
            public string MarketTemplateCode { get; set; }
            public int AsiaSortOrder { get; set; }
            public int EuroSortOrder { get; set; }
            public string Identifier { get; set; }
        }

        #endregion
    }
}
