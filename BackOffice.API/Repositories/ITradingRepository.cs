using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

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
        Task<BaseResponse<MarketFixtureResponse>> GetFixtures(MarketFixtureRequest request, string region);
        Task<BaseResponse<MarketFixtureResponse>> GetOutrightFixtures(MarketFixtureRequest request, string region);
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
        Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request, string region);
        Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request, string region);
        Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request, string region);
        Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request, string region);
        Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request, string region);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId, string region);
        Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string region);
        Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region);
        Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region);
        #endregion

        #region Sports
        Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, string region);
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string region);
        #endregion

        #region Segment
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string region);
        #endregion

        #region League
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string region);
        #endregion

        #region Fixtures
        Task<BaseResponse<MarketFixtureResponse>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region);
        Task<BaseResponse<FixtureOddsHistoryResponse>> GetFixtureMarketSelections(int fixtureTypeId, string providerFixtureId, string providerMarketId, string providerSelectionId, string region);
        Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region);
        #endregion
    }
}
