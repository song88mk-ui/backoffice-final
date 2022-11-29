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
        #region Top Fixtures
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds);
        Task<BaseResponse<ClientTopFixtureMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request, string region);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region);
        #endregion

        #region Top Leagues
        Task<BaseResponse<TopLeaguesResponse>> GetClientAllLeagues(ClientAllLeaguesRequest request, string region);
        Task<BaseResponse<ClientTopLeaguesResponse>> GetClientTopLeagues(TopLeaguesRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<CopyClientTopLeagueMessageResponse>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request);
        Task<BaseResponse<ClientTopLeagueMessageResponse>> AddOrUpdateTopLeagues(ClientTopLeaguesRequest request, string region);
        Task<BaseResponse<SportsLookupResponse>> GetClientSports(string LanguageCode, string region);
        #endregion

        #region Parlay Bonus
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request, string region);
        Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request, IDictionary<string, string> regionclientIds);
        #endregion
    }
}
