using Backoffice.Web.Models;
using Backoffice.Web.Pages.OddsEngine;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout : IDisposable
    {
        #region Properties        

        [Parameter] public SportClickCallbackModel SelectedSport { get; set; }
        #endregion

        #region Methods        

        public async Task OnSportClickEvent(SportClickCallbackModel model)
        {
            SelectedSport = model;

        }

        public void Dispose()
        {

        }

        #endregion

    }
}
