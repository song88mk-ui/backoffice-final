using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class ConfirmCancelBetModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public CancelBetModel CancelBetModel { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            CancelBetModel = ModalParameters.TryGet<CancelBetModel>("CancelBetModel");
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmCancelClickEvent()
        {
            try
            {
                Alerts.Clear();
                
                //var response = (await AuthenticationService.DeleteUserGroup(new Backoffice.Shared.Models.Requests.UserGroupRequest
                //{
                //    UserGroupId = UserGroup.UserGroupId,
                //    UserGroupName = UserGroup.UserGroupName,
                //}));

                //if (response.Result)
                //{
                //    //activity log
                //    await BackOfficeService.AddActivityLog(new ActivityLogModel
                //    {
                //        Page = Page.Admin_ManageUserGroups.ToString().Replace("_", "."),
                //        Payload = $"{UserGroup.UserGroupId}",
                //        Activity = Activity.Delete.ToString(),
                //        From = JsonSerializer.Serialize(UserGroup),
                //        To = "-",
                //        UserId = UserModel.Id,
                //        UserName = UserModel.UserName,
                //    });


                //    Alerts.Add(new AlertModel
                //    {
                //        AlertStatus = "Success",
                //        AltertMessage = "User Group successfully deleted",
                //        Opacity = 1.0m
                //    });

                //    await InvokeAsync(StateHasChanged);
                //    await Task.Delay(3500);

                //    ModalService.Cancel();

                //    await Modal.OnSaveClickCallback.InvokeAsync();
                //}
                //else
                //{
                //    Alerts.Add(new AlertModel
                //    {
                //        AlertStatus = "Error",
                //        AltertMessage = $"{response.Message}",
                //        Opacity = 1.0m
                //    });
                //}
            }
            catch (Exception)
            {

            }
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
