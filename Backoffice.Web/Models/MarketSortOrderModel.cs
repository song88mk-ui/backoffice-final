namespace Backoffice.Web.Models
{
    public class MarketSortOrderModel
    {
        public MarketSortOrderModel()
        {
            MarketTemplates = new List<ClientMarketTemplate>();
            MarketTemplateGroups = new List<ClientMarketTemplateGroup>();
        }

        public string ClientCode { get; set; }
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplate> MarketTemplates { get; set; }
        public List<ClientMarketTemplateGroup> MarketTemplateGroups { get; set; }

        // for cancelling changes this can be used when reverting to the original sort order and group names assigned
        public List<ClientMarketTemplate> OriginalMarketTemplates { get; set; } 

        #region Nested Class

        public class ClientMarketTemplate
        {
            public List<string> MarketTemplateGroupNames { get; set; }
            public string MarketTemplateId { get; set; }
            public string MarketTemplateName { get; set; }
            public int SortOrder { get; set; }
        }

        public class ClientMarketTemplateGroup
        {
            public int MarketTemplateGroupId { get; set; }
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public List<string> MarketTemplateIds { get; set; }
            public bool IsSelected { get; set; }
            public bool IsActive { get; set; }
        }

        #endregion
    }


}
