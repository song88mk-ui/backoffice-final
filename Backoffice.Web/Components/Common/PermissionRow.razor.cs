using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Components.Common
{
    public partial class PermissionRow : ComponentBase, IDisposable
    {
        #region Properties
        [Parameter] public List<PermissionModel> Permissions { get; set; }
        [Parameter] public PermissionTreeModel.PermissionClaim Claim { get; set; }
        [Parameter] public List<PermissionTreeModel.Group> Groups { get; set; }
        [Parameter] public bool IsAdmin { get; set; }
        [Parameter] public bool IsSubUser { get; set; }
        [Parameter] public int Availability { get; set; }
        #endregion

        #region Methods

        protected override Task OnParametersSetAsync()
        {
            Claim.IsReadOnly = Claim.IsReadOnly || (IsAdmin && Claim.ClaimType == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", "."));

            if (IsAdmin && Claim.ClaimType == SportsbookClientPermission.SBC_SubUsers.ToString().Replace("_", "."))
                Claim.ClaimValue = "0";

            return base.OnParametersSetAsync();
        }

        public async Task OnCheckboxClicked(ChangeEventArgs e, bool isReadOnlyClaim)
        {
            if (Convert.ToBoolean(e.Value) == true)
            {
                if (isReadOnlyClaim)
                {
                    Claim.ClaimValue = "2";
                }
                else
                {
                    Claim.ClaimValue = "1";
                }
            }
            else
            {
                Claim.ClaimValue = "0";
            }

            var permission = Permissions.FirstOrDefault(permission => permission.ClaimType == Claim.ClaimType);
            if (permission != null)
            {
                permission.ClaimValue = Claim.ClaimValue;
            }
        }
        public void Dispose()
        {
        }
        #endregion
    }
}
