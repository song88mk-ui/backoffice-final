using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;

namespace Backoffice.Web.Models
{
    public class TopFixturesModel
    {
        public TopFixturesModel()
        {
            TopFixtures = new List<OrderedClientTopFixture>();
            TopFixturesFiltered = new List<OrderedClientTopFixture>();
        }
        public List<OrderedClientTopFixture> TopFixtures { get; set; }
        public List<OrderedClientTopFixture> TopFixturesFiltered { get; set; }
        
    }

    public class TopFixtureCallback
    {
        public string ClientIds { get; set; }
        public string SearchText { get; set; }
    }
}
