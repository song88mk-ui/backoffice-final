using Backoffice.Web.Components.Maintenance;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Maintenance : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Maintenance";
            IconClass = "ico ico-maintenance";
        }

        public async Task OnMaintenanceClickEvent(object value)
        {
            if ((bool)value)
            {
                var parameters = new ModalParameters();
                parameters.Add("ModalDialogClass", "modal-dialog-centered");
                var options = new ModalOptions();

                ModalService.Open<ConfirmationModal>("Maintenance", parameters, options);
            }
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
