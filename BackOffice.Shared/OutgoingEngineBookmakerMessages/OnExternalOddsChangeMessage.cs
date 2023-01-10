using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.OutgoingEngineBookmakerMessages
{
    public class OnExternalOddsChangeMessage
    {
        public OnExternalOddsChangeMessage()
        {
            Markets = new List<ExternalMarketMessage>();
            SportTranslations = new List<SportTranslation>();
            SegmentTranslations = new List<SegmentTranslation>();
            LeagueTranslations = new List<LeagueTranslation>();
            FixtureTranslations = new List<FixtureTranslation>();
        }

        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Score { get; set; }
        public string FixtureSpecifier { get; set; }
        public bool IsOfferedLive { get; set; }
        public int HomeRedCards { get; set; }
        public int AwayRedCards { get; set; }
        public string CurrentPeriod { get; set; }
        public int CurrentMinute { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public int TotalTime { get; set; }
        public string Favorite { get; set; }
        public int SortOrder { get; set; }
        public string CorrelationId { get; set; }
        public string ClientCodes { get; set; }
        public List<SportTranslation> SportTranslations { get; set; }
        public List<SegmentTranslation> SegmentTranslations { get; set; }
        public List<LeagueTranslation> LeagueTranslations { get; set; }
        public List<FixtureTranslation> FixtureTranslations { get; set; }
        public List<ExternalMarketMessage> Markets { get; set; }
    }

    public class ExternalMarketMessage
    {
        public ExternalMarketMessage()
        {
            Selections = new List<ExternalSelectionMessage>();
            MarketTranslations = new List<MarketTranslation>();
        }
        public long MarketId { get; set; }
        public string MarketTemplateId { get; set; }
        public string Line { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        //public int CashoutStatusId { get; set; }
        //public string CashoutStatusName { get; set; }
        public string MarketSpecifier { get; set; }
        public string MarketName { get; set; }
        public int SortOrder { get; set; }
        public string ClientCodes { get; set; }
        public string ClientCodeMargin { get; set; }
        public List<MarketTranslation> MarketTranslations { get; set; }
        public List<ExternalSelectionMessage> Selections { get; set; }
    }

    public class ExternalSelectionMessage
    {
        public ExternalSelectionMessage()
        {
            SelectionTranslations = new List<SelectionTranslation>();
        }
        public long SelectionId { get; set; }
        public string SelectionTemplateId { get; set; }
        public decimal Odds { get; set; }
        public double Probability { get; set; }
        public bool IsActive { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionName { get; set; }
        public int SortOrder { get; set; }
        public List<ClientOdds> ClientOdds { get; set; }
        public List<SelectionTranslation> SelectionTranslations { get; set; }
    }

    public class ClientOdds
    {
        public string ClientCode { get; set; }
        public decimal DecimalOdds { get; set; }
        public decimal MalaysianOdds { get; set; }
        public decimal HongkongOdds { get; set; }
        public decimal IndonesianOdds { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
    }
}
