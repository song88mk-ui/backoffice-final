using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<bool>> Login(LogInModel user);
        Task<TokenModel> AuthenticateToken();
        Task<bool> Logout();
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request);
    }
}
