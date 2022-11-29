using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<ClientListResponse>> GetClients();
        Task<BaseResponse<string>> GetClientRegion(int clientId);
        Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs);

        #region TOP FIXTURES
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        #endregion

        #region TOP LEAGUES
        Task<BaseResponse<bool>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request);

        #endregion

        Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies();
    }
}
