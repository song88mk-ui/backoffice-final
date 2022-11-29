using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class CopyClientTopFixtureRequest
    {
        public CopyClientTopFixtureRequest()
        {
            ClientIds = new List<int>();
            TopFixtures = new List<ClientTopSetup>();
        }
        public List<int> ClientIds { get; set; }
        public List<ClientTopSetup> TopFixtures { get; set; }
    }
}
