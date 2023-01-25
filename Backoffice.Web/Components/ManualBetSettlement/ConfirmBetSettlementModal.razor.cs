using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class ConfirmBetSettlementModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        private ManualBetDetailsModel ManualBetDetailsModel { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ManualBetDetailsModel = ModalParameters.TryGet<ManualBetDetailsModel>("ManualBetDetailsModel");
        }
        public async Task OnModalCancel()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");
            parameters.Add("ManualBetDetailsModel", ManualBetDetailsModel);

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public async Task OnModalConfirm()
        {
            var response = (await BackOfficeService.ManualSettlement(new ManualBetSettlementRequest()
            {
                BetData = ManualBetDetailsModel.Bets.Select(bet => new ManualBetRequest()
                {
                    BetId = bet.BetId.ToString(),
                    NewStatus = (BetStatus) bet.BetStatusId,
                    Reason = bet.Reason,
                }).ToList()                
            })).Result;

            if (response.IsSuccess)
            {
                
            }

            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
