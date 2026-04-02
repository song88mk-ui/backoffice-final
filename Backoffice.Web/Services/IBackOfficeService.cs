using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Events;

namespace Backoffice.Web.Services
{
    public interface IBackofficeService
    {
        string Page { get; set; }
        LanguageCode LanguageCode { get; set; }
        event EventHandler<UnauthorizedEventArgs> Unauthorized;
        Task<BaseResponse<MaintenanceResponse>> GetMaintenanceV2();
        Task<BaseResponse<MaintenanceResponse>> SetMaintenanceV2(SetMaintenanceRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<MessageResponse>> AddActivityLog(ActivityLogModel request);
        Task<BaseResponse<MessageResponse>> AddActivityLogList(ActivityLogListRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementActivityLogs(ManualBetSettlementActivityLogRequest request);
        Task<BaseResponse<PlatformClientModel>> GetPlatformClients();
        Task<BaseResponse<ClientModel>> GetClients();
        Task<BaseResponse<ClientModel>> GetClientsV2(CancellationToken cancellationToken = default);
        Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request);
        Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request);
        Task<BaseResponse<BetSettingResponse>> GetBetSetting(BetSettingRequest request);
        Task<BaseResponse<ExchangeRatesResponse>> GetExchangeRates();
        Task<BaseResponse<ClientTopFixtureModel>> GetClientTopFixtures(GetClientTopFixtureRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostClientTopFixtures(PostClientTopFixtureRequest request);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        Task<BaseResponse<GetAllLeaguesModel>> GetAllLeaguesAsync(GetAllLeaguesRequest request);
        Task<BaseResponse<TopLeaguesModel>> GetTopLeaguesAsync(GetTopLeaguesRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostTopLeaguesAsync(PostTopLeaguesRequest request);
        Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request);
        Task<BaseResponse<SportsTreeModel>> GetSportstreeByClientId(int clientId);
        Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId, LanguageCode langCode, CancellationToken cancellationToken = default);
        Task<BaseResponse<SportsLookupModel>> GetSportsAsync();
        Task<BaseResponse<ClientParlayBonusModel>> GetClientParlayBonus(GetParlayBonusRequest request);
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, LanguageCode langCode);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request, LanguageCode langCode);
        Task<BaseResponse<MarketFixtureResponseV2>> GetMostEvenLineMarketFixtures(MarketFixtureRequest request, LanguageCode langCode);
        Task<BaseResponse<MarketFixtureDetailResponse>> GetMarketFixtureDetails(string providerFixtureId, LanguageCode langCode);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId);
        Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequest request);
        Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request);
        Task<BaseResponse<ManualBetSettlementResponse>> ManualSettlement(ManualBetSettlementRequest request);
        Task<BaseResponse<ResendSettlementBetDetailModel>> GetResendSettlementBetDetails(ResendSettlementBetDetailRequest request);
        Task<BaseResponse<ResendSettlementResponse>> ResendSettlement(ResendSettlementRequest request);
        Task<BaseResponse<BetModifyResponse>> CancelBet(BetModifyRequest request);
        Task<BaseResponse<bool>> DeactivateFixtureMarkets(DeactivateFixtureMarketRequest request);
        Task<BaseResponse<AnnouncementResponse>> GetAnnouncements(AnnouncementRequest request, CancellationToken cancellationToken);
        Task<BaseResponse<AnnouncementResponse>> AddOrUpdateAnnouncements(AddOrUpdateAnnouncementRequest request, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> DeleteAnnouncements(DeleteAnnouncementRequest request, CancellationToken cancellationToken);

        #region ConfigTemplate
        Task<BaseResponse<TranslationResponse>> GetTranslation(LanguageCode langCode);
        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long configTemplateId);
        Task<BaseResponse<ConfigTemplateResponse>> GetConfigTemplate(string searchText, int sportId = -1, int scopeId = -1, CancellationToken cancellationToken = default);
        Task<BaseResponse<long>> AddOrUpdateConfigTemplate(ConfigTemplateModel request);
        Task<BaseResponse<bool>> SetLeagueState(ConfigState state, ConfigSetLeagueStateRequest payload);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(long id);
        Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigClient(ConfigState state, ConfigTemplateLeagueRequest payload, long templateId = -1, CancellationToken token = default);
        Task<BaseResponse<int>> GetTemplateConfigCount(string state = "", CancellationToken token = default);
        Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMargin(long configTemplateId);
        Task<BaseResponse<bool>> AddOrUpdateConfigTemplateMargin(ConfigTemplateMarginModel request);
        Task<BaseResponse<CopyClientConfigResponse>> CopyClientConfig(CopyClientConfigRequest request);
        #endregion

        #region Market Group
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(Guid clientId, int sportId, int fixtureStatusId);
        Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request);
        Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request);
        Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request);
        #endregion

        #region Market Sort Order
        Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, LanguageCode langCode);
        Task<BaseResponse<bool>> CopyClientMarketSortOrder(CopyClientMarketSortOrderRequest request);
        Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request);

        #endregion

        #region Sport Settings
        Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request);
        Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request);
        Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request);
        Task<BaseResponse<List<TierLeagueOrderModel>>> GetTierLeagueOrder();
        Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request);
        Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request);

        #endregion

        #region Lookup - Sport/Segment/League
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode);
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, LanguageCode langCode = LanguageCode.EN);
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, LanguageCode langCode = LanguageCode.EN);

        #endregion

        void Dispose();
    }
}
