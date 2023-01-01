using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared.OddsEngine
{
    public partial class OEMonitoringLayout : IDisposable
    {
        #region Properties
        public EventCallback<SportClickCallbackModel> NavigateSportEvent { get; set; }
        public SportClickCallbackModel DefaultSport { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            DefaultSport = new SportClickCallbackModel();

            NavigateSportEvent = EventCallback.Factory.Create<SportClickCallbackModel>(this, OnNavigateSportEvent);
        }
        private async Task OnNavigateSportEvent(SportClickCallbackModel model)
        {
            DefaultSport = model;
        }

        public async Task OnSportClickEvent(SportClickCallbackModel model)
        {

        }

        public void Dispose()
        {

        }

        #endregion

    }
}
