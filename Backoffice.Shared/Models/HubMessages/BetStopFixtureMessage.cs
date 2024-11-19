using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.HubMessages
{
    public class BetStopFixtureMessage
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public bool IsBetStop { get; set; }
        public List<int> MarketTemplateGroups { get; set; }
        public string CorrelationId { get; set; }
    }
}
