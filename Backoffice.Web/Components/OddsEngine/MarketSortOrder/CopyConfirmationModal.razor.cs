using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MarketSortOrder
{
    public partial class CopyConfirmationModal
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public string ClientName { get; set; }
        public string SportName { get; set; }
        public string FixtureStatusName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientName = ModalParameters.TryGet<string>("ClientName");
            SportName = ModalParameters.TryGet<string>("SportName");
            FixtureStatusName = ModalParameters.TryGet<string>("FixtureStatusName");
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnCopyClickEvent()
        {
            var fromClientId = ModalParameters.TryGet<Guid>("FromClientId");
            var toClientId = ModalParameters.TryGet<Guid>("ToClientId");

            var sportId = ModalParameters.TryGet<int>("SportId");
            var fixtureStatusId = ModalParameters.TryGet<int>("FixtureStatusId");

            var userId = ModalParameters.TryGet<string>("UserId");

            var request = new CopyClientMarketSortOrderRequest
            {
                CopyFromClientId = fromClientId,
                CopyFromSportId = sportId,
                CopyFromFixtureStatusId = fixtureStatusId,
                CopyToClientId = toClientId,
                CopyToSportId = sportId,
                CopyToFixtureStatusId = fixtureStatusId,
                UserId = userId
            };

            var response = await _backofficeService.CopyClientMarketSortOrder(request);

            if (response.StatusCode == StatusCode.Success)
            {
                ModalService.Cancel();
                await Modal.OnSaveClickCallback.InvokeAsync();
            }
        }

        #endregion
    }
}
