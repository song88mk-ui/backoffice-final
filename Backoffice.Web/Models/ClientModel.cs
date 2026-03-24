using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class ClientModel
    {
        public ClientModel()
        {
            Clients = new List<Client>();
        }

        public List<Client> Clients { get; set; }
    }
    public class Client
    {
        public Guid ClientGuid { get; set; }
        public long ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Region { get; set; }
    }

    public class PlatformClientModel
    {
        public PlatformClientModel()
        {
            Clients = new List<PlatformClientResponse>();
        }

        public List<PlatformClientResponse> Clients { get; set; }
    }
}
