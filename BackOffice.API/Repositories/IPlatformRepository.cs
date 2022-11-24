using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IPlatformRepository
    {
        Task<BaseResponse<ClientTopFixtureMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region);
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region);
    }
}
