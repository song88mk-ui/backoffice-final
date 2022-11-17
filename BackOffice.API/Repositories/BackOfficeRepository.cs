using Backoffice.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        public Task<BaseResponseDTO<List<ClientListResponseDTO>>> GetClients()
        {
            throw new System.NotImplementedException();
        }
    }
}
