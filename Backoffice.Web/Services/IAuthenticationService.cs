using Backoffice.Shared.Models;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<bool>> Login(LogInModel user);
        Task<bool> Logout();
    }
}
