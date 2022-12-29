using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Pages.OddsEngine;

namespace Backoffice.Web.Services
{
    public interface ITradingService
    {
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request);
        Task<BaseResponse<SportsLookupResponse>> GetSportLookup();
        Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId);
        Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSetting(BoostedSettingsModel request, string userId);
    }
}
