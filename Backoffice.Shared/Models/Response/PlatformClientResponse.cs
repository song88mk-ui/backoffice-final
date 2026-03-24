using System;

namespace Backoffice.Shared.Models.Response
{
    public class PlatformClientResponse
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public int ClientIdRef { get; set; }
        public bool IsAgent { get; set; }
    }
}
