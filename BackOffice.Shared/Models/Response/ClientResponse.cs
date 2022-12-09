using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ClientResponse
    {
        public ClientResponse()
        {
            Clients = new List<Client>();
        }

        public List<Client> Clients { get; set; }

        #region Nested Class

        public class Client
        {
            public long ClientId { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
            public string Region { get; set; }
        }

        #endregion
    }
}
