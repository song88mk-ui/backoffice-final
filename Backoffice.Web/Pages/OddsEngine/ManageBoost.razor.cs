using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageBoost : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }        
        public MetaData MetaData { get; set; }
        public PriceboostFilterModel PriceboostFilterModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }        
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage Boost";
            IconClass = string.Empty;
            
            PriceboostFilterModel= new PriceboostFilterModel();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100
            };

            PriceboostFilterModel.Clients = await LoadClients();
            PriceboostFilterModel.Sports = await LoadSports();
            PriceboostFilterModel.Segments = await LoadSegments(PriceboostFilterModel.SportId);
            PriceboostFilterModel.Leagues = await LoadLeagues(PriceboostFilterModel.SportId, PriceboostFilterModel.SegmentId);
            //await InvokeAsync(StateHasChanged);
        }        

        public async Task OnEdit()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();            
            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public async Task OnViewLog()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<ViewLogsBoostedModal>($"View Logs", parameters, options);
        }

        public async Task OnDelete()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");

            var options = new ModalOptions();
            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public async Task GetContents()
        {

        }

        public async Task OnClientChangedEvent(string clientCode)
        {

        }

        public async Task OnSportChangedEvent(int sportId)
        {
            PriceboostFilterModel.SegmentId = 0;
            PriceboostFilterModel.LeagueId = 0;

            PriceboostFilterModel.Segments = await LoadSegments(sportId);

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSegmentChangedEvent(Tuple<int,int> param)
        {
            PriceboostFilterModel.LeagueId = 0;
            
            PriceboostFilterModel.Leagues = await LoadLeagues(param.Item1, param.Item2);

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueChangedEvent()
        {
            await GetContents();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task<List<Models.SelectListItemModel<string>>> LoadClients()
        {
            ClientModel clients = new ClientModel();
            clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }
            }

            return clients.Clients.Select(x => new Models.SelectListItemModel<string>()
            {
                Name = x.ClientName,
                Value = x.ClientId.ToString()
            }).ToList();
        }

        private async Task<List<Models.SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        private async Task<List<Models.SelectListItemModel<int>>> LoadSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var segments = result.SegmentLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }

        private async Task<List<Models.SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            if (result == null)
                return new List<Models.SelectListItemModel<int>>();

            var leagues = result.LeagueLookup.Select(s => new Models.SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).Take(100).ToList();

            return leagues;
        }
        #endregion
    }
}
