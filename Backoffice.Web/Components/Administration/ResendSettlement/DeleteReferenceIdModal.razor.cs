using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.ResendSettlement
{
    public partial class DeleteReferenceIdModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private ResendSettlementBetDetailModel Bet { get; set; }
        private EventCallback<ResendSettlementBetDetailModel> DeleteBetCallback { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Bet = ModalParameters.TryGet<ResendSettlementBetDetailModel>("ResendSettlementBetDetailModel");
            DeleteBetCallback = ModalParameters.TryGet<EventCallback<ResendSettlementBetDetailModel>>("OnBetDeleteCallback");
        }

        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }

        public async Task OnModalDelete()
        {
            await DeleteBetCallback.InvokeAsync(Bet);

            ModalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
