using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout : IDisposable
    {
        #region Properties        
        public string? Region { get; set; }                
        #endregion

        #region Methods
        protected override void OnInitialized()
        {
            TranslationState.PropertyChanged += async (s, e) => await InvokeAsync(StateHasChanged);

            var page = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            var splitVal = page.Split("/").FirstOrDefault();

            if (splitVal != null)
            {
                var regions = new List<string>() { "HK", "SA" };
                Region = regions.Contains(splitVal.Trim().ToUpper()) ? splitVal.Trim().ToLower() : null;
            }
        }        
        #endregion
    }
}
