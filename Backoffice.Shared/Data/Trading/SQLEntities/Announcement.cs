using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class Announcement
    {
        public Guid AnnouncementId { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
        public DateTime DisplayStart { get; set; }
        public DateTime DisplayEnd { get; set; }
        public DateTime DateModified { get; set; }
        public string Status { get; set; }
        public bool IsDeleted { get; set; }
    }
}
