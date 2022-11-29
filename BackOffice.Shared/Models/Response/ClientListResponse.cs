using System.Collections.Generic;
using System;

namespace Backoffice.Shared.Models.Response
{
    public class ClientListResponse
    {
        public ClientListResponse()
        {
            Clients = new List<ClientResponse>();
        }
        public List<ClientResponse> Clients { get; set; }
    }
}
