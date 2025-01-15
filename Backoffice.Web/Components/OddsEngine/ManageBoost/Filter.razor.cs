using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Backoffice.Web.Components.Common;
using System.Drawing;
using Backoffice.Web.States;
using System.ComponentModel;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSportChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnSegmentChangedCallback { get; set; }
        [Parameter] public EventCallback<int> OnLeagueChangedCallback { get; set; }
        [Parameter] public EventCallback<string> OnInputSearchCallback { get; set; }
        [Parameter] public string Region { get; set; } = "HK";
        public string SelectedClientCode { get; set; }
        public int SelectedSportId { get; set; }
        public int SelectedSegmentId { get; set; }
        public int SelectedLeagueId { get; set; }
        public SearchFormModel SearchForm { get; set; }

        private string _region = string.Empty;
        private List<SelectListItemModel<string>> _clients;
        private List<SelectListItemModel<int>> _sports;
        private List<SelectListItemModel<int>> _segments;
        private List<SelectListItemModel<int>> _leagues;
        private LanguageCode _languageCode;
        private List<ClientInformationModel> _clientListReference = new List<ClientInformationModel>();
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _clients = new List<SelectListItemModel<string>>();
            _sports = new List<SelectListItemModel<int>>();
            _segments = new List<SelectListItemModel<int>>();
            _leagues = new List<SelectListItemModel<int>>();

            SearchForm = new SearchFormModel();

            _clients = await LoadClients();
            SelectedClientCode = _clients.FirstOrDefault().Value;
            _sports = await LoadSports();
            SelectedSportId = 0;
            _segments = await LoadSegments(SelectedSportId);
            SelectedSegmentId = 0;
            _leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
            SelectedLeagueId = 0;

            await OnInitCallback.InvokeAsync();
            _userState.PropertyChanged += UserStatePropertyChanged;
        }
        protected override async Task OnParametersSetAsync()
        {
            if (_languageCode != _translationState.ActiveLanguageCode)
            {
                _languageCode = _translationState.ActiveLanguageCode;

                _sports = await LoadSports();
                SelectedSportId = 0;
                _segments = await LoadSegments(SelectedSportId);
                SelectedSegmentId = 0;
                _leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
                SelectedLeagueId = 0;

                StateHasChanged();
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
            _modalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
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
            _modalService.Open<ViewAllLogsModal>("View Logs", parameters, options);
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();

            await OnClientChangedCallback.InvokeAsync(SelectedClientCode);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());

            _segments = await LoadSegments(SelectedSportId);
            SelectedSegmentId = 0;

            _leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
            SelectedLeagueId = 0;

            await OnSportChangedCallback.InvokeAsync(SelectedSportId);
        }

        public async Task OnSegmentChangedEvent(ChangeEventArgs e)
        {
            SelectedSegmentId = int.Parse(e.Value.ToString());

            _leagues = await LoadLeagues(SelectedSportId, SelectedSegmentId);
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
            _clients = new();
            _sports = new();
            _segments = new();
            _leagues = new();
            SearchForm = new();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private async void UserStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_userState.TradingClients != null && _userState.TradingClients.Any() && !_clientListReference.Any())
            {
                _clientListReference = new List<ClientInformationModel>(_userState.TradingClients);

                _clients = await LoadClients();
                SelectedClientCode = _clients.FirstOrDefault().Value;

                await OnClientChangedCallback.InvokeAsync(SelectedClientCode);
            }
        }
        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            var response = await _tradingService.GetClientList();

            var clientList = response.Result;
            if (clientList == null)
            {
                return new();
            }

            clientList = _userState.ClientCodes.Any() ? clientList.Where(c => _userState.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<ClientInformationModel>();

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
            var response = await _tradingService.GetSportLookup(_languageCode.ToString());

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
            var response = await _tradingService.GetSegmentLookup(sportId, _languageCode);

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
            var response = await _tradingService.GetLeagueLookup(sportId, segmentId, _languageCode);

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
