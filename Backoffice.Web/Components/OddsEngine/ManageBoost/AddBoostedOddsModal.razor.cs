using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class AddBoostedOddsModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        #endregion
        #region Methods
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnNextModalClick()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");            

            var options = new ModalOptions();
            options.HasHeaderBackButton = true;
            //options.CallbackObject = AddPriceBoostFilterModel;
            //ModalService.Open<NextBoostModalBody>($"Add Boosted Odds - {(AddPriceBoostFilterModel.ShowMatchTypeMenu ? "Match Type" : "Outright")}", parameters, options);
            ModalService.Open<NextBoostModalBody>("Add Boosted Odds", parameters, options);
        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
