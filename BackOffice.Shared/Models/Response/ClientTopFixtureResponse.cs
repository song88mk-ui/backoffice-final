using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientTopFixtureResponse
    {
        public ClientTopFixtureResponse()
        {
            TopFixtures = new List<OrderedClientTopFixture>();
        }

        public List<OrderedClientTopFixture> TopFixtures { get; set; }

        #region Nested Class
        public class OrderedClientTopFixture
        {
            public Guid ClientId { get; set; }
            public int ClientIdRef { get; set; }
            public string ClientName { get; set; }
            public List<FixtureModel> TopFixtures { get; set; }
        }
        #endregion
    }
}
