namespace Backoffice.Web.Models
{
    public class ClientListModel
    {
        public ClientListModel()
        {
            Clients = new List<Client>();
        }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPage { get; set; }
        public List<Client> Clients { get; set; }

        #region Nested Class

        public class Client
        {
            public string ClientCode { get; set; }
            public int Num { get; set; }
            public int ClientId { get; set; }
            public string Name { get; set; }
            public string Website { get; set; }
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public string Region { get; set; }
        }

        #endregion
    }
}
