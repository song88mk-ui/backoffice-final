using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IBackofficeRepository
    {
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementHistoryLogs(ManualBetSettlementActivityLogRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<ClientResponse>> GetClients();
        Task<BaseResponse<string>> GetClientRegion(int clientId);
        Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs);
        Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request);
        Task<BaseResponse<BetSettingResponse>> GetBetSetting(BetSettingRequest request);
        #region Config
        Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplates(string userId, List<string> clientCodes, List<int> sportIds = null, List<int> scopeIds = null);
        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(string userId, string templateId, List<string> clientCodes = null);
        Task<BaseResponse<List<ConfigTemplateModel>>> AddOrUpdateConfigTemplate(string userId, ConfigTemplateModel request);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string userId, string id);
        Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigClient(string userId, List<string> clientCodes, ConfigTemplateLeagueRequest payload, bool isAssigned);
        #endregion

        #region TOP FIXTURES
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        #endregion

        Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies();
        Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request, List<string> brands = null);
    }
}
