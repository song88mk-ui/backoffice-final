using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout
    {
        #region Properties        
        public string? Region { get; set; }
        public HeaderNavMenu HeaderNavMenuReference { get; set; }
        public SportClickCallbackModel SelectedSport { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            var splitVal = page.Split("/").FirstOrDefault();

            if (splitVal != null)
            {
                var regions = new List<string>() { "HK", "SA" };
                Region = regions.Contains(splitVal.Trim().ToUpper()) ? splitVal.Trim().ToLower() : null;
            }

            base.InitializeState();
            await Task.CompletedTask;
        }
        public void OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;
            HeaderNavMenuReference.ActivateMenu("monitoring");
        }
        #endregion
    }
}
