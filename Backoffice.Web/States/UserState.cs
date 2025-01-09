using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Backoffice.Web.States
{
    public class UserState
    {
        #region Properties

        public string Id => _id.ToUpper();
        public string Name => _name;
        public string MainUserId => _mainUserId;
        public string AccessToken => _accessToken;
        public string RoleId => _roleId;
        public string RoleName => RoleName;
        public ClaimsPrincipal? ClaimsPrincipal => _principal;
        public List<string> ClientCodes => _clientCodes ?? new List<string>();
        public List<ClientInformationModel> TradingClients => _tradingClients ?? new List<ClientInformationModel>();
        public List<Claim> Claims => _claims;
        public List<Client> SportsbookClients => _sportsbookClients ?? new List<Client>();
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
        public TimeSpan TimeZoneOffset { get; set; }
        public string ConnectionId { get; set; }

        #endregion

        #region Fields

        private string _id = string.Empty;
        private string _mainUserId = string.Empty;
        private string _name = string.Empty;
        private string _accessToken = string.Empty;
        private string _roleId = string.Empty;
        private string _roleName = string.Empty;
        private ClaimsPrincipal? _principal;
        private List<string>? _clientCodes;
        private List<ClientInformationModel>? _tradingClients;
        private List<Claim> _claims;
        private List<Client>? _sportsbookClients;
        private bool _isSuperAdmin;
        private List<Client> _clientReference;

        public event PropertyChangedEventHandler? PropertyChanged = null!;

        #endregion

        #region Methods

        public async void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;

            var parsedToken = JwtParser.ParseClaimsFromJwt(accessToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));

            _principal = claimsPrincipal;

            var userClaims = claimsPrincipal.Claims.ToList();

            if (userClaims != null && userClaims.Any())
            {
                var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                _id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                _name = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                _mainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                _roleId = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLEID")?.Value ?? string.Empty;
                _roleName = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty;
                _clientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                _isSuperAdmin = !string.IsNullOrEmpty(_roleName) && _roleName.ToUpper().Equals("SUPER ADMIN") ? true : false;

                _claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList();
            }
            else
            {
                _id = string.Empty;
                _name = string.Empty;
                _mainUserId = string.Empty;
                _roleId = string.Empty;
                _roleName = string.Empty;
                _clientCodes = new List<string>();
                _isSuperAdmin = false;

                _claims = new List<Claim>();
                _sportsbookClients = new List<Client>();
            }

            OnPropertyChanged();
        }
        public void SetTradingClients(List<ClientInformationModel> clients)
        {
            _tradingClients = new List<ClientInformationModel>(clients);
            OnPropertyChanged();
        }
        public void SetSportsbookClients(List<Client> clients)
        {
            if (_sportsbookClients == null && _clientCodes != null)
            {
                if (clients.Any())
                {
                    if (_isSuperAdmin)
                        _clientCodes = clients.Select(c => c.ClientCode).ToList();
                    else
                    {
                        clients = clients.Where(c => _clientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                    }
                }

                _sportsbookClients = clients;
            }
            OnPropertyChanged();
        }
        protected async void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
