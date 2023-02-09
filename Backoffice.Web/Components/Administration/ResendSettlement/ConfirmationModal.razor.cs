using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models;

namespace Backoffice.Web.Components.Administration.ResendSettlement
{
    public partial class ConfirmationModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private List<ResendSettlementBetDetailModel> ResendSettlementModel { get; set; }
        private EventCallback<BaseResponse<ResendSettlementResponse>> OnResendSettlementCallback { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ResendSettlementModel = ModalParameters.TryGet<List<ResendSettlementBetDetailModel>>("ResendSettlementModel");
            OnResendSettlementCallback = ModalParameters.TryGet<EventCallback<BaseResponse<ResendSettlementResponse>>>("OnResendSettlementCallback");
        }

        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }

        public async Task OnModalConfirm()
        {
            if (ResendSettlementModel.Any())
            {
                var response = await BackOfficeService.ResendSettlement(new ResendSettlementRequest()
                {
                    Bets = ResendSettlementModel.Select(s =>
                        new ResendSettlementRequest.ResendSettlementRequestData()
                        {
                            BetId = s.Bet.BetId.Value.ToString(),
                            CurrentStatus = (BetStatus)s.Bet.BetStatusId
                        }).ToList()
                });

                ModalService.Cancel();

                await OnResendSettlementCallback.InvokeAsync(response);                
            }
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
