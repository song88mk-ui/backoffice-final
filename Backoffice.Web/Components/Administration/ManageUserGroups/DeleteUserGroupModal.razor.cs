using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class DeleteUserGroupModal : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public UserGroupResponse UserGroup { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            UserGroup = ModalParameters.TryGet<UserGroupResponse>("UserGroup");
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnDeleteClickEvent()
        {
            try
            {
                Alerts.Clear();

                var response = (await AuthenticationService.DeleteUserGroup(new Backoffice.Shared.Models.Requests.UserGroupRequest
                {
                    UserGroupId = UserGroup.UserGroupId,
                    UserGroupName = UserGroup.UserGroupName,
                }));

                if (response.Result)
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Success",
                        AltertMessage = "User Group successfully deleted",
                        Opacity = 1.0m
                    });

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"{response.Message}",
                        Opacity = 1.0m
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
