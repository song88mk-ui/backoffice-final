using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class BookmakerConfiguration
    {
        public int BookmakerId { get; set; }
        public string BookmakerName { get; set; }
        public int PrematchHierarchy { get; set; }
        public int LiveHierarchy { get; set; }
        public int PrematchWeight { get; set; }
        public int LiveWeight { get; set; }
        public int PrematchDelay { get; set; }
        public int LiveDelay { get; set; }
        public bool PrematchIsLastUpdate { get; set; }
        public bool LiveIsLastUpdate { get; set; }
        public int PrematchLastUpdateWeight { get; set; }
        public int LiveLastUpdateWeight { get; set; }
        public DateTime ModifiedDateTime { get; set; }
    }
}
