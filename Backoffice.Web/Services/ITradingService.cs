using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Pages.OddsEngine;

namespace Backoffice.Web.Services
{
    public interface ITradingService
    {
        Task<BaseResponse<List<BoostedSettingsModel>>> GetBoostedSettings(BoostedSettingsRequest request);
        Task<BaseResponse<bool>> AddOrUpdateBoostedSetting(BoostedSettingsModel request, string userId);
    }
}
