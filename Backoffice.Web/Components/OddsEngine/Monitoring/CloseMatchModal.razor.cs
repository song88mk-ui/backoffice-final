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

        [Inject] public IBackofficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        private string _fixtureName { get; set; }
        private string _providerFixtureId { get; set; }
        private bool _isPreload { get; set; }
        #endregion        

        #region Methods
        protected override void OnInitialized()
        {
            _fixtureName = ModalParameters.TryGet<string>("FixtureName");
            _providerFixtureId = ModalParameters.TryGet<string>("ProviderFixtureId");
        }

        public void OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmClickEvent()
        {
            _isPreload = true;
            await Console.Out.WriteLineAsync("Test");

            var request = new DeactivateFixtureMarketRequest
            {
                ProviderFixtureId = _providerFixtureId,
                Raw = string.Empty,
            };

            var result = await BackOfficeService.DeactivateFixtureMarkets(request);

            await Modal.OnSaveClickCallback.InvokeAsync(new DeactivateFixtureCallbackModel
            {
                Result = result.Result,
                FixtureName = _fixtureName
            });

            ModalService.Cancel();

            _isPreload = false;
        }

        public void Dispose()
        {

        }
        #endregion        
    }
}
