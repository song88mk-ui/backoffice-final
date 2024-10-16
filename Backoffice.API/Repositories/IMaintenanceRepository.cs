using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IMaintenanceRepository
    {
        Task<BaseResponse<bool>> SetMaintenance(bool content);
        Task<BaseResponse<bool>> GetMaintenance();
        Task<BaseResponse<List<string>>> GetMaintenanceV2(List<string> clientIds);
        Task<BaseResponse<bool>> SetMaintenanceV2(SetMaintenanceRequest request);
    }
}
