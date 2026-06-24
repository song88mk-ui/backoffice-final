using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class PriceBoostConfig
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal BoostedOdds { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
        public string BoostedSelectionName { get; set; }
        public string BoostedSelectionTranslation { get; set; }
        public string MarketSpecifier { get; set; }
    }

    public class PriceBoostConfigAudit
    {
        public string ClientCode { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureTypeId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal BoostedOdds { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ChangesMade { get; set; }
        public string SportName { get; set; }
    }
}
