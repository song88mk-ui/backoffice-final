using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json.Bson;

namespace Backoffice.Web.Components.ManageUser
{
    public partial class ViewUserLogModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IAuthenticationService AuthenticationService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private string UserId { get; set; }
        private MetaData MetaData { get; set; }
        public bool IsPreLoader { get; set; }
        public List<LogInActivityLogModel> LogInActivityLogs { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            UserId = ModalParameters.TryGet<string>("UserId");
            LogInActivityLogs = new List<LogInActivityLogModel>();
            MetaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };

            await GetContent();
        }
        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }
        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            await GetContent();
        }
        public void Dispose()
        {

        }
        #endregion

        #region Utilities
        private async Task GetContent()
        {
            var userResponse = (await AuthenticationService.GetLogInActivityLogList(UserId, MetaData)).Result;
            if (userResponse != null)
            {
                LogInActivityLogs = userResponse.ActivityLogs;
                MetaData = userResponse.MetaData;
            }

            IsPreLoader = false;
            StateHasChanged();
        }
        #endregion
    }
}
