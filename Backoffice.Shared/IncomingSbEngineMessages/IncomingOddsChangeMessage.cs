using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineBookmakerMessages;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backoffice.Shared.IncomingSbEngineMessages
{
    // Wire contract: Odds Engine v2 publishes OnOddsChange as OnInternalOddsChangeMessage with
    // abbreviated JSON property names (FiNa, FiTyId, Ma, ...). The [JsonPropertyName] attributes
    // below bind those names. Properties without an attribute are not on the OEv2 wire and keep
    // their defaults (e.g. CurrentPeriod: OEv2 sends only the numeric CuPe, no period name).
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
        [JsonPropertyName("SpId")]
        public int SportId { get; set; }
        [JsonPropertyName("SpNa")]
        public string SportName { get; set; }
        [JsonPropertyName("SegId")]
        public int SegmentId { get; set; }
        [JsonPropertyName("SegNa")]
        public string SegmentName { get; set; }
        [JsonPropertyName("LeId")]
        public int LeagueId { get; set; }
        [JsonPropertyName("LeNa")]
        public string LeagueName { get; set; }
        [JsonPropertyName("FiId")]
        public long FixtureId { get; set; }
        [JsonPropertyName("PrFiId")]
        public string ProviderFixtureId { get; set; }
        [JsonPropertyName("FiNa")]
        public string FixtureName { get; set; }
        [JsonPropertyName("FiStDa")]
        public DateTime FixtureStartDate { get; set; }
        [JsonPropertyName("FiStId")]
        public int FixtureStatusId { get; set; }
        [JsonPropertyName("FiStNa")]
        public string FixtureStatusName { get; set; }
        [JsonPropertyName("Sc")]
        public string Score { get; set; }
        [JsonPropertyName("FiSp")]
        public string FixtureSpecifier { get; set; }
        [JsonPropertyName("IsOfLi")]
        public bool IsOfferedLive { get; set; }
        [JsonPropertyName("HoReCa")]
        public int HomeRedCards { get; set; }
        [JsonPropertyName("AwReCa")]
        public int AwayRedCards { get; set; }
        [JsonPropertyName("CuPe")]
        public int CurrentPeriodId { get; set; }
        public string CurrentPeriod { get; set; }
        [JsonPropertyName("CuMi")]
        public int CurrentMinute { get; set; }
        [JsonPropertyName("FiTyId")]
        public int FixtureTypeId { get; set; }
        [JsonPropertyName("FiTyNa")]
        public string FixtureTypeName { get; set; }
        [JsonPropertyName("ToTi")]
        public int TotalTime { get; set; }
        [JsonPropertyName("Fa")]
        public string Favorite { get; set; }
        [JsonPropertyName("SoOr")]
        public int SortOrder { get; set; }
        [JsonPropertyName("ClCo")]
        public string ClientCodes { get; set; }
        public string CorrelationId { get; set; }
        [JsonPropertyName("IsBo")]
        public bool? IsBoosted { get; set; }
        //public List<SportTranslation> SportTranslations { get; set; }
        //public List<SegmentTranslation> SegmentTranslations { get; set; }
        //public List<LeagueTranslation> LeagueTranslations { get; set; }
        public List<FixtureTranslation> FixtureTranslations { get; set; }
        [JsonPropertyName("Ma")]
        public List<IncomingMarketMessage> Markets { get; set; }
        [JsonPropertyName("HoId")]
        public string HomeId { get; set; }
        public int HomeBetradarId { get; set; }
        [JsonPropertyName("AwId")]
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
        [JsonPropertyName("PrFiId")]
        public string ProviderFixtureId { get; set; }
        [JsonPropertyName("MaId")]
        public long MarketId { get; set; }
        [JsonPropertyName("MaTeId")]
        public string MarketTemplateId { get; set; }
        [JsonPropertyName("PrMaId")]
        public string ProviderMarketId { get; set; }
        [JsonPropertyName("Li")]
        public string Line { get; set; }
        [JsonPropertyName("MaStId")]
        public MarketStatus MarketStatus { get; set; }
        //public CashoutStatus CashoutStatus { get; set; }
        [JsonPropertyName("MaSp")]
        public string MarketSpecifier { get; set; }
        [JsonPropertyName("MaNa")]
        public string MarketName { get; set; }
        [JsonPropertyName("SoOr")]
        public int SortOrder { get; set; }
        public bool IsInsert { get; set; }
        [JsonPropertyName("ClCo")]
        public string ClientCodes { get; set; }
        [JsonPropertyName("ClCoMa")]
        public string ClientCodeMargin { get; set; }
        //public List<MarketTranslation> MarketTranslations { get; set; }
        [JsonPropertyName("Se")]
        public List<IncomingSelectionMessage> Selections { get; set; }
    }

    public class IncomingSelectionMessage
    {
        public IncomingSelectionMessage()
        {
            //SelectionTranslations = new List<SelectionTranslation>();
        }
        [JsonPropertyName("PrFiId")]
        public string ProviderFixtureId { get; set; }
        [JsonPropertyName("MaTeId")]
        public string MarketTemplateId { get; set; }
        [JsonPropertyName("SeTeId")]
        public string SelectionTemplateId { get; set; }
        [JsonPropertyName("MaSp")]
        public string MarketSpecifier { get; set; }
        [JsonPropertyName("SeId")]
        public long SelectionId { get; set; }
        public string ProviderSelectionId { get; set; }
        [JsonPropertyName("Od")]
        public decimal Odds { get; set; }
        [JsonPropertyName("Pr")]
        public double Probability { get; set; }
        [JsonPropertyName("WoPr")]
        public double WonProbability { get; set; }
        [JsonPropertyName("LoPr")]
        public double LostProbability { get; set; }
        [JsonPropertyName("VoPr")]
        public double VoidProbability { get; set; }
        [JsonPropertyName("HaWoPr")]
        public double HalfWonProbability { get; set; }
        [JsonPropertyName("HaLoPr")]
        public double HalfLostProbability { get; set; }
        [JsonPropertyName("IsAc")]
        public bool IsActive { get; set; }
        [JsonPropertyName("SeSp")]
        public string SelectionSpecifier { get; set; }
        [JsonPropertyName("SeNa")]
        public string SelectionName { get; set; }
        [JsonPropertyName("SoOr")]
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
