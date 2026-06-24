using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class ClientOddsFeedConfiguration
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public bool PrematchOdds { get; set; }
        public bool LiveOdds { get; set; }
        public int? MaxFixtures { get; set; }
    }
}
