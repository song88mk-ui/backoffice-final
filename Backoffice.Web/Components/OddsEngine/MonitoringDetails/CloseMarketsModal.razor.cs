using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class CloseMarketsModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected MarketFixtureResponse.FixtureModel Fixture { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Fixture = ModalParameters.TryGet<MarketFixtureResponse.FixtureModel>("Fixture");

        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }


        public void Dispose()
        {

        }

        #endregion
    }
}
