using static Backoffice.Shared.Models.Requests.GetTopLeaguesRequest;

namespace Backoffice.Web.Models
{
    public class CopyTopLeaguesModel
    {
        public int CopyFromClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientIdSelectedModel> ClientIdSelectedModel { get; set; }
        public List<OrderedClientTopLeague> CopyFromClientTopLeagues { get; set; }
    }
}
