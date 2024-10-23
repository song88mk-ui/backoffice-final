using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IMaintenanceRepository
    {
        Task<BaseResponse<bool>> SetMaintenance(bool content);
        Task<BaseResponse<bool>> GetMaintenance();
        Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2(List<string> clientCodes);
        Task<BaseResponse<bool>> SetMaintenanceV2(SetMaintenanceRequest request);
    }
}
