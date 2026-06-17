using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class ClientSportOrderAudit
    {
        public string ClientCode { get; set; }
        public string ChangesFrom { get; set; }
        public string ChangesTo { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
