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
        [Inject] private IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private List<ResendSettlementBetDetailModel> ResendSettlementModel { get; set; }
        private EventCallback<BaseResponse<ResendSettlementResponse>> OnResendSettlementCallback { get; set; }

        // BO-125: the same dialog confirms "Resend Cancellation to Bet Genius", which sends the bet ids to the
        // provider-cancellation endpoint instead of resending the settlement to the operator.
        private bool IsProviderCancellation { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ResendSettlementModel = ModalParameters.TryGet<List<ResendSettlementBetDetailModel>>("ResendSettlementModel");
            OnResendSettlementCallback = ModalParameters.TryGet<EventCallback<BaseResponse<ResendSettlementResponse>>>("OnResendSettlementCallback");
            IsProviderCancellation = ModalParameters.TryGet<bool>("IsProviderCancellation");
        }

        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }

        public async Task OnModalConfirm()
        {
            if (ResendSettlementModel.Any())
            {
                var response = IsProviderCancellation
                    ? await BackofficeService.ResendProviderCancellation(new ResendProviderCancellationRequest()
                    {
                        BetIds = ResendSettlementModel.Select(s => s.Bet.BetId.Value.ToString()).ToList()
                    })
                    : await BackofficeService.ResendSettlement(new ResendSettlementRequest()
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
