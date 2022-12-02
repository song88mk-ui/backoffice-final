using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services
{
    public interface IBackOfficeService
    {
        Task<BaseResponse<ClientListResponse>> GetClients();
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(GetClientTopFixtureRequest request);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeByClientId(int clientId);
        Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
    }
}
