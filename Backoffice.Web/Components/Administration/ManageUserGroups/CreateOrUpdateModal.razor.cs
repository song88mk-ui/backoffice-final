using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public UserGroupResponse UserGroup { get; set; }
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
                }
            }
        }

        public async Task OnCancelClickEvent()
        {
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
