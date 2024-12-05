using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.IncomingSbEngineMessages
{
    public class IncomingOddsChangeMessage
    {
        public IncomingOddsChangeMessage()
        {
            Markets = new List<IncomingMarketMessage>();
            //SportTranslations = new List<SportTranslation>();
            //SegmentTranslations = new List<SegmentTranslation>();
            //LeagueTranslations = new List<LeagueTranslation>();
            FixtureTranslations = new List<FixtureTranslation>();
            PeriodScores = new List<IncomingPeriodScore>();
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
        public int CurrentPeriodId { get; set; }
        public string CurrentPeriod { get; set; }
        public int CurrentMinute { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public int TotalTime { get; set; }
        public string Favorite { get; set; }
        public int SortOrder { get; set; }
        public string ClientCodes { get; set; }
        public string CorrelationId { get; set; }
        public bool? IsBoosted { get; set; }
        //public List<SportTranslation> SportTranslations { get; set; }
        //public List<SegmentTranslation> SegmentTranslations { get; set; }
        //public List<LeagueTranslation> LeagueTranslations { get; set; }
        public List<FixtureTranslation> FixtureTranslations { get; set; }
        public List<IncomingMarketMessage> Markets { get; set; }
        public string HomeId { get; set; }
        public int HomeBetradarId { get; set; }
        public string AwayId { get; set; }
        public int AwayBetradarId { get; set; }
        public int LeagueBetradarId { get; set; }
        public List<IncomingPeriodScore> PeriodScores { get; set; }
        public int CurrentServer { get; set; }
        public bool IsInsert { get; set; }
    }

    public class IncomingMarketMessage
    {
        public IncomingMarketMessage()
        {
            Selections = new List<IncomingSelectionMessage>();
            //MarketTranslations = new List<MarketTranslation>();
        }
        public string ProviderFixtureId { get; set; }
        public long MarketId { get; set; }
        public string MarketTemplateId { get; set; }
        public string ProviderMarketId { get; set; }
        public string Line { get; set; }
        public MarketStatus MarketStatus { get; set; }
        //public CashoutStatus CashoutStatus { get; set; }
        public string MarketSpecifier { get; set; }
        public string MarketName { get; set; }
        public int SortOrder { get; set; }
        public bool IsInsert { get; set; }
        public string ClientCodes { get; set; }
        public string ClientCodeMargin { get; set; }
        //public List<MarketTranslation> MarketTranslations { get; set; }
        public List<IncomingSelectionMessage> Selections { get; set; }
    }

    public class IncomingSelectionMessage
    {
        public IncomingSelectionMessage()
        {
            //SelectionTranslations = new List<SelectionTranslation>();
        }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string MarketSpecifier { get; set; }
        public long SelectionId { get; set; }
        public string ProviderSelectionId { get; set; }
        public decimal Odds { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public bool IsActive { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionName { get; set; }
        public int SortOrder { get; set; }
        public bool IsInsert { get; set; }
        public List<ClientOdds> ClientOdds { get; set; }
        //public List<SelectionTranslation> SelectionTranslations { get; set; }
    }

    public class IncomingPeriodScore
    {
        public int Home { get; set; }
        public int Away { get; set; }
        public string MatchStatusCode { get; set; }
        public string PeriodType { get; set; }
    }
}
