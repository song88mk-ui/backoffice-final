namespace Backoffice.Shared.Models.Requests
{
    public class ClientOddstableLeagueOrderListRequest
    {
        // Empty = every client.
        public string ClientCode { get; set; }
        // 0 = every sport.
        public int SportId { get; set; }
    }
}
