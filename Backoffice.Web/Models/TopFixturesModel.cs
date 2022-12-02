using static Backoffice.Shared.Models.Response.ClientTopFixtureResponse;

namespace Backoffice.Web.Models
{
    public class TopFixturesModel
    {
        public TopFixturesModel()
        {
            TopFixtures = new List<OrderedClientTopFixture>();
        }
        public List<OrderedClientTopFixture> TopFixtures { get; set; }
        public string ClientCode { get; set; }
        public Guid ClientId { get; set; }
    }
}
