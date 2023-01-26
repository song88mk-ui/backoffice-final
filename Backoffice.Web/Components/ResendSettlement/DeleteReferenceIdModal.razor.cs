using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ResendSettlement
{
    public partial class DeleteReferenceIdModal : IDisposable
    {
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        private ResendSettlementBetDetailModel Bet { get; set; }
        private EventCallback<ResendSettlementBetDetailModel> DeleteBetCallback { get; set; }
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
