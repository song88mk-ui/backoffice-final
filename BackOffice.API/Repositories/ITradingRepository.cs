using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface ITradingRepository
    {

        #region Client
        Task<BaseResponse<List<ClientInformationModel>>> GetClientList(string region);
        Task<BaseResponse<ConfigModel>> GetClientConfig(ClientConfigRequest request, string region);
        Task<BaseResponse<ClientConfigResponse>> AddOrUpdateConfig(ConfigSettingsModel request, string region);
        Task<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>> GetConfigLogs(ConfigLogsRequest request, string region);
        Task<BaseResponse<List<ClientLeagueConfig>>> GetClientLeagueConfig(string clientCode, string region);
        Task<BaseResponse<List<ClientMarketConfig>>> GetClientMarketConfig(string clientCode, string region);
        Task<BaseResponse<List<ClientMarketConfig>>> GetClientMarketConfig(int pageSize, int page, string clientCode, string region);
        #endregion

        #region Sportsbook
        Task<BaseResponse<MarketFixtureResponse>> GetFixtures(MarketFixtureRequest request);
        Task<BaseResponse<MarketFixtureResponse>> GetOutrightFixtures(MarketFixtureRequest request);
        #endregion

        #region Priceboost
        Task<Tuple<BaseResponse<PriceBoostConfigResponse>, OnActivityLogListMessage>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region);
        Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region);
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, string region);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSettings(BoostedSettingsModel request, string userId, string region);
        Task<BaseResponse<List<PriceBoostConfigAuditModel>>> GetPriceBoostAuditList(PriceBoostConfigRequest request, string region);
        #endregion

        #region Activity Log
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        #endregion
    }
}
