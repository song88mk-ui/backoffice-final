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
        public string UserName { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateTime TimeStamp { get; set; }
        public MetaData MetaData { get; set; }
        public string SearchText { get; set; } = "";
        // BO-104: extra values matched against Payload when SearchText is set. The Client Site
        // Config logs modal resolves a typed client name/code to client GUIDs here, because the
        // log row stores only "{userName}.{clientGuid}" in Payload.
        public List<string> PayloadSearchTerms { get; set; } = new List<string>();
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
