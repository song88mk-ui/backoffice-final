using System;
using System.Collections.Generic;

namespace Backoffice.Shared.IncomingSbEngineMessages
{
    public class IncomingBetStopFixtureMessage
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public long FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public int FixtureTypeId { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public int FixtureStatusId { get; set; }
        public bool IsBetStop { get; set; }
        public List<int> MarketTemplateGroups { get; set; }
        public string CorrelationId { get; set; }
    }
}
