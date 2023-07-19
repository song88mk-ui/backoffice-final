using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Components.Common;
using System.Drawing;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSportChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSegmentChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnLeagueChangedCallback { get; set; }
        [Parameter] public EventCallback<string> OnInputSearchCallback { get; set; }
        [Parameter] public string Region { get; set; }

        private string _region;
        private List<SelectListItemModel<string>> _clients;
        public string _selectedClientCode;
        public List<SelectListItemModel<int>> _sports;
        public int _selectedSportId;
        public List<SelectListItemModel<int>> _segments;
        public int _selectedSegmentId;
        public List<SelectListItemModel<int>> _leagues;
        public int _selectedLeagueId;
        public SearchFormModel _searchForm;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _clients = new List<SelectListItemModel<string>>();
            _sports = new List<SelectListItemModel<int>>();
            _segments = new List<SelectListItemModel<int>>();
            _leagues = new List<SelectListItemModel<int>>();

            _searchForm = new SearchFormModel();


            _clients = await LoadClients();
            _selectedClientCode = _clients.FirstOrDefault().Value;
            _sports = await LoadSports();
            _selectedSportId = 0;
            _segments = await LoadSegments(_selectedSportId);
            _selectedSegmentId = 0;
            _leagues = await LoadLeagues(_selectedSportId, _selectedSegmentId);
            _selectedLeagueId = 0;

            await OnInitCallback.InvokeAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            if (_region != Region.ToUpper())
            {
                _region = Region.ToUpper();
                TradingService.Region = _region;
            }
        }

        public async Task OnAddBoostedOdds()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("Region", _region);
            //parameters.Add("PriceBoostFilterModel", PriceboostFilterModel);            

            var options = new ModalOptions();
            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public async Task OnViewAllLogs()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 85rem;");
            //parameters.Add("PriceBoostFilterModel", PriceboostFilterModel);
            parameters.Add("SelectedPage", SportsbookClientPermission.SBC_OE_PriceBoost_Manage);
            parameters.Add("Region", _region);

            var options = new ModalOptions();
            ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            _selectedClientCode = e.Value.ToString();

            await OnClientChangedCallback.InvokeAsync(_selectedClientCode);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            _selectedSportId = int.Parse(e.Value.ToString());

            _segments = await LoadSegments(_selectedSportId);
            _selectedSegmentId = 0;

            _leagues = await LoadLeagues(_selectedSportId, _selectedSegmentId);
            _selectedLeagueId = 0;

            await OnSportChangedCallback.InvokeAsync(_selectedSportId);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            _selectedSegmentId = int.Parse(e.Value.ToString());

            _leagues = await LoadLeagues(_selectedSportId, _selectedSegmentId);
            _selectedLeagueId = 0;

            await OnSegmentChangedCallback.InvokeAsync(_selectedSegmentId);
        }

        public async Task OnLeagueChangedEvent(ChangeEventArgs e)
        {
            _selectedLeagueId = int.Parse(e.Value.ToString());

            await OnLeagueChangedCallback.InvokeAsync(_selectedLeagueId);
        }

        public async Task OnInputSearchEvent()
        {
            await OnInputSearchCallback.InvokeAsync(_searchForm.SearchText);
        }

        public void Dispose()
        {
            _clients = new();
            _sports = new();
            _segments = new();
            _leagues = new();
            _searchForm = new();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            var response = await TradingService.GetClientList();

            var clientList = response.Result;
            if (clientList == null)
            {
                return new();
            }

            clientList = UserClientCodes != null && UserClientCodes.Any() ? clientList.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientInformationModel>();

            var result = clientList?.Select(s => new SelectListItemModel<string>
            {
                Name = s.ClientName,
                Value = s.ClientCode
            }).OrderBy(x => x.Name).ToList();

            if (result == null)
                result = new List<SelectListItemModel<string>>();

            result.Insert(0, new()
            {
                Value = string.Empty,
                Name = "All Clients"
            });

            return result;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var response = await TradingService.GetSportLookup();

            var sportsLookup = response.Result;
            if (sportsLookup == null)
                return new();

            var result = sportsLookup.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            result.Insert(0, new()
            {
                Value = 0,
                Name = "All Sports"
            });

            return result;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSegments(int sportId)
        {
            var response = await TradingService.GetSegmentLookup(sportId);

            var segmentsLookup = response.Result;
            if (segmentsLookup == null)
                return new();

            var result = segmentsLookup.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            result.Insert(0, new()
            {
                Value = 0,
                Name = "All Segments"
            });

            return result;
        }

        private async Task<List<SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
        {
            var response = await TradingService.GetLeagueLookup(sportId, segmentId);

            var leaguesLookup = response.Result;
            if (leaguesLookup == null)
                return new();

            var result = leaguesLookup.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).Take(100).ToList();

            result.Insert(0, new()
            {
                Value = 0,
                Name = "All Leagues"
            });

            return result;
        }

        #endregion
    }
}
