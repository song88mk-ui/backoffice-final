using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class TopFixturesModel
    {
        public TopFixturesModel()
        {
            TopFixtures = new List<ClientTopFixtureResponse.OrderedClientTopFixture>();
            TopFixturesFiltered = new List<ClientTopFixtureResponse.OrderedClientTopFixture>();
        }
        public List<ClientTopFixtureResponse.OrderedClientTopFixture> TopFixtures { get; set; }
        public List<ClientTopFixtureResponse.OrderedClientTopFixture> TopFixturesFiltered { get; set; }
        
    }

    public class TopFixtureCallback
    {
        public List<ClientResponse> SelectedClients { get; set; }
        public string SearchText { get; set; }
    }
}
