using System.Collections.Generic;
using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientResponse
    {
        public long ClientId { get; set; }
        public string BusinessName { get; set; }
        public string Brand { get; set; }
        public string Region { get; set; }
    }
}
