using Backoffice.Shared.Models;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthenticationService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<BaseResponse<bool>> Login(LogInModel user)
        {
            var result = new BaseResponse<bool>
                {
                    StatusCode = Backoffice.Shared.Enums.StatusCode.Error,
                    Message = Backoffice.Shared.Enums.StatusCode.Error.ToString(),
                    Result = false
                }; ;
            var bodyContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/authenticate", bodyContent);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<BaseResponse<TokenModel>>(authPayload);
                _httpContextAccessor.HttpContext?.Session.SetString("UserToken", token.Result.AccessToken);
                //await _localStorage.SetItemAsync("authToken", token.Result.Token);
                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(user.Name);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token.Result.AccessToken);
                result = new BaseResponse<bool>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = true
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var authPayload = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<BaseResponse<TokenModel>>(authPayload); 
                result = new BaseResponse<bool>
                {
                    StatusCode = token.StatusCode,
                    Message = token.Message,
                    Result = false
                };
            }

            return result;
        }

        public async Task<bool> Logout()
        {
            _httpContextAccessor.HttpContext.Session.Remove("UserToken");
            ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            return true;
        }
    }
}
