using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services
{
    public interface IBackOfficeService
    {
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(string clientIds)
    }
}
