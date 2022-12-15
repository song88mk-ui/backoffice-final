using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class SubUser : IDisposable
    {
        #region Properties
        [Inject] public IAuthenticationService AuthenticationService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        public UserListResponse SubUsers { get; set; }
        public MetaData MetaData { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Sub Users";
            IconClass = "ico-sub-user";
        }

        public async Task OnInitEvent()
        {
            SubUsers = await GetSubUsers(1);
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

            var options = new ModalOptions();
            //ModalService.Open<CreateModal>("Create Client", parameters, options);
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
                MainUserId = "1001",
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
                    Users = response.Result.Users.ToList()
                };
            }

            return new UserListResponse();
        }
        #endregion
    }
}
