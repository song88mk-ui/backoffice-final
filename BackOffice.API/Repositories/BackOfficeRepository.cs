using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        public Task<BaseResponse<List<ClientListResponse>>> GetClients()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetClientRegion(int clientId)
        {
            throw new System.NotImplementedException();
        }
    }
}
