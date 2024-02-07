using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Activity = Backoffice.Shared.Enums.Activity;

namespace Backoffice.Web.Components.Administration.Maintenance
{
    public partial class ConfirmationModal
    {
        [Inject] IModalService ModalService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public AlertModel AlertModel { get; set; }
        public bool IsPreLoader { get; set; }
        public bool IsUpdating { get; set; }

        private bool _isMaintenance { get; set; }
        private bool _oldMaintenanceValue { get; set; }

        protected override async Task OnInitializedAsync()
        { 
            IsUpdating = false;
            AlertModel = new AlertModel();
            _isMaintenance = ModalParameters.TryGet<bool>("IsMaintenance");
            _oldMaintenanceValue = ModalParameters.TryGet<bool>("OldIsMaintenance");
        }
        public async Task OnSaveClickedEvent()
        {
            IsPreLoader = true;
            StateHasChanged();

            await SetMaintenance();
        }

        #region Utilities
        private async Task SetMaintenance()
        {
            IsUpdating = true;
            var timer = new Stopwatch();

            timer.Start();
            var result = await BackofficeService.SetMaintenance(_isMaintenance);

            if (result.StatusCode == StatusCode.Success)
            {

                var activityLog = new ActivityLogModel()
                {
                    Page = AdminPermission.Admin_Maintenance.ToString().Replace("_", "."),
                    Payload = "",
                    From = _oldMaintenanceValue.ToString(),
                    To = _isMaintenance.ToString(),
                    Activity = Activity.Update.ToString(),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow
                };

                await BackofficeService.AddActivityLog(activityLog);

                timer.Stop();
                AlertModel = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = StatusCode.Success.ToString(),
                    Opacity = 1m
                };

                Thread.Sleep(1500);
                await Modal.OnSaveClickCallback.InvokeAsync();
                ModalService.Cancel();
            }
            else
            {
                timer.Stop();
                AlertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = result.Message,
                    Opacity = 1m
                };
                IsUpdating = false;
            }

            IsPreLoader = false;
        }
        #endregion
    }
}
