using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class PriceBoostActivityLogModel
    {
        public PriceBoostActivityLogModel()
        {
            From = new PriceBoostLogModel();
            To = new PriceBoostLogModel();
        }
        public long Id { get; set; }
        public string Page { get; set; }
        public string Payload { get; set; }
        public string Activity { get; set; }
        public PriceBoostLogModel From { get; set; }
        public PriceBoostLogModel To { get; set; }
        public string UserId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
    public class PriceBoostLogModel
    {
        public PriceBoostLogModel()
        {
            Translations = new List<BoostedSelectionTranslation>();
        }
        public string ClientName { get; set; } = "";
        public string ClientCode { get; set; } = "";
        public string SelectionName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string SportName { get; set; } = "";
        public string SegmentName { get; set; } = "";
        public string LeagueName { get; set; } = "";
        public string FixtureName { get; set; } = "";
        public decimal Odds { get; set; } = 0m;
        public string UserName { get; set; } = "";
        public string Summary { get; set; } = "";
        public string HtmlDetail { get; set; } = "";
        public string SelectionTemplateId { get; set; } = "";
        public string MarketSpecifier { get; set; } = "";
        public List<BoostedSelectionTranslation> Translations { get; set; }
    }
}
