using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backoffice.Shared.IncomingSbEngineMessages
{
    // Wire contract: Odds Engine v2 publishes OnBetStopFixture with abbreviated id property names
    // (FiId, SpId, LeId, ...) while IsBetStop/CorrelationId stay unabbreviated. Properties without
    // an attribute (MarketTemplateGroups) are not on the OEv2 wire and keep their defaults.
    public class IncomingBetStopFixtureMessage
    {
        [JsonPropertyName("SpId")]
        public int SportId { get; set; }
        [JsonPropertyName("SegId")]
        public int SegmentId { get; set; }
        [JsonPropertyName("LeId")]
        public int LeagueId { get; set; }
        [JsonPropertyName("FiId")]
        public long FixtureId { get; set; }
        [JsonPropertyName("PrFiId")]
        public string ProviderFixtureId { get; set; }
        [JsonPropertyName("FiTyId")]
        public int FixtureTypeId { get; set; }
        [JsonPropertyName("FiStDa")]
        public DateTime FixtureStartDate { get; set; }
        [JsonPropertyName("FiStId")]
        public int FixtureStatusId { get; set; }
        public bool IsBetStop { get; set; }
        public List<int> MarketTemplateGroups { get; set; }
        public string CorrelationId { get; set; }
    }
}
