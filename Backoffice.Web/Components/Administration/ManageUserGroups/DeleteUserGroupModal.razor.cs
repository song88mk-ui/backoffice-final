using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class DeleteUserGroupModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public UserGroupResponse UserGroup { get; set; }
        public List<AlertModel> Alerts { get; set; }
        public decimal Opacity { get; set; }
        public bool IsPreload { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Opacity = 0m;
            Alerts = new List<AlertModel>();
            UserGroup = ModalParameters.TryGet<UserGroupResponse>("UserGroup");
        }

        public async Task OnCloseModalClickEvent()
        {
            if (IsPreload)
                return;

            ModalService.Cancel();
        }

        public async Task OnDeleteClickEvent()
        {
            try
            {
                if (IsPreload)
                    return;

                Opacity = Opacity == 1m ? 0.99m : 1m;

                Alerts.Clear();
                if (UserGroup.UserGroupName.ToLower().Equals("default"))
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = "Cannot delete this user group",
                        Opacity = Opacity
                    });
                    return;
                }

                var response = (await AuthenticationService.DeleteUserGroup(new Backoffice.Shared.Models.Requests.UserGroupRequest
                {
                    UserGroupId = UserGroup.UserGroupId,
                    UserGroupName = UserGroup.UserGroupName,
                }));

                if (response.Result)
                {
                    //activity log
                    await BackOfficeService.AddActivityLog(new ActivityLogModel
                    {
                        Page = AdminPermission.Admin_ManageUserGroups.ToString().Replace("_", "."),
                        Payload = $"{UserGroup.UserGroupId}",
                        Activity = Activity.Delete.ToString(),
                        From = JsonSerializer.Serialize(UserGroup),
                        To = "-",
                        UserId = UserModel.Id,
                        UserName = UserModel.UserName,
                    });

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync("DELETE");
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        Status = "Error",
                        Message = $"{response.Message}",
                        Opacity = Opacity
                    });
                }
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
