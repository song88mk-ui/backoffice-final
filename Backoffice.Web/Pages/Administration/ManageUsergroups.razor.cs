using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ManageUsergroups : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        public MetaData MetaData { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage User Group";
            IconClass = string.Empty;
        }

        public async Task OnInitEvent(ClientListCallbackModel request)
        {
            //ClientListCallbackModel = request;
            //ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, 1);
        }
        public async Task SelectPage(int page)
        {
            //MetaData.CurrentPage = page;
            //ClientListModel = await GetClientList(ClientListCallbackModel.SearchText, ClientListCallbackModel.SortBy, MetaData.CurrentPage);
        }

        public async Task OnCreateUserGroupClickEvent()
        {
            //var sports = (await BackOfficeService.GetSportsAsync()).Result;
            //var realSports = new SportsLookupModel();
            //var virtualSports = new SportsLookupModel();

            //if (sports != null && sports.SportLookup != null && sports.SportLookup.Any())
            //{
            //    realSports.SportLookup = sports.SportLookup.Where(s => s.SportId < 1000).ToList();

            //    var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };

            //    virtualSports.SportLookup = sports.SportLookup.Where(s => virtualSportIds.Contains(s.SportId + 1000)).Select(s => new Models.SportLookup
            //    {
            //        SportId = s.SportId + 1000,
            //        SportName = $"Virtual {s.SportName}"
            //    }).ToList();

            //    virtualSports.SportLookup.Add(new Models.SportLookup
            //    {
            //        SportId = 1055,
            //        SportName = "Virtual Horse Racing"
            //    });

            //    virtualSports.SportLookup.Add(new Models.SportLookup
            //    {
            //        SportId = 1058,
            //        SportName = "Virtual Dog Racing"
            //    });
            //}

            //var parameters = new ModalParameters();

            //parameters.Add("ModalClass", "modal-center modal-show");
            //parameters.Add("ModalDialogClass", "modal-lg");
            //parameters.Add("Client", new ClientListResponse.Client());
            //parameters.Add("RealSports", realSports);
            //parameters.Add("VirtualSports", virtualSports);
            //parameters.Add("IsEdit", false);

            //var options = new ModalOptions();
            //ModalService.Open<CreateOrUpdateModal>("Create Client", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
