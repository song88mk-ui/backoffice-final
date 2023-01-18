namespace Backoffice.Web.Models
{
    public class TopFixtureCallbackModel
    {
        #region Ctor

        public TopFixtureCallbackModel()
        {
            Clients = new List<Client>();
        }

        #endregion

        public string SearchText { get; set; }
        public List<Client> Clients { get; set; }

        #region Nested Class

        public class Client
        {
            public long ClientId { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
            public string Region { get; set; }
        }

        #endregion
    }
}
