using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<List<ClientListResponse>>> GetClients();
        Task<string> GetClientRegion(int clientId);
        Task<Dictionary<string, string>> GetClientsByRegion(string clientIdRefs);
        Task<BaseResponse<BaseResponse<bool>>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
    }
}
