using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models.Requests;

namespace Backoffice.Web.Services;

public interface IProviderMappingService
{
    Task<BaseResponse<UnmappedResponse>> GetUnmappedSports();
    Task<BaseResponse<SportMappingResponse>> GetSportMapping(MappingRequest request);
    Task<BaseResponse<bool>> AddOrUpdateProviderMapping(string category, ProviderMappingRequest request);
    Task<BaseResponse<bool>> AddOrUpdateProviderMappingBulk(string category, ProviderMappingBulkRequest request);
}