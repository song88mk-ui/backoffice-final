using Backoffice.Shared.Models;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IMaintenanceRepository
    {
        Task<BaseResponse<bool>> SetMaintenance(bool content);
        Task<BaseResponse<bool>> GetMaintenance();
    }
}
