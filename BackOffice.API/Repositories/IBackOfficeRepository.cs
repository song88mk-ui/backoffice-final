using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<List<ClientListResponse>>> GetClients();
        Task<string> GetClientRegion(int clientId);
    }
}
