using Backoffice.Web.Components.Maintenance;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Maintenance : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        private bool OldIsMaintenance { get; set; }
        public AlertModel AlertModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public bool IsPreLoader { get; set; }
        public bool IsMaintenance
        {
            get { return _isMaintenance; }
            set
            {
                if (value == _isMaintenance) return;
                OnMaintenanceClickEvent(value);
            }
        }

        #endregion

        #region Fields

        private bool _isMaintenance;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            //IsPreLoader = true;
            AlertModel = new AlertModel();

            PageTitle = "Maintenance";
            IconClass = "ico ico-maintenance";

            await GetContent();
        }

        public async Task OnMaintenanceClickEvent(bool value)
        {
            var parameters = new ModalParameters();
            parameters.Add("IsMaintenance", (bool)value);
            parameters.Add("OldIsMaintenance", OldIsMaintenance);
            parameters.Add("ModalDialogClass", "modal-dialog-centered");
            var options = new ModalOptions();

            ModalService.Open<ConfirmationModal>("Maintenance", parameters, options);
        }
        public async Task OnSaveClickedEvent()
        {
            if(_isMaintenance != OldIsMaintenance)
            {
                OldIsMaintenance = _isMaintenance;
            }
            IsPreLoader = false;
        }
        public void Dispose()
        {
        }
        #endregion

        #region Utilities

        private async Task GetContent()
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
            }

            var result = await BackOfficeService.GetMaintenance();

            if (result.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
            {
                _isMaintenance = result.Result;
                OldIsMaintenance = result.Result;
            }
            else
            {
                AlertModel = new AlertModel()
                {
                    Status = "Error",
                    Message = result.Message,
                    Opacity = 1m
                };
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
