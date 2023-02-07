using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ManageBoost
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }

        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSportChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSegmentChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnLeagueChangedCallback { get; set; }
        [Parameter] public EventCallback<string> OnInputSearchCallback { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SelectedSportId { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public int SelectedSegmentId { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }
        public int SelectedLeagueId { get; set; }

        public SearchFormModel SearchForm { get; set; }

        #endregion

        //#region Fields

        //public string _clientCode;
        //public int _sportId;
        //public int _segmentId;
        //public int _leagueId;

        //#endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();

            SearchForm = new SearchFormModel();

            Clients = await LoadClients();
            SelectedClientCode = Clients.FirstOrDefault().Value;

            Sports = await LoadSports();
            SelectedSportId = 0;

            Segments = await LoadSegments(SelectedSportId);
            SelectedSegmentId = 0;

            Leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
            SelectedLeagueId = 0;

            //await PriceboostFilterModelChanged.InvokeAsync(PriceboostFilterModel);
            await OnInitCallback.InvokeAsync();
        }

        public async Task OnAddBoostedOdds()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            //parameters.Add("PriceBoostFilterModel", PriceboostFilterModel);            

            var options = new ModalOptions();
            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public async Task OnViewAllLogs()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle","max-width: 85rem;");
            //parameters.Add("PriceBoostFilterModel", PriceboostFilterModel);
            parameters.Add("SelectedPage", SportsbookClientPermission.SBC_OE_PriceBoost_Manage);

            var options = new ModalOptions();
            ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();

            await OnClientChangedCallback.InvokeAsync(SelectedClientCode);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());

            Segments = await LoadSegments(SelectedSportId);
            SelectedSegmentId = 0;

            Leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
            SelectedLeagueId = 0;

            await OnSportChangedCallback.InvokeAsync(SelectedSportId);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegmentId = int.Parse(e.Value.ToString());

            Leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
            SelectedLeagueId = 0;

            await OnSegmentChangedCallback.InvokeAsync(SelectedSegmentId);
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            SelectedLeagueId = int.Parse(e.Value.ToString());

            await OnLeagueChangedCallback.InvokeAsync(SelectedLeagueId);
        }

        public async Task OnInputSearchEvent()
        {
            await OnInputSearchCallback.InvokeAsync(SearchForm.SearchText);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            var result = (await TradingService.GetClientList()).Result;

            if (result != null)
            {
                if (UserModel.ClientCodes.Any())
                {
                    result = result.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }
            }
            var clients = result.Select(s => new SelectListItemModel<string>
            {
                Name = s.ClientName,
                Value = s.ClientCode
            }).ToList();

            clients.Insert(0, new SelectListItemModel<string>
            {
                Value = string.Empty,
                Name = "All Clients"
            });


            return clients;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            sports.Insert(0, new SelectListItemModel<int> 
            { 
                Value = 0,
                Name = "All Sports"
            });

            return sports;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var segments = result.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            segments.Insert(0, new SelectListItemModel<int>
            {
                Value = 0,
                Name = "All Segments"
            });

            return segments;
        }

        private async Task<List<SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var leagues = result.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).Take(100).ToList();

            leagues.Insert(0, new SelectListItemModel<int>
            {
                Value = 0,
                Name = "All Leagues"
            });

            return leagues;
        }

        #endregion
    }
}
