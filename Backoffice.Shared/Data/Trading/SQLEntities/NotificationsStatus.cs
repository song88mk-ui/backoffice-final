using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class NotificationsStatus
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime DateRead { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
