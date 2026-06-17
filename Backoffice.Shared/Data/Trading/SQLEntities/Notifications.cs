using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class Notifications
    {
        public Guid Id { get; set; }
        public string ClaimType { get; set; }
        public DateTime DateCreated { get; set; }
        public string Page { get; set; }
        public string ClientCode { get; set; }
        public int? SportsId { get; set; }
        public string Details { get; set; }
        public string PageRedirect { get; set; }
    }
}
