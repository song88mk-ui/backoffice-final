using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface IBackOfficeService
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
        Task<BaseResponse<ClientTopFixtureModel>> GetClientTopFixtures(GetClientTopFixtureRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostClientTopFixtures(PostClientTopFixtureRequest request);
        Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request);
        Task<BaseResponse<GetAllLeaguesModel>> GetAllLeaguesAsync(GetAllLeaguesRequest request);
        Task<BaseResponse<TopLeaguesModel>> GetTopLeaguesAsync(GetTopLeaguesRequest request);
        Task<BaseResponse<PlatformMessageModel>> PostTopLeaguesAsync(PostTopLeaguesRequest request);
        Task<BaseResponse<bool>> CopyTopLeaguesAsync(CopyTopLeaguesRequest request);        
        Task<BaseResponse<SportsTreeModel>> GetSportstreeByClientId(int clientId);
        Task<BaseResponse<SportsTreeResponse>> GetSportsTreeAsync(int fixtureTypeId);
        Task<BaseResponse<SportsLookupModel>> GetSportsAsync();
        Task<BaseResponse<ClientParlayBonusModel>> GetClientParlayBonus(GetParlayBonusRequest request);
        Task<BaseResponse<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequest request);
        Task<BaseResponse<List<MainMarketTemplateResponse>>> GetTopMarkets(int sportId);
        Task<BaseResponse<MarketFixtureResponse>> GetMarketFixtures(MarketFixtureRequest request);
        Task<BaseResponse<MarketFixtureDetailResponse>> GetMarketFixtureDetails(string providerFixtureId);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId);
        Task<BaseResponse<List<MarketTemplatePlayerPropsResponse>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequest request);
        Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request);
        Task<BaseResponse<ManualBetSettlementResponse>> ManualSettlement(ManualBetSettlementRequest request);        
        Task<BaseResponse<ResendSettlementBetDetailModel>> GetResendSettlementBetDetails(ResendSettlementBetDetailRequest request);
        Task<BaseResponse<ResendSettlementResponse>> ResendSettlement(ResendSettlementRequest request);
        Task<BaseResponse<BetModifyResponse>> CancelBet(BetModifyRequest request);
        Task<BaseResponse<bool>> DeactivateFixtureMarkets(DeactivateFixtureMarketRequest request);

        #region ConfigTemplate
        Task<BaseResponse<ConfigTemplateModel>> GetClientConfigTemplateById(string token, long configTemplateId);
        Task<BaseResponse<List<ConfigTemplateModel>>> GetClientConfigTemplate(string token);
        Task<BaseResponse<List<ConfigTemplateModel>>> AddOrUpdateConfigTemplate(string token, ConfigTemplateModel request);
        Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string token, string id);
        Task<BaseResponse<List<ConfigClientModel>>> GetConfigClient(string token, string type, MetaData metaData);
        Task<BaseResponse<TemplateConfigCount>> GetTemplateConfigCount(string token);
        Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMargin(string token, long configTemplateId);

        #endregion

        void Dispose();
    }
}
