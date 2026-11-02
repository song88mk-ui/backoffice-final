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
        Task<BaseResponse<ClientFeatureFlagsResponse>> GetClientFeatureFlags(string clientId);
        Task<BaseResponse<ClientFeatureFlagsResponse>> UpdateClientFeatureFlags(UpdateClientFeatureFlagsRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetMarketSettlementActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<MessageResponse>> AddActivityLog(ActivityLogModel request);
        Task<BaseResponse<MessageResponse>> AddActivityLogList(ActivityLogListRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementActivityLogs(ManualBetSettlementActivityLogRequest request);
        Task<BaseResponse<PlatformClientModel>> GetPlatformClients();
        Task<BaseResponse<ClientModel>> GetClients();
        Task<BaseResponse<ClientModel>> GetClientsV2(CancellationToken cancellationToken = default);
        Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request);
        Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request);
        Task<BaseResponse<ValidateClientImportResponse>> ValidateClientImport(ValidateClientImportRequest request);
        Task<BaseResponse<CommitClientImportResponse>> CommitClientImport(CommitClientImportRequest request);
        Task<BaseResponse<ClientImportProgressResponse>> GetClientImportProgress(Guid batchId);
        Task<BaseResponse<string>> GetClientImportEnvironment();
        Task<BaseResponse<List<ClientExportRow>>> ExportClients();
        Task<BaseResponse<ValidateClientUpdateResponse>> ValidateClientUpdate(ValidateClientUpdateRequest request);
        Task<BaseResponse<CommitClientUpdateResponse>> CommitClientUpdate(CommitClientUpdateRequest request);
        Task<BaseResponse<List<ClientFieldChangeResponse>>> GetClientImportFieldChanges(Guid batchId);
        Task<BaseResponse<List<ClientImportBatchSummary>>> GetClientImportBatchHistory(string operationType);
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
        //Task<BaseResponse<SportsLookupModel>> GetSportsAsync();
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
        Task<BaseResponse<ResendSettlementResponse>> ResendProviderCancellation(ResendProviderCancellationRequest request);
        Task<BaseResponse<BetModifyResponse>> CancelBet(BetModifyRequest request);
        Task<BaseResponse<bool>> DeactivateFixtureMarkets(DeactivateFixtureMarketRequest request);
        Task<BaseResponse<AnnouncementResponse>> GetAnnouncements(AnnouncementRequest request, CancellationToken cancellationToken);
        Task<BaseResponse<AnnouncementResponse>> AddOrUpdateAnnouncements(AddOrUpdateAnnouncementRequest request, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> DeleteAnnouncements(DeleteAnnouncementRequest request, CancellationToken cancellationToken);

        #region ConfigTemplate
        Task<BaseResponse<TranslationResponse>> GetTranslation(LanguageCode langCode);
        Task<BaseResponse<List<SportsbookTranslationResponse>>> GetSportsbookTranslations(string languageCode);
        Task<BaseResponse<TranslationStatsResponse>> GetSportsbookTranslationStats();
        Task<BaseResponse<bool>> AddOrUpdateSportsbookTranslation(AddOrUpdateSportsbookTranslationRequest request);
        Task<BaseResponse<bool>> DeleteSportsbookTranslation(DeleteSportsbookTranslationRequest request);
        Task<BaseResponse<bool>> DeleteSportsbookTranslationKey(DeleteSportsbookTranslationKeyRequest request);
        Task<BaseResponse<SportsbookGlossaryListResponse>> GetSportsbookGlossary(GetSportsbookGlossaryRequest request);
        Task<BaseResponse<TranslationStatsResponse>> GetSportsbookGlossaryStats();
        Task<BaseResponse<bool>> AddOrUpdateSportsbookGlossary(AddOrUpdateSportsbookGlossaryRequest request);
        Task<BaseResponse<bool>> SetSportsbookGlossaryProcessed(SetSportsbookGlossaryProcessedRequest request);
        Task<BaseResponse<bool>> DeleteSportsbookGlossary(DeleteSportsbookGlossaryRequest request);
        Task<BaseResponse<List<SportsbookGlossaryExportRow>>> ExportSportsbookGlossary(string searchText, string targetLanguage = null, bool untranslatedOnly = false, bool exactMatch = false);
        Task<BaseResponse<ImportSportsbookGlossaryResponse>> ImportSportsbookGlossary(ImportSportsbookGlossaryRequest request);
        Task<BaseResponse<List<SportsbookTranslationExportRow>>> ExportSportsbookTranslations();
        Task<BaseResponse<ImportSportsbookTranslationResponse>> ImportSportsbookTranslations(ImportSportsbookTranslationRequest request);

        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long configTemplateId);
        Task<BaseResponse<ConfigTemplateResponse>> GetConfigTemplate(string searchText, int sportId = -1, int scopeId = -1, CancellationToken cancellationToken = default);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetConfigMarketTemplateGroups(CancellationToken cancellationToken = default);
        Task<BaseResponse<MarketLookupResponse>> GetConfigMarketLookup(MarketLookupRequest request, CancellationToken cancellationToken = default);
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

        #region Selector Market Templates
        Task<BaseResponse<List<ClientSelectorMarketTemplateModel>>> GetClientSelectorMarketTemplates(Guid clientId);
        Task<BaseResponse<List<SelectorMarketTemplateLookupModel>>> GetSelectorMarketTemplateLookup();
        Task<BaseResponse<bool>> AddOrUpdateClientSelectorMarketTemplates(ClientSelectorMarketTemplateRequest request);
        Task<BaseResponse<bool>> CopyClientSelectorMarketTemplates(CopyClientSelectorMarketTemplateRequest request);
        #endregion

        #region Sport Settings
        Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request);
        Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request);
        Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request);
        Task<BaseResponse<List<ClientOddstableLeagueOrderListResponse>>> GetClientOddstableLeagueOrderList(ClientOddstableLeagueOrderListRequest request);
        Task<BaseResponse<ClientOddstableLeagueSettingResponse>> AddOrUpdateClientOddstableLeagueSettings(ClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ClientOddstableLeagueSettingResponse>> CopyClientOddstableLeagueSettings(CopyClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ImportClientOddstableLeagueSettingResponse>> ImportClientOddstableLeagueSettings(ImportClientOddstableLeagueSettingRequest request);
        Task<BaseResponse<ActivityLogResponse>> GetOddstableLeagueSortingLogs(OddstableLeagueSortingLogRequest request);
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
