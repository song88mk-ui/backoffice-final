using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services;

public interface IProviderMappingService
{
    Task<BaseResponse<UnmappedResponse>> GetUnmappedSports();
    Task<BaseResponse<UnmappedResponse>> GetUnmappedLeagues();
    Task<BaseResponse<LeagueMappingResponse>> GetLeaguesMapping(MappingRequest request);
    Task<BaseResponse<SportMappingResponse>> GetSportMapping(MappingRequest request);
    Task<BaseResponse<SportMappingItemModel>> GetSportMappingBySpId(int spId);
    Task<BaseResponse<LeagueMappingItemModel>> GetLeagueMappingByLeId(int leId);
    Task<BaseResponse<bool>> AddOrUpdateProviderMapping(string category, ProviderMappingRequest request);
    Task<BaseResponse<bool>> AddOrUpdateProviderMappingBulk(string category, ProviderMappingBulkRequest request);
}