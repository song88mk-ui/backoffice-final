using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class PostClientTopFixtureRequest
    {
        public PostClientTopFixtureRequest()
        {
            TopFixtures = new List<ClientTopSetup>();
        }

        public int ClientId { get; set; }
        public List<ClientTopSetup> TopFixtures { get; set; }
    }
}