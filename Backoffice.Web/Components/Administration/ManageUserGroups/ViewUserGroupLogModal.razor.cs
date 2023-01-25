using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json.Bson;

namespace Backoffice.Web.Components.Administration.ManageUserGroups
{
    public partial class ViewUserGroupLogModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private string UserGroupId { get; set; }
        private MetaData MetaData { get; set; }
        public bool IsPreLoader { get; set; }
        public List<ActivityLogModel> ActivityLogs { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ActivityLogs = null;
            IsPreLoader = true;
            UserGroupId = ModalParameters.TryGet<string>("UserGroupId");
            ActivityLogs = new List<ActivityLogModel>();
            MetaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };
            await InvokeAsync(StateHasChanged);
            await GetContent();
        }
        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }
        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);
            await GetContent();
        }
        public void Dispose()
        {
            
        }
        #endregion


        #region Utilities
        private async Task GetContent()
        {
            var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = AdminPermission.Admin_ManageUserGroups.ToString().Replace("_", "."),
                Payload = $"{UserGroupId}",
                MetaData = MetaData
            })).Result;

            if (response != null)
            {
                ActivityLogs = response.ActivityLogs;
                MetaData = response.MetaData;
            }
            IsPreLoader = false;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
