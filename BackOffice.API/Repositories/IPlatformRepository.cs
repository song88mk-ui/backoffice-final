using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.ClientTopLeaguesResponse;

namespace Backoffice.API.Repositories
{
    public interface IPlatformRepository
    {
        #region TOP FIXTURE
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds);
        Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region);
        Task<BaseResponse<PlatformMessageResponse>> CopyClientTopFixtures(CopyClientTopFixtureRequest request, string region);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region);
        #endregion

        #region TOP LEAGUES
        Task<BaseResponse<TopLeaguesResponse>> GetClientAllLeagues(ClientAllLeaguesRequest request, string region);
        Task<BaseResponse<ClientTopLeaguesResponse>> GetClientTopLeagues(TopLeaguesRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<PlatformMessageResponse>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request, string region);
        Task<BaseResponse<PlatformMessageResponse>> AddOrUpdateTopLeagues(ClientTopLeaguesRequest request, string region);
        Task<BaseResponse<SportsLookupResponse>> GetClientSports(string LanguageCode, string region);
        #endregion

        #region Parlay Bonus
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request, string region);
        Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request, IDictionary<string, string> regionclientIds);
        #endregion
    }
}
