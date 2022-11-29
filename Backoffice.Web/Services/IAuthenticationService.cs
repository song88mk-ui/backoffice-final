using Backoffice.Web.Models;

namespace Backoffice.Web.Services
{
    public interface IAuthenticationService
    {
        Task<bool> Login(UserModel user);
        Task<bool> Logout();
    }
}
