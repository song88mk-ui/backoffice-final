using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<Tuple<bool, List<string>>>> Login(LogInModel user);
        Task<TokenModel> AuthenticateToken();
        Task<bool> Logout();
        Task<BaseResponse<bool>> Register(UserRegisterRequest request);
        Task<BaseResponse<bool>> UpdateUser(UserRegisterRequest request);
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request);
        Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUser);
    }
}
