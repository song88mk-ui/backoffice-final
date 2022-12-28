using System;
using System.Collections.Generic;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Shared.Models
{
    public class FixtureMarketModel
    {
        public FixtureMarketModel()
        {
            Selections = new List<SelectionModel>();
        }
        public string FixtureName { get; set; }
        public int FixtureStatusId { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public string Score { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public List<int> BetStop { get; set; }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string ProviderMarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public string MarketSpecifier { get; set; }
        public string ClientCode { get; set; }
        public string ClientCodeMargin { get; set; }
        public List<SelectionModel> Selections { get; set; }
    }
}
