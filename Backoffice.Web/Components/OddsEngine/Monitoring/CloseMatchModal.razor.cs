using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Drawing;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class CloseMatchModal : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        protected FixtureMarketModel Fixture { get; set; }
        protected bool IsPreload { get; set; }
        #endregion        

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Fixture = ModalParameters.TryGet<FixtureMarketModel>("Market");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmClickEvent()
        {
            IsPreload = true;

            var request = new DeactivateFixtureMarketRequest
            {
                ProviderFixtureId = Fixture.ProviderFixtureId,
                Raw = string.Empty,
            };

            var result = await BackOfficeService.DeactivateFixtureMarkets(request);

            await Modal.OnSaveClickCallback.InvokeAsync(new DeactivateFixtureCallbackModel
            {
                Result = result.Result,
                FixtureName = Fixture.FixtureName
            });

            ModalService.Cancel();

            IsPreload = false;
        }

        public void Dispose()
        {

        }

        #endregion        
    }
}
