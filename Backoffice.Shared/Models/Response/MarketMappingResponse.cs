using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class MarketMappingResponse
    {
        public MarketMappingResponse()
        {
            MarketMapping = new List<MarketMappingModel>();
        }
        public List<MarketMappingModel> MarketMapping { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

    public class MarketMappingModel
    {
        public string MaTeId { get; set; }
        public string MaTeNa { get; set; }
        public DateTime Ti { get; set; }
        public ProviderModel<List<string>> Info { get; set; }
    }
}
