using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageBoost : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage Boost";
            IconClass = string.Empty;
        }

        public async Task OnEdit()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();            
            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public async Task OnViewLog()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ViewLogsBoostedModal>($"View Logs", parameters, options);
        }

        public async Task OnDelete()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
