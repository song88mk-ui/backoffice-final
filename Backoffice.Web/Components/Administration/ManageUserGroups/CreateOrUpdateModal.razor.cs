using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public UserGroupResponse UserGroup { get; set; }
        public UserGroupResponse OldUserGroup { get; set; }
        public PermissionResponse Permissions { get; set; }
        public ClientModel ClientModel { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public bool IsEdit { get; set; }
        public decimal Opacity { get; set; }
        public bool IsPreload { get; set; }
        public bool IsClientEmpty { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Opacity = 0m;
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

            if (UserGroup.Claims.Any())
            {
                foreach (var permission in Permissions.Permissions)
                {
                    var userClaim = UserGroup.Claims.FirstOrDefault(x => x.ClaimType == permission.ClaimType);

                    var claimValue = "0";
                    if (userClaim != null)
                        claimValue = userClaim.ClaimValue;

                    OldUserGroup.Claims.Add(new ClaimModel
                    {
                        ClaimType = permission.ClaimType,
                        ClaimValue = claimValue
                    });

                    var permissionModel = Permissions.Permissions.FirstOrDefault(p => p.ClaimType == permission.ClaimType);
                    if (permissionModel != null)
                    {
                        permissionModel.ClaimValue = claimValue;
                    }
                }
            }
        }

        public async Task OnCancelClickEvent()
        {
            if (IsPreload)
                return;

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
                if (IsPreload)
                    return;

                Alerts.Clear();
                Opacity = Opacity == 1m ? 0.99m : 1m;

                if (string.IsNullOrEmpty(UserGroup.UserGroupName))
                {
                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = "User Group Name is required",
                        Opacity = Opacity
                    });
                }
                else if (UserGroup.UserGroupName.ToLower().Equals("super admin") || UserGroup.UserGroupName.ToLower().Equals("superadmin"))
                {
                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = "Invalid User Group Name ",
                        Opacity = Opacity
                    });
                }
                else if (UserGroup.UserGroupName.IndexOfAny(new[] { '<', '>' }) >= 0)
                {
                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = "User Group Name must not contain < or >",
                        Opacity = Opacity
                    });
                }

                if (Alerts.Any())
                {
                    await JSRuntime.InvokeVoidAsync("scrollToTopOfElement", ".modal", "fast");
                    return;
                }

                IsPreload = true;

                var claims = new List<ClaimModel>();
                var newClaims = new List<ClaimModel>();

                foreach (var permission in Permissions.Permissions)
                {
                    if (string.IsNullOrEmpty(permission.ClaimValue))
                        permission.ClaimValue = "0";

                    claims.Add(new ClaimModel
                    {
                        ClaimType = permission.Id,
                        ClaimValue = permission.ClaimValue
                    });
                    newClaims.Add(new ClaimModel
                    {
                        ClaimType = permission.Name,
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

                if (response.StatusCode == StatusCode.Success && response.Result != null)
                {
                    UserGroup.Claims = newClaims;

                    var userGroupId = !IsEdit ? response.Result.UserGroupId : request.UserGroupId;

                    await BackofficeService.AddActivityLog(new ActivityLogModel
                    {
                        Page = AdminPermission.Admin_ManageUserGroups.ToString().Replace("_", "."),
                        Payload = $"{userGroupId}",
                        Activity = !IsEdit ? Activity.Create.ToString() : Activity.Update.ToString(),
                        From = !IsEdit ? "-" : JsonSerializer.Serialize(OldUserGroup),
                        To = JsonSerializer.Serialize(UserGroup),
                        UserId = _userState.Model.Id,
                        UserName = _userState.Model.Name
                    });

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync(IsEdit ? "EDIT" : "ADD");
                }
                else
                {
                    Alerts.Add(new()
                    {
                        Status = AlertStatus.Error,
                        Message = $"{response.Message}",
                        Opacity = Opacity
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }

            IsPreload = false;
        }

        public async Task IsEmptyClientCallback(bool isEmpty)
        {
            IsClientEmpty = isEmpty;
        }


        public void Dispose()
        {

        }

        #endregion
    }
}
