using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<BaseResponse<TokenModel>> Login(LogInModel loginModel);
        Task<BaseResponse<TokenModel>> Authenticate(string token);
        Task<BaseResponse<bool>> Logout(string token);
        Task<BaseResponse<UserListResponse>> GetUserList(UserGroupRequest request, string token, bool isShowSuperAdmin = false);
        Task<BaseResponse<LogInActivityLogResponse>> GetLogInActivityLogList(string userId, string search, MetaData metaData, string token);
        Task<BaseResponse<long>> Register(UserRegisterRequest request, string token);
        Task<BaseResponse<bool>> UpdateUser(UserRegisterRequest request, string token);
        Task<BaseResponse<bool>> UpdateUserPassword(string userId, UserRegisterRequest request, string token);
        Task<BaseResponse<UserResponse>> GetCurrentUser(string userId, string token);
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request, string token);
        Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUserOnly, string token);
        Task<BaseResponse<UserGroupListResponse>> GetUserGroupList(UserGroupListRequest request, string token);
        Task<BaseResponse<UserListResponse>> GetUserGroupUsers(UserGroupRequest request, string token);
        Task<BaseResponse<UserGroupResponse>> AddOrUpdateUserGroup(UserGroupRequest request, string token);
        Task<BaseResponse<bool>> DeleteUserGroup(UserGroupRequest request, string token);
        Task<BaseResponse<ReportRolesUrlResponse>> GetReportRolesUrl(ReportRolesUrlRequest request, string token);
    }
}
