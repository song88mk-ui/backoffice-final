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
            var sports = (await BackOfficeService.GetSportsAsync()).Result;
            var realSports = new SportsLookupModel();
            var virtualSports = new SportsLookupModel();

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

                var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };

                virtualSports.SportLookup = sports.SportLookup.Where(s => virtualSportIds.Contains(s.SportId + 1000)).Select(s => new Models.SportLookup
                {
                    SportId = s.SportId,
                    SportName= $"Virtual {s.SportName}"
                }).ToList();

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1055,
                    SportName = "Virtual Horse Racing"
                });

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1058,
                    SportName = "Virtual Dog Racing"
                });
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

        public async Task OnEditClientClickEvent(ClientListResponse.Client client)
        {
            var sports = (await BackOfficeService.GetSportsAsync()).Result;
            var realSports = new SportsLookupModel();
            var virtualSports = new SportsLookupModel();

            if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            {
                realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

                var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };

                virtualSports.SportLookup = sports.SportLookup.Where(s => virtualSportIds.Contains(s.SportId + 1000)).Select(s => new Models.SportLookup
                {
                    SportId = s.SportId,
                    SportName = $"Virtual {s.SportName}"
                }).ToList();

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1055,
                    SportName = "Virtual Horse Racing"
                });

                virtualSports.SportLookup.Add(new Models.SportLookup
                {
                    SportId = 1058,
                    SportName = "Virtual Dog Racing"
                });
            }

            client.BetSettings = new List<ClientListResponse.ClientBetSetting>();


            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("Client", client);
            parameters.Add("RealSports", realSports);
            parameters.Add("VirtualSports", virtualSports);
            parameters.Add("IsEdit", true);

            var options = new ModalOptions();
            ModalService.Open<CreateOrUpdateModal>("Edit Client", parameters, options);
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
