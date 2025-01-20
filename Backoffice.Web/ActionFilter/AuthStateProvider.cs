using Backoffice.Shared.Utilities;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Backoffice.Web.ActionFilter
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        [Inject] public NavigationManager NavigationManager { get; set; }
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthenticationState _anonymous;
        private readonly UserState _userState;

        public AuthStateProvider(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, UserState userState)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _userState = userState;

            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                if(string.IsNullOrEmpty(_userState.Model.AccessToken))
                    _userState.SetAccessToken(_httpContextAccessor.HttpContext?.Session.GetString("UserToken"));

                if (string.IsNullOrWhiteSpace(_userState.Model.AccessToken))
                    return _anonymous;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.Model.AccessToken);
                
                return new AuthenticationState(_userState.Model.ClaimsPrincipal);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
                return _anonymous;
            }
        }

        public void NotifyUserAuthentication(string email)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("UserToken");
            var claims = new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token), "jwtAuthType"));
            _ = claims.Claims.Append(new Claim(ClaimTypes.Name, email));

            var authState = Task.FromResult(new AuthenticationState(claims));

            NotifyAuthenticationStateChanged(authState);
        }

        public void NotifyUserLogout()
        {
            var authState = Task.FromResult(_anonymous);
            NotifyAuthenticationStateChanged(authState);
        }
    }
}
