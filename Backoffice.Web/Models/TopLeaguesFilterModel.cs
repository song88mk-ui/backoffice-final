namespace Backoffice.Web.Models
{
    public class TopLeaguesFilterModel
    {
        public TopLeaguesFilterModel()
        {
            Clients = new List<Client>();
        }

        public string ClientId { get; set; }
        public string SportId { get; set; }
        public string SportName { get; set; }
        public string Search { get; set; }
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
