using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class MarketTemplateGroupResponse
    {
        public MarketTemplateGroupResponse()
        {
            Sports = new List<SportMarketTemplateGroup>();
        }
        public List<SportMarketTemplateGroup> Sports { get; set; }
        #region Nested Class

        public class SportMarketTemplateGroup
        {
            public SportMarketTemplateGroup()
            {
                Prematch = new List<MarketTemplateGroup>();
                Live = new List<MarketTemplateGroup>();
            }
            public int SportId { get; set; }
            public List<MarketTemplateGroup> Prematch { get; set; }
            public List<MarketTemplateGroup> Live { get; set; }
        }
        #endregion
    }
}
