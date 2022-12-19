using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class PriceBoostConfigModel
    {
        public PriceBoostConfigModel()
        {
            BoostedSelectionTranslations = new List<BoostedSelectionTranslation>();
        }
        public string ClientCode { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int FixtureTypeId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketName { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionName { get; set; }
        public string SelectionSpecifier { get; set; }
        public decimal BROdds { get; set; }
        public decimal ClientOdds { get; set; }
        public decimal BoostedOdds { get; set; }
        [MaxLength(5, ErrorMessage = "Odds is too long.")]
        //[Range(1, 99999, ErrorMessage = "Boosted Odds min value cannot be lower than 1.00!")]
        public string BoostedOddsStr { get; set; }
        public string BoostedSelectionName { get; set; }
        public List<BoostedSelectionTranslation> BoostedSelectionTranslations { get; set; }
        public int FixtureStatusId { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public string Score { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string IconFixtureClass { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
