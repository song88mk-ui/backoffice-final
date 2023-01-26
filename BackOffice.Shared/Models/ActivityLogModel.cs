using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class ActivityLogModel
    {
        public long Id { get; set; }
        public string Page { get; set; }
        public string Payload { get; set; }
        public string Activity { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
