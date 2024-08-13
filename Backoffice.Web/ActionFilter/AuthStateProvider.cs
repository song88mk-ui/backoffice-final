using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
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

        public AuthStateProvider(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("UserToken");
                if (string.IsNullOrWhiteSpace(token))
                    return _anonymous;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var parsedToken = JwtParser.ParseClaimsFromJwt(token);
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));
                var claimsIdentity = new ClaimsIdentity(parsedToken, "jwtAuthType");

                return new AuthenticationState(claimsPrincipal);
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
