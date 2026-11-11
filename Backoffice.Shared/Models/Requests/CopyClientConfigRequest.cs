using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    // Copy Client Settings (Config page): every league template assignment (prematch and live) of FromClientCode
    // replaces those of each target client. BO-162: ToClients holds every target; the single ToClient* fields are the
    // request an older Web sends and are used only when ToClients is empty.
    public class CopyClientConfigRequest
    {
        public string FromClientCode { get; set; }
        public List<CopyClientConfigTarget> ToClients { get; set; } = new List<CopyClientConfigTarget>();
        public string UserId { get; set; }

        public Guid ToClientId { get; set; }
        public string ToClientCode { get; set; }
        public string ToClientName { get; set; }
    }

    public class CopyClientConfigTarget
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
    }
}
