using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class NotificationsRequest
    {
        public Guid UserId { get; set; }
        public string PageFilter { get; set; }
        public string ClientFilter { get; set; }
        public int SportFilter { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsNewOnly { get; set; }
        public MetaData MetaData { get; set; }
    }
}
