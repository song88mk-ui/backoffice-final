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

        #region Nested Class
        public class ClientTopFixtureSetup
        {
            public long FixtureId { get; set; } = -1;
            public int SortOrder { get; set; } = -1;
        }
        #endregion
    }
}