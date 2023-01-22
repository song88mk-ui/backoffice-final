using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class HeaderConfigTopMenu : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        #endregion
        #region Methods
        public async Task OnImportExportMarginsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show modal-z-index");
            parameters.Add("ModalDialogClass", "modal-md");            

            var options = new ModalOptions();
            ModalService.Open<ImportExportMarginModal>($"Import/Export Margins", parameters, options);
        }

        public async Task OnLogsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ViewLogs>($"View Logs", parameters, options);
        }

        public async Task OnCopyClientSettingsClickEvent()
        {

        }
        public void Dispose()
        {
            
        }
        #endregion
    }
}
