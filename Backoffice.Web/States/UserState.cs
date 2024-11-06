using Azure.Core;
using Backoffice.Shared.Utilities;
using System.Security.Claims;

namespace Backoffice.Web.States
{
    public class UserState
    {
        #region Properties

        public string AccessToken { get { return _accessToken; } }
        public ClaimsPrincipal? ClaimsPrincipal { get { return _principal; } }
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

        #endregion

        #region Fields

        private string _accessToken;
        private ClaimsPrincipal? _principal;

        #endregion

        #region Methods

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;

            var parsedToken = JwtParser.ParseClaimsFromJwt(accessToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));
            
            _principal = claimsPrincipal;
        }
        #endregion
    }
}
