using System.Collections.Generic;

namespace Backoffice.Shared.OutgoingEngineBookmakerMessages
{
    public class OnExternalBetStopFixtureMessage
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public bool IsBetStop { get; set; }
        public List<int> MarketTemplateGroups { get; set; }
        public string CorrelationId { get; set; }
    }
}
