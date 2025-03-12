using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services;

public interface IProviderMappingService
{
    Task<BaseResponse<UnmappedResponse>> GetUnmappedSports();
}