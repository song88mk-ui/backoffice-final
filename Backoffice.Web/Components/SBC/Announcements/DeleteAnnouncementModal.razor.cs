using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using ActivityLogModel = Backoffice.Shared.Models.ActivityLogModel;

namespace Backoffice.Web.Components.SBC.Announcements
{
    public partial class DeleteAnnouncementModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }


        #endregion

        #region Fields
        private bool _isPreload;
        private AlertModel _alert;
        private CancellationTokenSource _cts;
        private Models.Announcements _announcement { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreload = false;
            _announcement = ModalParameters.TryGet<Models.Announcements>("Announcement");
        }

        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnDeleteModalClick()
        {
            _isPreload = true;
            await InvokeAsync(StateHasChanged);

            _cts = new CancellationTokenSource();

            var token = _cts.Token;

            var response = await BackofficeService.DeleteAnnouncements(new DeleteAnnouncementRequest
            {
                AnnouncementId = _announcement.Id
            }, token);

            if (response.StatusCode == StatusCode.Success)
            {
                _alert = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = $"Announcement {_announcement.Title} is successfully deleted."
                };

                var oldData = new
                {
                    Id = _announcement.Id,
                    Message = _announcement.Message,
                    LanguageCode = _announcement.LanguageCode,
                    Title = _announcement.Title,
                    StartDate = _announcement.StartDate,
                    EndDate =_announcement.EndDate,
                    Clients = _announcement.Clients
                };

                //activity log
                await BackofficeService.AddActivityLog(new ActivityLogModel
                {
                    Page = SportsbookClientPermission.SBC_Settings_Announcements.ToString().Replace("_", "."),
                    Payload = $"{_announcement.Id}.{_announcement.LanguageCode}",
                    Activity = Activity.Delete.ToString(),
                    From = JsonSerializer.Serialize(oldData),
                    To = "-",
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName
                });

                ModalService.Cancel();
                await Modal.OnSaveClickCallback.InvokeAsync(_announcement.Id);
            }
            else
            {
                _alert = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = $"Error in deleting {_announcement.Title}."
                };
            }

            _isPreload = false;
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
