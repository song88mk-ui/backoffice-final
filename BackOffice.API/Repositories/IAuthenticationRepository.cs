using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<BaseResponse<TokenModel>> Login(LogInModel loginModel);
        Task<BaseResponse<TokenModel>> Authenticate(string token);
        Task<BaseResponse<bool>> Logout(string token);
    }
}
