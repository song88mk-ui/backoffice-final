using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class MarketFixtureResponse
    {
        public MarketFixtureResponse()
        {
            Fixtures = new List<FixtureModel>();
        }

        public List<FixtureModel> Fixtures { get; set; }

        #region Nested Class
        public class FixtureModel
        {
            public FixtureModel()
            {
                Markets = new List<MarketModel>();
            }
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public string ProviderFixtureId { get; set; }
            public string FixtureId { get; set; }
            public string FixtureName { get; set; }
            public string HomeName { get; set; }
            public string AwayName { get; set; }
            public DateTime FixtureStartDate { get; set; }
            public bool IsLive { get; set; }
            public bool IsLiveStream { get; set; }
            public bool IsSuspended { get; set; }
            public int FixtureStatusId { get; set; }
            public string FixtureStatusName { get; set; }
            public int MarketCount { get; set; }
            public string SMSCode { get; set; }
            public string StatisticsURL { get; set; }
            public string Score { get; set; }
            public string CurrentPeriod { get; set; }
            public string CurrentMinute { get; set; }
            public List<int> BetStop { get; set; }
            public bool IsBetStopGlobal { get; set; }
            public int SortOrder { get; set; }
            public int FixtureTypeId { get; set; }
            public SportMarketTemplateGroup MarketTemplateGroups { get; set; }
            public List<MarketModel> Markets { get; set; }
        }

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

        public class MarketTemplateGroup
        {
            public int MarketTemplateGroupId { get; set; }
            public string MarketTemplateGroupName { get; set; }
            public int SortOrder { get; set; }
            public List<string> MarketTemplateIds { get; set; }
        }

        public class MarketModel
        {
            public MarketModel()
            {
                Selections = new List<SelectionModel>();
            }

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
            public bool IsBoosted { get; set; }
            public List<SelectionModel> Selections { get; set; }
        }

        public class SelectionModel
        {
            public string ProviderSelectionId { get; set; }
            public string SelectionId { get; set; }
            public string SelectionTemplateId { get; set; }
            public string SelectionName { get; set; }
            public string SelectionSpecifier { get; set; }
            public string DecimalOdds { get; set; }
            public bool IsActive { get; set; }
            public int SortOrder { get; set; }
            public string CorrelationId { get; set; }
            public string OldDecimalOdds { get; set; }
            public List<ClientOddsModel> ClientOdds { get; set; }
        }

        public class ClientOddsModel
        {
            public string ClientCode { get; set; }
            public decimal Odds { get; set; }
            public decimal OldOdds { get; set; }
            public string SelectionTemplateId { get; set; }
            public long SelectionId { get; set; }
        }
        #endregion
    }
}
