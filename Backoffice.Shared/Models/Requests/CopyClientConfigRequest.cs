using System;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientConfigRequest
    {
        public string FromClientCode { get; set; }
        public Guid ToClientId { get; set; }
        public string ToClientCode { get; set; }
        public string ToClientName { get; set; }
    }
}
