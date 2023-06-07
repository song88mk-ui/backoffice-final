using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class PermissionRow : ComponentBase, IDisposable
    {
        #region Properties
        [Parameter] public List<PermissionModel> Permissions { get; set; }
        [Parameter] public PermissionTreeModel.PermissionClaim Claim { get; set; }
        [Parameter] public List<PermissionTreeModel.Group> Groups { get; set; }
        [Parameter] public bool IsReadOnly { get; set; }
        [Parameter] public bool IsSubUser { get; set; }
        [Parameter] public int Availability { get; set; }
        #endregion

        #region Methods
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
