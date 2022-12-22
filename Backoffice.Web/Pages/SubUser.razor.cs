using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.SubUser;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Backoffice.Web.Pages
{
    public partial class SubUser : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public PermissionResponse Permissions { get; set; }
        public SubUserClaimsModel SubUserClaims { get; set; }
        public UserListResponse SubUsers { get; set; }
        public MetaData MetaData { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Permissions = new PermissionResponse();
            SubUserClaims = new SubUserClaimsModel();
            SubUsers = new UserListResponse();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Sub Users";
            IconClass = "ico ico-sub-user";
        }

        public async Task OnInitEvent(bool isFirstTime)
        {
            SubUsers = await GetSubUsers(1);

            if(isFirstTime)
            {
                SubUserClaims.MainUserId = UserModel.Id;
                SubUserClaims.MainUserName = UserModel.UserName;

                Permissions = await GetPermissionList();
                if(Permissions != null && Permissions.Permissions.Any())
                {
                    foreach(var permission in Permissions.Permissions)
                    {
                        if(permission.IsSubUser)
                        {
                            var subUserClaim = new SubUserClaim
                            {
                                Name = permission.Name,
                                ClaimType = permission.ClaimType,
                                HasReadOnly = permission.HasReadOnly,
                                ClaimValue = "0",
                                IsEnabled = true
                            };

                            var claim = UserModel.Claims.FirstOrDefault(c => c.Type == permission.ClaimType);
                            if(claim != null && Convert.ToInt32(claim.Value) > 0)
                            {
                                subUserClaim.ClaimValue = claim.Value;
                            }

                            if (string.IsNullOrEmpty(subUserClaim.ClaimValue) || subUserClaim.ClaimValue == "0")
                                subUserClaim.IsEnabled = false;

                            SubUserClaims.SubUserClaims.Add(subUserClaim);

                        }
                    }
                }
            }
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            SubUsers = await GetSubUsers(page);
        }

        public async Task OnCreateSubUserClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("SubUserClaims", SubUserClaims);
            
            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Create Sub-Users", parameters, options);
        }

        public async Task OnSaveClickEvent(object item)
        {
            //var clientIdRefs = string.IsNullOrEmpty(item.ToString()) ? new List<string>() : item.ToString().Split(",").ToList();
            //foreach (var clientIdRef in clientIdRefs)
            //{
            //    if (string.IsNullOrEmpty(clientIdRef))
            //        continue;

            //    var clientTopFixture = TopFixturesModel.TopFixtures.FirstOrDefault(t => t.ClientIdRef == Convert.ToInt32(clientIdRef));
            //    if (clientTopFixture != null)
            //    {
            //        var result = (await BackOfficeService.GetClientTopFixtures(new GetClientTopFixtureRequest { ClientIds = item.ToString() })).Result;

            //        clientTopFixture.TopFixtures = result.TopFixtures.FirstOrDefault()?.TopFixtures ?? new List<ClientTopFixtureResponse.TopFixture>();

            //    }
            //}
        }

        public void Dispose()
        {
        }


        #endregion


        #region Utilities

        private async Task<UserListResponse> GetSubUsers(int page)
        {
            var request = new SubAccountListRequest
            {
                MainUserId = UserModel.Id,
                Page = page,
                PageSize = 25
            };

            var response = await AuthenticationService.GetSubAccounts(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = new MetaData
                {
                    CurrentPage = response.Result.MetaData.CurrentPage,
                    PageSize = response.Result.MetaData.PageSize,
                    TotalCount = response.Result.MetaData.TotalCount,
                    TotalPages = response.Result.MetaData.TotalPages,
                };

                return new UserListResponse
                {
                    MetaData = MetaData,
                    Users = response.Result.Users ?? new List<UserResponse>()
                };
            }

            return new UserListResponse();
        }

        private async Task<PermissionResponse> GetPermissionList()
        {
            var response = await AuthenticationService.GetPermissionList(true);

            if (response.StatusCode == StatusCode.Success)
            {
                return new PermissionResponse
                {
                    Permissions = response.Result.Permissions ?? new List<PermissionModel>(),
                };
            }

            return new PermissionResponse();
        }
        #endregion
    }
}
