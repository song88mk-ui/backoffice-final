namespace Backoffice.Web.Models
{
    public class TopFixturesModel
    {
        public TopFixturesModel()
        {
            TopFixtures = new List<ClientTopFixtureModel.OrderedClientTopFixture>();
            TopFixturesFiltered = new List<ClientTopFixtureModel.OrderedClientTopFixture>();
        }
        public List<ClientTopFixtureModel.OrderedClientTopFixture> TopFixtures { get; set; }
        public List<ClientTopFixtureModel.OrderedClientTopFixture> TopFixturesFiltered { get; set; }

    }
}
