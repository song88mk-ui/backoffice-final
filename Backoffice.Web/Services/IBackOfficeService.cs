using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface IBackOfficeService
    {
        Task<BaseResponse<ClientModel>> GetClients();
        Task<BaseResponse<ClientListModel>> GetClientList(GetClientListRequest request);
        Task<BaseResponse<ClientTopFixtureModel>> GetClientTopFixtures(GetClientTopFixtureRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostClientTopFixtures(PostClientTopFixtureRequest request);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        
        Task<BaseResponse<GetAllLeaguesModel>> GetAllLeaguesAsync(GetAllLeaguesRequest request);
        Task<BaseResponse<TopLeaguesModel>> GetTopLeaguesAsync(GetTopLeaguesRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostTopLeaguesAsync(PostTopLeaguesRequest request);
        Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request);        
        Task<BaseResponse<SportsTreeModel>> GetSportstreeByClientId(int clientId);
        Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId);
        Task<BaseResponse<SportsLookupModel>> GetSportsAsync(string languageCode);
        Task<BaseResponse<ClientParlayBonusModel>> GetClientParlayBonus(GetParlayBonusRequest request);
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request);
    }
}
