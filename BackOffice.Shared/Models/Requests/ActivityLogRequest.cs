using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class ActivityLogRequest
    {
        public string Page { get; set; }
        public string Payload { get; set; }
        public string Activity { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string UserId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateTime TimeStamp { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class PriceboostActivityLogRequest : ActivityLogRequest
    {
        public PriceboostActivityLogRequest()
        {
            UserClients = new List<string>();
        }
        public List<string> UserClients { get; set; }
    }
}
