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
        Task<BaseResponse<ClientTopFixtureMessageResponse>> PostClientTopFixtures(PostClientTopFixtureRequest request, string region);
        Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(IDictionary<string, string> regionclientIds);
        Task<BaseResponse<SportsTreeResponse>> GetSportstreeDetails(int clientId, string region);
        Task<BaseResponse<ClientTopLeaguesResponse>> GetClientTopLeagues(TopLeaguesRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<ClientTopLeagueMessageResponse>> AddOrUpdateTopLeagues(ClientTopLeaguesRequest request, string region);
        Task<BaseResponse<CopyClientTopLeagueMessageResponse>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request);
        Task<BaseResponse<SportsLookupResponse>> GetClientSports(string LanguageCode, string region);
        Task<BaseResponse<TopLeaguesResponse>> GetClientAllLeagues(ClientAllLeaguesRequest request, string region);
        Task<BaseResponse<ClientParlayBonusResponse>> GetClientParlayBonus(GetParlayBonusRequest request, IDictionary<string, string> regionclientIds);
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request, string region);
    }
}
