using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

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
