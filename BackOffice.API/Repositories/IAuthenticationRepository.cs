using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<BaseResponse<TokenModel>> Login(LogInModel loginModel);
        Task<BaseResponse<TokenModel>> Authenticate(string token);
        Task<BaseResponse<bool>> Logout(string token);
        Task<BaseResponse<ManageUserListResponse>> GetUserList(string token, string search = "", bool isShowSuperAdmin = false);
        Task<BaseResponse<bool>> Register(UserRegisterRequest request, string token);
        Task<BaseResponse<bool>> UpdateUser(UserRegisterRequest request, string token);
        Task<BaseResponse<UserResponse>> GetCurrentUser(string userId, string token);
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request, string token);
        Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUserOnly, string token);
        Task<BaseResponse<UserGroupListResponse>> GetUserGroupList(UserGroupListRequest request, string token);
        Task<BaseResponse<List<UserResponse>>> GetUserGroupUsers(UserGroupRequest request, string token);
        Task<BaseResponse<bool>> AddOrUpdateUserGroup(UserGroupRequest request, string token);
        Task<BaseResponse<bool>> DeleteUserGroup(UserGroupRequest request, string token);
    }
}
