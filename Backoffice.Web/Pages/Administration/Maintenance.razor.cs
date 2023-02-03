using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Components.Maintenance;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Server.IISIntegration;

namespace Backoffice.Web.Pages.Administration
{
    public partial class Maintenance : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public bool IsMaintenance { get; set; }
        private bool oldIsMaintenance { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public AlertModel AlertModel { get; set; }
        private ActivityLogModel _activityLog { get; set; }
        public bool IsPreLoader { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            _activityLog = new ActivityLogModel();
            AlertModel = new AlertModel();
            PageTitle = "Maintenance";
            IconClass = "ico ico-maintenance";

            await GetContent();
        }

        public async Task OnMaintenanceClickEvent(object value)
        {
            if ((bool)value == oldIsMaintenance)
                return;

            IsMaintenance = (bool)value;
            var parameters = new ModalParameters();
            parameters.Add("IsMaintenance", IsMaintenance);
            parameters.Add("ModalDialogClass", "modal-dialog-centered");
            var options = new ModalOptions();

            _activityLog = new ActivityLogModel()
            {
                Page = Page.Admin_Maintenance.ToString().Replace("_", "."),
                Payload = "",
                From = oldIsMaintenance.ToString(),
                To = IsMaintenance.ToString(),
                Activity = Activity.Update.ToString(),
                UserId = UserModel.Id,
                UserName = UserModel.UserName,
                TimeStamp = DateTime.UtcNow
            };

            ModalService.Open<ConfirmationModal>("Maintenance", parameters, options);
        }
        public async Task OnSaveClickedEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            var result = await BackOfficeService.SetMaintenance(IsMaintenance);

            if (result.StatusCode == StatusCode.Success)
            {
                await BackOfficeService.AddActivityLog(_activityLog);

                oldIsMaintenance = IsMaintenance;
                _activityLog = new ActivityLogModel();
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
            if(!IsPreLoader)
            {
                IsPreLoader = true;
                StateHasChanged();
            }    

            var result = await BackOfficeService.GetMaintenance();

            if (result.StatusCode == Backoffice.Shared.Enums.StatusCode.Success)
            {
                IsMaintenance = result.Result;
                oldIsMaintenance = result.Result;
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
