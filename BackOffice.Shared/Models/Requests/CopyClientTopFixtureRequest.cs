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
            TopFixtures = new List<ClientTopFixtureSetup>();
        }
        public List<int> ClientIds { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }
}
