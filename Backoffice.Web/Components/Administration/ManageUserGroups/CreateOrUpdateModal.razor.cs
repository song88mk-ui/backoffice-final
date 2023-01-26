using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using System.Text.Json;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public UserGroupResponse UserGroup { get; set; }
        public UserGroupResponse OldUserGroup { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ClientModel ClientModel { get; set; }
        public bool IsEdit { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            UserGroup = ModalParameters.TryGet<UserGroupResponse>("UserGroup");
            Permissions = ModalParameters.TryGet<PermissionResponse>("Permissions");
            ClientModel = ModalParameters.TryGet<ClientModel>("ClientModel");

            OldUserGroup = new UserGroupResponse
            {
                UserGroupId = UserGroup.UserGroupId,
                UserGroupName = UserGroup.UserGroupName,
                Num = UserGroup.Num,
            };

            IsEdit = ModalParameters.TryGet<bool>("IsEdit");


            if(UserGroup.Claims.Any())
            {
                foreach(var claim in UserGroup.Claims)
                {
                    var permission = Permissions.Permissions.FirstOrDefault(p => p.ClaimType == claim.ClaimType);
                    if(permission != null)
                    {
                        permission.ClaimValue = claim.ClaimValue;
                    }
                    OldUserGroup.Claims.Add(new ClaimModel
                    {
                        ClaimType = claim.ClaimType,
                        ClaimValue = claim.ClaimValue
                    });
                }
            }
        }

        public async Task OnCancelClickEvent()
        {
            UserGroup = new UserGroupResponse
            {
                Num = OldUserGroup.Num,
                UserGroupId = OldUserGroup.UserGroupId,
                UserGroupName = OldUserGroup.UserGroupName,
                Claims = OldUserGroup.Claims 
            };

            ModalService.Cancel();
        }

        public async Task OnSaveClickEvent()
        {
            try
            {
                Alerts.Clear();

                if (string.IsNullOrEmpty(UserGroup.UserGroupName))
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = "User Group Name is required",
                        Opacity = 1.0m
                    });
                }

                if (UserGroup.UserGroupName.ToLower().Equals("super admin") || UserGroup.UserGroupName.ToLower().Equals("superadmin"))
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = "Invalid User Group Name ",
                        Opacity = 1.0m
                    });
                }

                if (Alerts.Any())
                    return;

                var claims = new List<ClaimModel>();

                foreach(var permission in Permissions.Permissions)
                {
                    if (string.IsNullOrEmpty(permission.ClaimValue))
                        permission.ClaimValue = "0";

                    claims.Add(new ClaimModel
                    {
                        ClaimType = permission.ClaimType,
                        ClaimValue = permission.ClaimValue
                    });                    
                }

                var request = new UserGroupRequest()
                {
                    UserGroupId = UserGroup.UserGroupId,
                    UserGroupName = UserGroup.UserGroupName,
                    Claims = claims
                };

                var response = (await AuthenticationService.AddOrUpdateUserGroup(request));

                if (response.Result)
                {
                    UserGroup.Claims = claims;

                    await BackOfficeService.AddActivityLog(new ActivityLogModel
                    {
                        Page = Page.Admin_ManageUserGroups.ToString().Replace("_", "."),
                        Payload = $"{request.UserGroupId}",
                        Activity = !IsEdit ? Activity.Create.ToString() : Activity.Update.ToString(),
                        From = !IsEdit ? "-" : JsonSerializer.Serialize(OldUserGroup),
                        To = JsonSerializer.Serialize(UserGroup),
                        UserId = UserModel.Id,
                        UserName = UserModel.UserName
                    });

                    if (IsEdit)
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Success",
                            AltertMessage = "User Group successfully updated",
                            Opacity = 1.0m
                        });
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Success",
                            AltertMessage = "User Group successfully created",
                            Opacity = 1.0m
                        });
                    }


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
            catch (Exception ex)
            {

            }
        }


        public void Dispose()
        {

        }

        #endregion
    }
}
