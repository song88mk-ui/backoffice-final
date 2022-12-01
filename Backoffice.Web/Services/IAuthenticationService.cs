using Backoffice.Shared.Models;
using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<bool>> Login(UserModel user);
        Task<bool> Logout();
    }
}
