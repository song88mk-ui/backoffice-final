using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties

        [Parameter] public ConfigState State { get; set; }
        [Parameter] public ConfigContentFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback OnStateHasChange { get; set; }

        #endregion

        #region Fields
        private List<ConfigClientViewModel> _configClients = new();
        private List<ConfigClientViewModel>? _selectedConfigClients = new();

        private bool _isSelectedLeagueShowing;
        private string _pageTitle;
        private MetaData _metaData = new();
        private bool _isPreloader = true;
        private bool _init = true;

        private ConfigState? _state = null;
        private ConfigStateFilterModel _stateFilterModel = new();
        private SearchFormModel _searchForm = new();
        private AlertModel _alertModel = new();
        private bool _loadFailureShown;
        private bool _shouldRender = true;
        private string _selectedClientName = string.Empty;
        private int _selectedClientCount;
        private LanguageCode _languageCode;

        private CancellationTokenSource? _cts;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                if (_state.HasValue && State != _state)
                {
                    // One instance serves both tabs, so a selection made on one tab must not show on the other.
                    if (_isSelectedLeagueShowing)
                    {
                        await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                        _isSelectedLeagueShowing = false;
                    }

                    _selectedConfigClients = new();
                }

                _languageCode = _translationState.ActiveLanguageCode;
                _selectedClientName = string.Empty;
                _languageCode = _translationState.ActiveLanguageCode;
                _backofficeService.LanguageCode = _languageCode;
                _state = State;

                _searchForm.SearchText = "";
                _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";

                var firstSport = FilterModel.Sports.FirstOrDefault();

                _stateFilterModel = new()
                {
                    ClientCodes = FilterModel.Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                    SportId = int.Parse(firstSport?.Value ?? "-1"),
                    SportName = firstSport?.Name ?? "-",
                    SearchText = _searchForm.SearchText
                };

                _selectedClientCount = _stateFilterModel.ClientCodes.Count();

                if (_selectedClientCount == 1)
                    _selectedClientName = FilterModel.Clients?.FirstOrDefault(x => x.IsChecked && x.Value.ToUpper() != "ALL")?.Name ?? string.Empty;

                await LoadLeagues();
            }

            _init = false;
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ValidateParameters() => _init || State != _state || _languageCode != _translationState.ActiveLanguageCode;

        public async Task HandleClientCheckboxClick(Dictionary<string, string> items)
        {
            _selectedClientName = string.Empty;
            _selectedClientCount = items.Count;

            if (_selectedClientCount == 1)
            {
                _selectedClientName = items.FirstOrDefault().Value;
            }

            _stateFilterModel.ClientCodes = new List<string>();
            if (items != null)
            {
                _stateFilterModel.ClientCodes = items.Keys.Where(x => x.ToUpper() != "ALL").ToList();
            }

            if (_isSelectedLeagueShowing)
            {
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _selectedConfigClients = new();

            await HandledPageSelect(1);
        }

        public async Task HandleSportCheckboxClick(ChangeEventArgs e)
        {
            if (_isSelectedLeagueShowing)
            {
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _selectedConfigClients = new();

            if (e != null)
            {
                var firstSport = FilterModel.Sports.FirstOrDefault(c => c.Value == e.Value.ToString());

                _stateFilterModel.SportId = int.Parse(firstSport?.Value ?? "-1");
                _stateFilterModel.SportName = firstSport?.Name ?? string.Empty;
            }
            await HandledPageSelect(1);
        }

        public async Task HandleModalSave(object e)
        {
            _selectedConfigClients = new();
            _loadFailureShown = false;
            _alertModel = new();

            if (e is AlertModel)
            {
                _alertModel = (AlertModel)e;
            }

            await HandledPageSelect(1);
            if (_isSelectedLeagueShowing)
            {
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            StateHasChanged();
            await OnStateHasChange.InvokeAsync();
        }

        public async Task HandleSearchInput()
        {
            _stateFilterModel.SearchText = _searchForm.SearchText;
            await HandledPageSelect(1);
        }

        public async Task HandledPageSelect(int selectedPage)
        {
            _metaData.CurrentPage = selectedPage;
            await LoadLeagues();
        }

        public async Task HandleConfigSelectionChange(ConfigClientViewModel config)
        {
            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            if (config.IsChecked)
                _selectedConfigClients.Add(config);
            else
                _selectedConfigClients.Remove(config);

            if (_selectedConfigClients.Count > 0 && !_isSelectedLeagueShowing)
            {
                _isSelectedLeagueShowing = true;
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }

            if (_selectedConfigClients.Count == 0)
            {
                _isSelectedLeagueShowing = false;
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }

            _shouldRender = true;
        }
        public async Task HandleClearSelectionClick()
        {
            _configClients.ForEach(x => x.IsChecked = false);

            var configs = _configClients.Where(x => !x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));

            if (_isSelectedLeagueShowing && _selectedConfigClients.Count == 0)
            {
                await _jsRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _shouldRender = true;
        }
        public void HandleSelectAllClick()
        {
            _configClients.ForEach(x => x.IsChecked = true);
            var configs = _configClients.Where(x => x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));
            _selectedConfigClients.AddRange(configs);

            _shouldRender = true;
        }
        // BO-151: on the Unassigned tab the selected leagues are assigned as Active or Restricted. On the Assigned tab the
        // same dialog changes that state. Templates are no longer part of it.
        public void HandleAssignClick() =>
            OpenAssignLeaguesModal(_state == ConfigState.Assigned ? AssignLeaguesMode.ChangeState : AssignLeaguesMode.Assign);

        public void HandleUnassignClick() => OpenAssignLeaguesModal(AssignLeaguesMode.Unassign);

        private void OpenAssignLeaguesModal(AssignLeaguesMode mode)
        {
            var leagues = _selectedConfigClients?.Where(x => x.IsChecked).ToList() ?? new List<ConfigClientViewModel>();

            if (leagues.Count == 0)
                return;

            var parameters = new ModalParameters();

            parameters.Add("Leagues", leagues);
            parameters.Add("Mode", mode);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");

            var title = mode switch
            {
                AssignLeaguesMode.Unassign => "Unassign Leagues",
                AssignLeaguesMode.ChangeState => "Change State",
                _ => "Assign Leagues"
            };

            _modalService.Open<AssignLeaguesModal>(title, parameters, new ModalOptions());

            _shouldRender = true;
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _selectedConfigClients = null;
            _state = null;
        }

        #endregion

        #region Utilities
        private async Task LoadLeagues()
        {
            _cts?.Cancel();

            if (!_isPreloader)
            {
                _isPreloader = true;
                _shouldRender = true;
            }

            var payload = new ConfigTemplateLeagueRequest();
            if (_stateFilterModel != null)
            {
                payload.ClientCodes = _stateFilterModel.ClientCodes ?? new List<string>();
                payload.SportId = _stateFilterModel.SportId;
                payload.SearchText = _stateFilterModel.SearchText;
            }
            payload.MetaData = _metaData;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            var response = await _backofficeService.GetConfigClient(_state ?? ConfigState.Assigned, payload, token: token);

            if (token.IsCancellationRequested)
                return;

            if (response.StatusCode == StatusCode.Success)
            {
                _configClients = new List<ConfigClientViewModel>();
                _metaData = new MetaData();

                if (response.Result != null)
                {
                    if (response.Result.ClientConfigs != null && response.Result.ClientConfigs.Any())
                        _configClients = ConfigClientViewModel.ConvertList(response.Result.ClientConfigs);

                    if (response.Result.MetaData != null)
                        _metaData = response.Result.MetaData;
                }

                _configClients.ForEach(config => config.IsChecked = _selectedConfigClients?.FirstOrDefault(x => x.ClientCode == config.ClientCode && x.LeagueId == config.LeagueId && x.SportId == config.SportId)?.IsChecked ?? config.IsChecked);

                if (_loadFailureShown)
                {
                    _alertModel = new();
                    _loadFailureShown = false;
                }
            }
            else
            {
                // A failed call must not look like "no leagues", and the previous list must not stay under this tab.
                _configClients = new List<ConfigClientViewModel>();
                _metaData = new MetaData();

                Log.Warning("OE Config could not load the {State} leagues: {Message}", _state, response.Message);
                _loadFailureShown = true;
                _alertModel = new AlertModel
                {
                    Status = AlertStatus.Error,
                    Message = "The leagues could not be loaded. Please try again, or contact support if it keeps happening.",
                    IsAlwaysShown = true
                };
            }

            _isPreloader = false;
            _shouldRender = true;
        }

        #endregion
    }
}
