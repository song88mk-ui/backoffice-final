using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class Client
    {
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public Guid ClientId { get; set; }
        public string Email { get; set; }
    }
}
