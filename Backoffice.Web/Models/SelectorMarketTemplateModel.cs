namespace Backoffice.Web.Models
{
    public class ClientSelectorMarketTemplateModel
    {
        public Guid RowKey { get; } = Guid.NewGuid();
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateCode { get; set; }
        public int AsiaSortOrder { get; set; }
        public int EuroSortOrder { get; set; }
        public string Identifier { get; set; }
    }

    public class SelectorMarketTemplateLookupModel
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateCode { get; set; }
        public int SortOrder { get; set; }
    }
}
