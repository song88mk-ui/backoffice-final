using System.Net.Http;

namespace Backoffice.API.Repositories
{
    public class TradingRepository : ITradingRepository
    {
        private HttpClient _httpClient { get; set; }

        public TradingRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
    }
}
