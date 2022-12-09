using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class Clients : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        public MetaData MetaData { get; set; }
        public ClientListModel ClientListModel { get; set; }
        public ClientListCallbackModel ClientListCallbackModel { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientListModel = new ClientListModel();
            ClientListCallbackModel = new ClientListCallbackModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Manage Clients";
            IconClass = "ico-clients";
        }

        public async Task OnInitEvent(ClientListCallbackModel request)
        {
            ClientListCallbackModel = request;
            ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, 1);
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task<ClientListModel> GetClientList(string search, int sortBy, int page)
        {
            var request = new GetClientListRequest
            {
                Search = search,
                Page = page,
                PageSize = 25,
                SortBy = (ClientSorting)sortBy
            };

            var response = await BackOfficeService.GetClientList(request);

            if (response.StatusCode == StatusCode.Success)
            {
                MetaData = new MetaData
                {
                    CurrentPage = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    TotalCount = response.Result.Total,
                    TotalPages = response.Result.TotalPage,
                };

                return new ClientListModel
                {
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    Total = response.Result.Total,
                    TotalPage = response.Result.TotalPage,
                    Clients = response.Result.Clients.Select((s, index) => new ClientListModel.Client
                    {
                        Num = index + 1,
                        ClientId = s.ClientId,
                        Name = s.Name,
                        StatusId = s.StatusId,
                        StatusName = s.StatusName,
                        Region = s.Region,
                        Website = s.Website
                    }).ToList()
                };
            }

            return new ClientListModel();
        }

        #endregion
    }
}
