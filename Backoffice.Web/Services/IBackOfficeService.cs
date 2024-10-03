using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface IBackofficeService
    {
        Task<BaseResponse<bool>> GetMaintenance();
        Task<BaseResponse<bool>> SetMaintenance(bool request);
        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<MessageResponse>> AddActivityLog(ActivityLogModel request);
        Task<BaseResponse<MessageResponse>> AddActivityLogList(OnActivityLogListMessage request);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request, string region);
        Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementActivityLogs(ManualBetSettlementActivityLogRequest request);
        Task<BaseResponse<ClientModel>> GetClients();
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
        Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId, LanguageCode langCode);
        Task<BaseResponse<SportsLookupModel>> GetSportsAsync();
        Task<BaseResponse<ClientParlayBonusModel>> GetClientParlayBonus(GetParlayBonusRequest request);
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId, LanguageCode langCode);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request, LanguageCode langCode);
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

        #region ConfigTemplate
        Task<BaseResponse<TranslationResponse>> GetTranslation(LanguageCode langCode);
        Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long configTemplateId);
        Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplate(string searchText, int sportId = -1, int scopeId = -1);
        Task<BaseResponse<long>> AddOrUpdateConfigTemplate(ConfigTemplateModel request);
        Task<BaseResponse<bool>> SetLeagueState(ConfigState state, ConfigSetLeagueStateRequest payload);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(long id);
        Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigClient(ConfigState state, ConfigTemplateLeagueRequest payload, int templateId= -1);
        Task<BaseResponse<int>> GetTemplateConfigCount(string state = "", CancellationToken token = default);
        Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMargin(long configTemplateId);
        Task<BaseResponse<bool>> AddOrUpdateConfigTemplateMargin(ConfigTemplateMarginModel request);
        Task<BaseResponse<CopyClientConfigResponse>> CopyClientConfig(CopyClientConfigRequest request);
        #endregion

        void Dispose();
    }
}
