using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Shared;
using Backoffice.Web.States.Base;
using System.Data;
using System.Security.Claims;

namespace Backoffice.Web.States
{
    public class UserState : BaseState<UserModel>
    {
        #region Properties

        public UserModel Model => base.Model;
        public TimeSpan TimeZoneOffset { get; set; }
        public string ConnectionId { get; set; }

        #endregion

        #region Fields

        private bool _isSuperAdmin;
        private List<string> _clientCodes { get; set; } = new List<string>();

        #endregion

        #region Ctor
        public UserState() : base(new UserModel())
        {
        }
        #endregion

        #region Methods

        public async void SetAccessToken(string accessToken)
        {
            base.Model = new UserModel();
            var parsedToken = JwtParser.ParseClaimsFromJwt(accessToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));
            var userClaims = claimsPrincipal.Claims.ToList();
            _isSuperAdmin = false;

            if (userClaims != null && userClaims.Any())
            {
                var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                base.Model = new UserModel()
                {
                    Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "",
                    Name = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "",
                    MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "",
                    RoleId = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLEID")?.Value ?? string.Empty,
                    RoleName = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty,
                    Claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList(),
                    AccessToken = accessToken,
                    IsAuthenticated = !string.IsNullOrEmpty(accessToken),
                    ClaimsPrincipal = claimsPrincipal
                };

                _clientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();

                _isSuperAdmin = !string.IsNullOrEmpty(base.Model.RoleName) && base.Model.RoleName.ToUpper().Equals("SUPER ADMIN") ? true : false;
            }

        }
        public void SetTradingClients(List<ClientInformationModel> clients)
        {
            if (clients != null && clients.Any())
            {
                if (!_isSuperAdmin)
                    clients = clients.Where(c => _clientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                base.Model.TradingClients = new List<ClientInformationModel>(clients);
            }
            else
                base.Model.TradingClients = new List<ClientInformationModel>();

            OnPropertyChanged();
        }
        public void SetSportsbookClients(List<Client> clients)
        {
            if (clients != null && clients.Any())
            {
                if (!_isSuperAdmin)
                    clients = clients.Where(c => _clientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                base.Model.SportsbookClients = new List<Client>(clients);
            }
            else
                base.Model.SportsbookClients = new List<Client>();

            OnPropertyChanged();
        }
        #endregion
    }
}
