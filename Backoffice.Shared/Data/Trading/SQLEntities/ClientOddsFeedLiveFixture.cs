using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class ClientOddsFeedLiveFixture
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string FixtureSpecifier { get; set; }
        public DateTime FixtureStartDate { get; set; }
    }
}
