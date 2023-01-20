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
        Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request, string region);
        Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request, string region);
        Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request, string region);
        Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request, string region);
        Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request, string region);

        #endregion

        #region Sportsbook
        Task<BaseResponse<MarketFixtureResponse>> GetFixtures(MarketFixtureRequest request, string region);
        Task<BaseResponse<MarketFixtureResponse>> GetOutrightFixtures(MarketFixtureRequest request, string region);
        #endregion

        #region Priceboost
        Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request, string region);
        Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request, string region);
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, string region);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSettings(BoostedSettingsModel request, string userId, string region);
        Task<BaseResponse<List<PriceBoostConfigAuditModel>>> GetPriceBoostAuditList(PriceBoostConfigRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        #endregion

        #region Activity Log
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request, string region);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId, string region);
        Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string region);
        Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region);
        Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region);
        Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region);
        #endregion

        #region Sports
        Task<BaseResponse<SportsTreeResponse>> GetSports(int fixtureTypeId, string region);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, string region);
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string region);
        Task<BaseResponse<SportSelectorMarketTemplateResponse>> GetSelectorMarketTemplates(string region);
        #endregion

        #region Segment
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string region);
        #endregion

        #region League
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string region);
        Task<BaseResponse<List<TierLeagueOrderResponse>>> GetTierLeagueOrderList(string region);
        #endregion

        #region Fixtures
        Task<BaseResponse<MarketFixtureResponse>> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string region);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId, string region);
        Task<BaseResponse<FixtureOddsHistoryResponse>> GetFixtureMarketSelections(int fixtureTypeId, string providerFixtureId, string providerMarketId, string providerSelectionId, string region);
        Task<BaseResponse<MarketFixtureDetailResponse>> GetFixtureMarketDetails(string providerFixtureId, string region);
        #endregion

        #region BET
        Task<BaseResponse<BetSelectionDetailsResponse>> GetBetSelections(BetSelectionDetailsRequest request, string region);
        Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request, string region);
        #endregion

        #region FASTODDS
        Task<BaseResponse<FastOddsSportsResponse>> GetFastOddsSports(int fixtureTypeId, string region);
        Task<BaseResponse<FastOddsFixturesResponse>> GetFixtures(FastOddsFixturesRequest request, string region);
        #endregion

        #region MANUAL
        Task<BaseResponse<ManualMarketSettlementResponse>> AddOrUpdateManualMarketSettlement(List<ManualMarketSettlementRequest> request, string userId, string region);
        Task<BaseResponse<ManualMarketSettlementDetailsResponse>> GetManualMarketSettlementDetails(ManualMarketSettlementDetailsRequest request, string region);
        Task<BaseResponse<string>> AddManualMessageSettlement(List<MessageSettlementRequest> request, string region);
        Task<BaseResponse<ManualMessageSettlementAuditResponse>> GetMessageSettlementAudit(ManualMessageSettlementRequest request, string region);
        #endregion

        Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region);
        Task<BaseResponse<BookmakerConfigurationResponse>> GetBookmakers(string region);
        Task<BaseResponse<Tuple<List<NotificationResponse>, MetaData>>> GetNotifications(NotificationsRequest request, string region);
        Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequest request, string region);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId, string region);
        Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(int fixtureTypeId, int sportId, string region);
    }
}
