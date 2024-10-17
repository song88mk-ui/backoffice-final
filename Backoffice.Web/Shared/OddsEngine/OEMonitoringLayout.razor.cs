using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Shared;
using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout
    {
        #region Properties        
        public HeaderNavMenu HeaderNavMenuReference { get; set; }
        public SportClickCallbackModel SelectedSport { get; set; }
        private NavMenuParameter _navMenuParameter { get; set; } = new();

        #endregion

        #region Methods
        protected override void OnInitialized()
        {
            _navMenuParameter.NavMenuAccess = PermissionCategory.OE;
            base.InitializeState();
        }

        public void OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;
            HeaderNavMenuReference.ActivateMenu("monitoring");
        }
        #endregion
    }
}
