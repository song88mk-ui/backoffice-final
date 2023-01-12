using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Clients;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class Clients : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        public MetaData MetaData { get; set; }
        public ClientListResponse ClientListModel { get; set; }
        public ClientListCallbackModel ClientListCallbackModel { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ClientListModel = new ClientListResponse();
            ClientListCallbackModel = new ClientListCallbackModel();

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            PageTitle = "Manage Clients";
            IconClass = "ico ico-clients";
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

        public async Task OnCreateClientClickEvent()
        {
            var sports = (await BackOfficeService.GetSportsAsync("EN")).Result;
            var realSports = new SportsLookupModel();
            var virtualSports = new SportsLookupModel();

            if(sports != null && sports.Sports != null && sports.Sports.Any())
            {
                realSports.Sports = sports.Sports.Where(s => s.SportId < 1000).ToList();
                virtualSports.Sports = sports.Sports.Where(s => s.SportId >= 1000).ToList();
            }

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("Client", new ClientListResponse.Client());
            parameters.Add("RealSports", realSports);
            parameters.Add("VirtualSports", virtualSports);
            parameters.Add("IsEdit", false);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Create Client", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task<ClientListResponse> GetClientList(string search, int sortBy, int page)
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

                return new ClientListResponse
                {
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    Total = response.Result.Total,
                    TotalPage = response.Result.TotalPage,
                    Clients = response.Result.Clients.Select((s, index) => new ClientListResponse.Client
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

            return new ClientListResponse();
        }

        #endregion
    }
}
