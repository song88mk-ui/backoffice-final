using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class SportSelectorMarketTemplateResponse
    {
        public SportSelectorMarketTemplateResponse()
        {
            MarketTemplates = new List<SelectorMarketTemplate>();
        }
        public int SportId { get; set; }
        public List<SelectorMarketTemplate> MarketTemplates { get; set; }

        #region Nested Class

        public class SelectorMarketTemplate
        {
            public string MarketTemplateId { get; set; }
            public string MarketTemplateCode { get; set; }
            public int SortOrder { get; set; }
        }

        #endregion
    }
}
