using Backoffice.Shared.Enums;
using Backoffice.Web.Components.Administration.Maintenance;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Maintenance : IDisposable
    {
        #region Properties
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        

        private bool OldIsMaintenance { get; set; }
        public AlertModel AlertModel { get; set; }
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
        private PageHeaderModel _pageHeaderModel;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            AlertModel = new AlertModel();
            _pageHeaderModel = new()
            {
                Title = "Maintenance",
                IconClass = "ico ico-maintenance"
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();


            await GetContent();
        }

        public void OnMaintenanceClickEvent(bool value)
        {
            var parameters = new ModalParameters();
            parameters.Add("IsMaintenance", (bool)value);
            parameters.Add("OldIsMaintenance", OldIsMaintenance);
            parameters.Add("ModalDialogClass", "modal-dialog-centered");
            var options = new ModalOptions();

            ModalService.Open<ConfirmationModal>("Maintenance", parameters, options);
        }
        public void OnSaveClickedEvent()
        {
            if (_isMaintenance != OldIsMaintenance)
            {
                OldIsMaintenance = _isMaintenance;
            }
            IsPreLoader = false;
        }
        public void Dispose()
        {
            AlertModel = null;

            GC.SuppressFinalize(this);
        }
        #endregion

        #region Utilities

        private async Task GetContent()
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
            }

            var result = await BackofficeService.GetMaintenance();

            if (result.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
            {
                _isMaintenance = result.Result;
                OldIsMaintenance = result.Result;
            }
            else
            {
                AlertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = result.Message,
                };
            }

            IsPreLoader = false;
        }

        #endregion
    }
}
