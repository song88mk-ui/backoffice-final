using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class MarketLookupResponse
    {
        public MarketLookupResponse()
        {
            MarketLookup = new List<MarketLookupModel>();
        }

        public List<MarketLookupModel> MarketLookup { get; set; }
    }

    public class MarketLookupModel
    {
        public string MarketTemplateId { get; set; }
        public string MarketTemplateName { get; set; }
        public bool IsActive { get; set; }
    }
}
