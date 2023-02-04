using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class HeaderConfigTopMenu : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ToastService? _toasterService { get; set; }

        public List<AlertModel> Alerts { get; set; }
        
        private Guid Guid { get; set; }        
        #endregion

        #region Fields
        private ToastService toasterService => _toasterService!;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Guid = new Guid();
            Alerts = new List<AlertModel>();
        }

        public async Task OnImportExportMarginsClickEvent()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show modal-z-index");
            parameters.Add("ModalDialogClass", "modal-md");
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

            var options = new ModalOptions();
            ModalService.Open<ViewLogs>($"View Logs", parameters, options);
        }

        public async Task OnCopyClientSettingsClickEvent()
        {

            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center");
            parameters.Add("ModalDialogClass", "modal-md");
            parameters.Add("OnCopyClientSettingsCallback", EventCallback.Factory.Create(this, OnCopyClientSettingsAlert));

            var options = new ModalOptions();
            ModalService.Open<CopyClientSettingsModal>($"Copy Client Settings", parameters);
        }        

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        public async Task ShowExportToast()
        {
            toasterService.AddToast(Toast.NewToast("Export Margins",
                $"Success! Excel file is being generated. System will notify once done"));
        }

        public async Task ShowImportToast(string message)
        {
            toasterService.AddToast(Toast.NewToast("Import Margins",
                $"{message}"));
        }

        public async Task OnCopyClientSettingsAlert()
        {
            Alerts.Clear();            

            Alerts.Add(new AlertModel()
            {
                Status = AlertStatus.Success.ToString(),
                Message = "Success! Copy Client Settings is being generated. System will notify once done.",
                Opacity = 1.0m
            });            

            Guid = Guid.NewGuid();
        }
        #endregion
    }
}
