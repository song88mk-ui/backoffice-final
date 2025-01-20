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
            var userModel = new UserModel();

            var parsedToken = JwtParser.ParseClaimsFromJwt(accessToken);
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(parsedToken, "jwtAuthType"));
            var userClaims = claimsPrincipal.Claims.ToList();

            if (userClaims != null && userClaims.Any())
            {
                var otherPermissions = new List<string>() { "CLIENTCODES", "USERID", "TOKENNAME", "MAINUSERID" };

                userModel.Id = userClaims.Where(x => x.Type.ToUpper() == "USERID").FirstOrDefault()?.Value ?? "";
                userModel.Name = userClaims.Where(x => x.Type.ToUpper() == "TOKENNAME").FirstOrDefault()?.Value ?? "";
                userModel.MainUserId = userClaims.Where(x => x.Type.ToUpper() == "MAINUSERID").FirstOrDefault()?.Value ?? "";
                userModel.RoleId = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLEID")?.Value ?? string.Empty;
                userModel.RoleName = userClaims.FirstOrDefault(c => c.Type.ToUpper() == "ROLENAME")?.Value ?? string.Empty;
                userModel.ClientCodes = userClaims.Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                _isSuperAdmin = !string.IsNullOrEmpty(userModel.RoleName) && userModel.RoleName.ToUpper().Equals("SUPER ADMIN") ? true : false;

                userModel.Claims = userClaims.Where(x => !otherPermissions.Contains(x.Type.ToUpper())).ToList();
            }
            else
            {
                _isSuperAdmin = false;
            }

            base.Model = new UserModel()
            {
                Id = userModel.Id,
                Name = userModel.Name,
                MainUserId = userModel.MainUserId,
                AccessToken = accessToken,
                RoleId = userModel.RoleId,
                RoleName = userModel.RoleName,
                IsAuthenticated = !string.IsNullOrEmpty(accessToken),
                Claims = new List<Claim>(userModel.Claims),
                ClaimsPrincipal = claimsPrincipal
            };
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
