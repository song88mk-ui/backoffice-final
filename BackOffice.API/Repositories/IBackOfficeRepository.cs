using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<List<ClientListResponse>>> GetClients();
        Task<BaseResponse<PageListResponse<ClientListResponse>>> GetClients(int page, int pageSize, string businessName, string sortOrder);
        Task<BaseResponse<string>> GetClientRegion(int clientId);
        Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs);
        Task<BaseResponse<BaseResponse<bool>>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        List<CurrenciesResponse> GetCurrencies();
    }
}
