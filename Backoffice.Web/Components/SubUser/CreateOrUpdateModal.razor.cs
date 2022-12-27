using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SubUser
{
    public partial class CreateOrUpdateModal : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        public UserResponse SubUser { get; set; }
        public bool IsEdit { get; set; }
        public string ConfirmPassword { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        public List<AlertModel> Alerts { get; set; }

        #endregion
        protected override async Task OnInitializedAsync()
        {
            try
            {
                SubUserClaims = ModalParameters.TryGet<SubUserClaimsModel>("SubUserClaims");
                Alerts = new List<AlertModel>();
                SubUser = ModalParameters.TryGet<UserResponse>("SubUser");
                IsEdit = ModalParameters.TryGet<bool>("IsEdit");
                SubUser.Status = IsEdit ? SubUser.Status : "Normal";

                if (SubUser.UserName.IndexOf(SubUserClaims.MainUserName) == 0)
                    SubUser.UserName = SubUser.UserName.Replace(SubUserClaims.MainUserName, "");

                ConfirmPassword = string.Empty;
            }
            catch (Exception)
            {

            }
        }
        public async Task OnSubUserPreferenceClicked(ChangeEventArgs e, SubUserClaim claim, bool isReadOnly)
        {
            if (Convert.ToBoolean(e.Value) == true)
            {
                if (isReadOnly == true)
                {
                    claim.ClaimValue = "2";
                }
                else
                {
                    claim.ClaimValue = "1";
                }
            }
            else
            {
                claim.ClaimValue = "0";
            }
        }
        public async Task OnStatusChangedEvent(ChangeEventArgs e)
        {
            SubUser.Status = e.Value.ToString();
        }
        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }
        public async Task OnSaveClickEvent()
        {
            Alerts.Clear();

            if (string.IsNullOrEmpty(SubUser.UserName))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Sub Username is required",
                    Opacity = 1.0m
                });
            }

            if (string.IsNullOrEmpty(SubUser.NickName))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Nickname is required",
                    Opacity = 1.0m
                });
            }

            if (!IsEdit && string.IsNullOrEmpty(SubUser.Password))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Password is required",
                    Opacity = 1.0m
                });
            }
            else if (!IsEdit && string.IsNullOrEmpty(ConfirmPassword))
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Please confirm password",
                    Opacity = 1.0m

                });
            }
            else if (!IsEdit && SubUser.Password != ConfirmPassword)
            {
                Alerts.Add(new AlertModel
                {
                    AlertStatus = "Error",
                    AltertMessage = "Password does not match",
                    Opacity = 1.0m
                });
            }

            if (Alerts.Any())
                return;

            var request = new UserRegisterRequest()
            {
                AppId = (int)Application.SportsbookClient,
                AppName = Application.SportsbookClient.ToString(),
                Email = "sbsubaccount@fci.com",
                MainUserId = SubUserClaims.MainUserId,
                NickName = SubUser.NickName,
                Password = IsEdit ? "" : SubUser.Password,
                Status = SubUser.Status,
                UserName = $"{SubUserClaims.MainUserName}{SubUser.UserName}",
                UserId = IsEdit ? SubUser.UserId : Guid.NewGuid().ToString(),
                RoleName = "Default",
                CreatedDate = IsEdit ? SubUser.CreatedDate : DateTime.UtcNow,
            };

            request.Claims = new List<ClaimModel>();

            if (SubUserClaims.SubUserClaims.Any())
            {
                foreach (var subClaim in SubUserClaims.SubUserClaims)
                {
                    request.Claims.Add(new ClaimModel { ClaimType = subClaim.ClaimType, ClaimValue = subClaim.ClaimValue });
                }
            }

            if(IsEdit == true)
            {
                var result = (await AuthenticationService.UpdateUser(request)).Result;

                if (result)
                {
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"Failed to insert {SubUserClaims.MainUserName}{SubUser.UserName}",
                        Opacity = 1.0m
                    });
                }
            }
            else
            {
                var result = (await AuthenticationService.Register(request)).Result;

                if (result)
                {
                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Error",
                        AltertMessage = $"Failed to insert {SubUserClaims.MainUserName}{SubUser.UserName}",
                        Opacity = 1.0m
                    });
                }
            }
            
        }
        public void Dispose()
        {

        }
    }
}
