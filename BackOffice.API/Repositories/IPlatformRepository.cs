using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IPlatformRepository
    {
        #region Top Fixture
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds);
        Task<BaseResponse<PlatformMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region);
        Task<BaseResponse<PlatformMessageResponse>> CopyClientTopFixtures(CopyClientTopFixtureRequest request, string region);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region);
        #endregion

        #region Top Leagues
        Task<BaseResponse<GetAllLeaguesResponse>> GetAllLeagues(GetAllLeaguesRequest request, string region);
        Task<BaseResponse<GetTopLeaguesResponse>> GetTopLeagues(GetTopLeaguesRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<bool>> CopyTopLeagues(CopyTopLeaguesRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<PlatformMessageResponse>> PostTopLeagues(PostTopLeaguesRequest request, string region);
        Task<BaseResponse<SportsLookupResponse>> GetClientSports(string LanguageCode, string region);
        #endregion

        #region Parlay Bonus
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request, string region);
        Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request, IDictionary<string, string> regionclientIds);
        #endregion
    }
}
