using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages.OddsEngine;

namespace Backoffice.Web.Services
{
    public interface ITradingService
    {
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup();
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId);
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId);

        Task<BaseResponse<List<ClientInformationModel>>> GetClientList();

        #region Priceboost
        Task<BaseResponse<List<PriceBoostConfigModel>>> GetPriceBoostConfig(PriceBoostConfigRequest request);
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSetting(BoostedSettingsModel request, string userId);
        Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request);
        #endregion


        Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request);
        Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request);
    }
}
