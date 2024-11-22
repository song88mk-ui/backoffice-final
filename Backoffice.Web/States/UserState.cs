using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using System.Security.Claims;

namespace Backoffice.Web.States
{
    public class UserState
    {
        #region Properties

        public string AccessToken => _accessToken;
        public ClaimsPrincipal? ClaimsPrincipal => _principal;
        public List<ClientInformationModel> TradingClients => _tradingClients;
        public List<Models.Client> SportsbookClients => _sportsbookClients;
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

        #endregion

        #region Fields

        private string _accessToken;
        private ClaimsPrincipal? _principal;
        private List<ClientInformationModel> _tradingClients;
        private List<Models.Client> _sportsbookClients;

        #endregion

        #region Methods

        public void SetAccessToken(string accessToken)
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
        public void SetSportsbookClients(List<Models.Client> clients)
        {
            if(_sportsbookClients == null)
            {
                _sportsbookClients = clients;
            }
        }
        #endregion
    }
}
