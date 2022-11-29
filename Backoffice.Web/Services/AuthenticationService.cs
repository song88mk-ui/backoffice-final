using Backoffice.Shared.Models;
using Backoffice.Web.ActionFilter;
using Backoffice.Web.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthenticationService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> Login(UserModel user)
        {
            var result = false;
            var bodyContent = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/authenticate", bodyContent);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var authContent = await response.Content.ReadAsStringAsync();
                var token = JsonSerializer.Deserialize<BaseResponse<TokenModel>>(authContent, _options);
                _httpContextAccessor.HttpContext.Session.SetString("UserToken", token.Result.AccessToken);
                //await _localStorage.SetItemAsync("authToken", token.Result.Token);
                ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(user.Name);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token.Result.AccessToken);
                result = true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {

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
