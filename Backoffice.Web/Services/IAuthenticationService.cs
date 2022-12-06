using Backoffice.Shared.Models;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<bool>> Login(LogInModel user);
        Task<TokenModel> AuthenticateToken();
        Task<bool> Logout();
    }
}
