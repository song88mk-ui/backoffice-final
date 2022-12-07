using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages;

namespace Backoffice.Web.Services
{
    public interface IBackOfficeService
    {
        Task<BaseResponse<ClientListResponse>> GetClients();
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(GetClientTopFixtureRequest request);
        Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        
        Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeaguesAsync(GetTopLeaguesRequest request);
        Task<BaseResponse<PlatformMessageResponse>> PostTopLeaguesAsync(PostTopLeaguesRequest request);
        Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request);        
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeByClientId(int clientId);
        Task<BaseResponse<SportsLookupResponse>> GetSportsAsync(string languageCode);
        Task<BaseResponse<List<ClientParlayBonusResponse>>> GetClientParlayBonus(GetParlayBonusRequest request);
        void Dispose();
    }
}
