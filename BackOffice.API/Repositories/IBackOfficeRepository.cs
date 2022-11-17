using Backoffice.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponseDTO<List<ClientListResponseDTO>>> GetClients();
    }
}
