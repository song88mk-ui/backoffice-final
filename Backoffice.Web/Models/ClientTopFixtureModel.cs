namespace Backoffice.Web.Models
{
    public class ClientTopFixtureModel
    {
        public ClientTopFixtureModel()
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
            public List<TopFixture> TopFixtures { get; set; }
        }

        public class TopFixture
        {
            public int Index { get; set; }
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public string FixtureId { get; set; }
            public string FixtureName { get; set; }
            public DateTime FixtureStartDate { get; set; }
            public string FixtureStatus { get; set; }
            public int SortOrder { get; set; }
        }
        #endregion
    }
}
