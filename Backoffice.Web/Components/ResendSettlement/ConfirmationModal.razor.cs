using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Components;
using Backoffice.Shared.Enums;

namespace Backoffice.Web.Components.ResendSettlement
{
    public partial class ConfirmationModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        private List<ResendSettlementBetDetailModel> ResendSettlementModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ResendSettlementModel = ModalParameters.TryGet<List<ResendSettlementBetDetailModel>>("ResendSettlementModel");
        }

        public async Task OnModalCancel()
        {
            ModalService.Cancel();
        }

        public async Task OnModalConfirm()
        {
            if (ResendSettlementModel.Any())
            {
                var response = (await BackOfficeService.ResendSettlement(
                new ResendSettlementRequest()
                {
                    Bets = ResendSettlementModel.Select(x =>
                        new ResendSettlementRequest.ResendSettlementRequestData()
                        {
                            BetId = x.Bet.BetId.Value.ToString(),
                            CurrentStatus = (BetStatus)x.Bet.BetStatusId
                        }).ToList()
                }
                )).Result;

                if (response.Data != null && response.Data.Any())
                {

                }

                ModalService.Cancel();
            }
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
