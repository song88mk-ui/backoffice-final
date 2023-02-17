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
        Task<BaseResponse<UserListResponse>> GetUserList(UserGroupRequest request, bool isShowSuperAdmin = false);
        Task<BaseResponse<LogInActivityLogResponse>> GetLogInActivityLogList(string userId, MetaData metaData, string searchText = "");
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request);
        Task<BaseResponse<PermissionResponse>> GetPermissionList(bool isSubUser);
        Task<BaseResponse<UserGroupListResponse>> GetUserGroupList(UserGroupListRequest request);
        Task<BaseResponse<UserListResponse>> GetUserGroupUsers(UserGroupRequest request);
        Task<BaseResponse<UserGroupResponse>> AddOrUpdateUserGroup(UserGroupRequest request);
        Task<BaseResponse<bool>> DeleteUserGroup(UserGroupRequest request);
        Task<BaseResponse<ReportRolesUrlResponse>> GetReportRolesUrl(ReportRolesUrlRequest request);
        void Dispose();
    }
}
