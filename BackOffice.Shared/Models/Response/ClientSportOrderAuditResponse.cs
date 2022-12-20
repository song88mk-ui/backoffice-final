using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientSportOrderAuditResponse
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string ChangesFrom { get; set; }
        public string ChangesTo { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
