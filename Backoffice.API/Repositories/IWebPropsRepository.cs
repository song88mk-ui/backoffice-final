using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IWebPropsRepository
    {
        Task<BaseResponse<ClientFeatureFlagsResponse>> GetClientFeatureFlagsAsync(string clientId);
        Task<BaseResponse<ClientFeatureFlagsResponse>> UpdateClientFeatureFlagsAsync(UpdateClientFeatureFlagsRequest request);
    }
}
