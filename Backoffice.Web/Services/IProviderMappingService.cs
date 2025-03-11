using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services;

public interface IProviderMappingService
{
    Task<BaseResponse<UnmappedResponse>> GetUnmappedSports();
    Task<BaseResponse<bool>> AddOrUpdateProviderMapping(string category, ProviderMappingRequest request);
    Task<BaseResponse<bool>> AddOrUpdateProviderMappingBulk(string category, ProviderMappingBulkRequest request);
}