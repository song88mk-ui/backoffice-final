using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface ITradingService
    {
        string Region { get; set; }
        Task<BaseResponse<List<ClientInformationModel>>> GetClientList(CancellationToken cancellationToken = default);
        Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request);
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSetting(BoostedSettingsModel request, string userId);
        Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(AddPriceBoostFilterModel request);
        Task<BaseResponse<PriceBoostConfigResponse>> AddOrUpdatePriceBoost(List<PriceBoostConfigModel> request);
        Task<BaseResponse<MarketFixtureResponse>> GetSportsBookFixtures(MarketFixtureRequest request);
        Task<BaseResponse<MarketFixtureResponse>> GetSportsBookOutrightFixtures(MarketFixtureRequest request);

        //Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request);
        //Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request);
        //Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request);
        //Task<BaseResponse<BetSelectionDetailsResponse>> GetBetSelectionDetails(BetSelectionDetailsRequest request);
        Task<BaseResponse<ManualMarketSettlementDetailsResponse>> GetManualMarketSettlementDetails(ManualMarketSettlementDetailsRequest request);
        Task<BaseResponse<ManualMarketSettlementResponse>> AddOrUpdateManualMarketSettlement(List<ManualMarketSettlementRequest> request, string userId);
        Task<BaseResponse<ManualBetDetailsResponse>> GetManualBetDetails(ManualBetDetailsRequest request);
        Task<BaseResponse<string>> AddManualMessageSettlement(List<MessageSettlementRequest> request);
        Task<BaseResponse<ConfigModel>> GetClientConfig(ClientConfigRequest request);
        Task<BaseResponse<MarketTemplateGroupResponse>> GetMarketTemplateGroups(int fixtureTypeId);
        Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(MarketLookupRequest request);
        Task<BaseResponse<ClientConfigResponse>> AddOrUpdateConfig(ConfigSettingsModel request);
        Task<BaseResponse<Tuple<List<ConfigLogsResponse>, MetaData>>> GetConfigLogs(ConfigLogsRequest request);
        Task<BaseResponse<MessageResponse>> Publish(MessageRequest request);
        void Dispose();
    }
}
