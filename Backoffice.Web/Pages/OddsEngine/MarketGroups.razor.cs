using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Components.OddsEngine.MarketGroups;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MarketGroups : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Market Groups";
            IconClass = string.Empty;
        }

        public async Task OnMarketGroupAdd()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Add Market Group", parameters, options);
        }

        public async Task OnEditMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<AddOrEditMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnDeleteMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteMarketGroupModal>($"Delete Market Group", parameters, options);
        }

        public async Task OnCopyMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<CopyMarketGroupModal>($"Edit Market Group", parameters, options);
        }

        public async Task OnViewLogs()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<MarketGroupViewLogsModal>($"View Logs Market Group", parameters, options);
        }

        public async Task OnViewMarketGroup()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("DialogStyle", "top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<ViewMarketGroupModal>($"View Market Group", parameters, options);
        }        

        public void Dispose()
        {
        }

        #endregion
    }
}
