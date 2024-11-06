using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Services;
using System.Security.Claims;

namespace Backoffice.Web.States
{
    public class UserState
    {
        #region Properties

        public string AccessToken => _accessToken;
        public ClaimsPrincipal? ClaimsPrincipal => _principal;
        public List<ClientInformationModel> TradingClients => _tradingClients;
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

        #endregion

        #region Fields

        private string _accessToken;
        private ClaimsPrincipal? _principal;
        private List<ClientInformationModel> _tradingClients;

        #endregion

        #region Methods

        public async void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;

            var parsedToken = JwtParser.ParseClaimsFromJwt(accessToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));

            _principal = claimsPrincipal;
        }
        public void SetTradingClients(List<ClientInformationModel> clients)
        {
            _tradingClients = clients;
        }
        #endregion
    }
}
