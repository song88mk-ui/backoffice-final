using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManualBetSettlement
{
    public partial class ResetSettlementModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

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

        public async Task OnModalReset()
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem");

            ModalOptions options = new ModalOptions();

            ModalService.Open<AddReferenceModal>("Add Reference ID", parameters, options);
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
