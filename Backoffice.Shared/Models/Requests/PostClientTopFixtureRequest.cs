using System.Collections.Generic;

namespace Backoffice.Shared.Models.Requests
{
    public class PostClientTopFixtureRequest
    {
        public PostClientTopFixtureRequest()
        {
            TopFixtures = new List<ClientTopFixtureSetup>();
        }

        public int ClientId { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }
}