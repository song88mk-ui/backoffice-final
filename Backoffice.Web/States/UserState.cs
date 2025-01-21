using Azure.Core;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Shared;
using Backoffice.Web.States.Base;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
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
                    ClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>(),
                    Claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList(),
                    AccessToken = accessToken,
                    IsAuthenticated = !string.IsNullOrEmpty(accessToken),
                    ClaimsPrincipal = claimsPrincipal
                };

                _isSuperAdmin = !string.IsNullOrEmpty(base.Model.RoleName) && base.Model.RoleName.ToUpper().Equals("SUPER ADMIN") ? true : false;
            }

        }
        public void SetTradingClients(List<ClientInformationModel> clients)
        {
            base.Model.TradingClients = new List<ClientInformationModel>(clients);
            OnPropertyChanged();
        }
        public void SetSportsbookClients(List<Client> clients)
        {
            var sbClients = new List<Client>();

            if (base.Model.ClientCodes != null)
            {
                if (clients.Any())
                {
                    if (_isSuperAdmin)
                        base.Model.ClientCodes = clients.Select(c => c.ClientCode).ToList();
                    else
                    {
                        clients = clients.Where(c => base.Model.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                    }
                }

                sbClients = new List<Client>(clients);
            }

            base.Model.SportsbookClients = sbClients;
            OnPropertyChanged();
        }
        #endregion
    }
}
