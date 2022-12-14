using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<BaseResponse<TokenModel>> Login(LogInModel loginModel);
        Task<BaseResponse<TokenModel>> Authenticate(string token);
        Task<BaseResponse<bool>> Logout(string token);
        Task<BaseResponse<bool>> Register(UserRegisterRequest request, string token);
        Task<BaseResponse<UserResponse>> GetCurrentUser(string userId, string token);
        Task<BaseResponse<UserListResponse>> GetSubAccounts(SubAccountListRequest request, string token);
    }
}
