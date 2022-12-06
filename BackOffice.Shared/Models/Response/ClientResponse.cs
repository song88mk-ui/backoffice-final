using System.Collections.Generic;
using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientResponse
    {
        public long ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Region { get; set; }
    }
}
