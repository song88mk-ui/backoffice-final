using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class HeaderConfigTopMenu : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ToastService? _toasterService { get; set; }

        [Parameter] public Pages.OddsEngine.Configuration ConfigurationRef { get; set; }

        [Parameter] public string Region { get; set; }
        #endregion

        #region Fields
        private ToastService toasterService => _toasterService!;
        #endregion

        #region Methods
        public async Task OnImportExportMarginsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show modal-z-index");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("Region", Region);
            parameters.Add("OnExportMarginCallback", EventCallback.Factory.Create(this, ShowExportToast));
            parameters.Add("OnImportMarginCallback", EventCallback.Factory.Create<string>(this, ShowImportToast));

            var options = new ModalOptions();
            ModalService.Open<ImportExportMarginModal>($"Import/Export Margins", parameters, options);
        }

        public async Task OnLogsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle","max-width:80rem");
            parameters.Add("Region", Region);

            var options = new ModalOptions();
            ModalService.Open<ViewLogs>($"View Logs", parameters, options);
        }

        public async Task OnCopyClientSettingsClickEvent()
        {

            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("Region", Region);
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create(this, ConfigurationRef.OnCopyClientSettingsAlert));

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }        

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Utilities        
        private async Task ShowExportToast()
        {
            toasterService.AddToast(Toast.NewToast("Export Margins",
                $"Success! Excel file is being generated. System will notify once done"));
        }

        private async Task ShowImportToast(string message)
        {
            toasterService.AddToast(Toast.NewToast("Import Margins",
                $"{message}"));
        }        
        #endregion
    }
}
