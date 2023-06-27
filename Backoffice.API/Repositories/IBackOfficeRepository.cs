using Backoffice.Shared.Enums;
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
        Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplates(List<int> sportIds = null, List<int> scopeIds = null);
        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long templateId);
        Task<BaseResponse<bool>> AddOrUpdateConfigTemplate(ConfigTemplateModel request);
        Task<BaseResponse<bool>> SetConfigState(ConfigState configState, List<ConfigClientModel> request);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string id);
        Task<BaseResponse<TemplateConfigCount>> GetTemplateConfigCount(List<string> clientCodes);
        Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigState(List<string> clientCodes, ConfigTemplateLeagueRequest payload, ConfigState configState);
        #endregion

        #region TOP FIXTURES
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        #endregion

        Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies();
        Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request, List<string> brands = null);
    }
}
