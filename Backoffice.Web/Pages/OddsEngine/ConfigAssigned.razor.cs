using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public ConfigState State { get; set; }
        [Parameter] public ConfigContentFilterModel FilterModel { get; set; }
        [Parameter] public EventCallback OnStateHasChanged { get; set; }

        #endregion

        #region Fields
        private List<BreadcrumbModel> _breadcrumbs;
        private List<ConfigClientViewModel> _configClients = new();
        private List<ConfigClientViewModel>? _selectedConfigClients = new();
        private List<SelectListItemWithCheckboxModel<string>>? _clients;

        private bool _isSelectedLeagueShowing;
        private string _pageTitle;
        private MetaData _metaData = new();
        private string _iconClass = "";
        private bool _isPreloader = true;
        private bool _isEdit = false;
        private ConfigState? _state = null;
        private List<SelectListItemModel<string>> _sports = new();
        private ConfigStateFilterModel _stateFilterModel = new();
        private SearchFormModel _searchForm = new();
        private AlertModel _alertModel = new();
        private bool _shouldRender = true;
        private string _selectedClientName = string.Empty;
        private int _selectedClientCount;
        private LanguageCode _languageCode;

        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";

            _breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"odds-engine/config/{(State == ConfigState.Assigned ? "assigned":"unassigned")}",
                    IsCurrentPage = true
                }
            };
        }

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _languageCode = TranslationState.ActiveLanguageCode;
                _selectedClientName = string.Empty;
                _clients = new List<SelectListItemWithCheckboxModel<string>>(FilterModel.Clients);
                _sports = new List<SelectListItemModel<string>>(FilterModel.Sports);
                _languageCode = TranslationState.ActiveLanguageCode;
                _backofficeService.LanguageCode = _languageCode;
                _state = State;

                _searchForm.SearchText = "";
                _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";

                _stateFilterModel = new()
                {
                    ClientCodes = _clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                    SportId = int.Parse(_sports.FirstOrDefault()?.Value ?? "-1"),
                    SportName = _sports.FirstOrDefault()?.Name ?? "-",
                    SearchText = _searchForm.SearchText
                };

                _selectedClientCount = _stateFilterModel.ClientCodes.Count();

                if (_selectedClientCount == 1)
                    _selectedClientName = _clients?.FirstOrDefault(x => x.IsChecked && x.Value.ToUpper() != "ALL")?.Name ?? string.Empty;

                await LoadLeagues(_stateFilterModel);
            }
        }
        protected override bool ShouldRender() => _shouldRender;
        protected override void OnAfterRender(bool firstRender) => _shouldRender = false;
        private bool ValidateParameters() => _clients == null || !FilterModel.Clients.SequenceEqual(_clients) || !FilterModel.Sports.SequenceEqual(_sports) || State != _state  || _languageCode != TranslationState.ActiveLanguageCode;

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
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
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _selectedConfigClients = new();

            await SelectedPage(1);
        }

        public async Task OnSportCheckboxClickEvent(ChangeEventArgs e)
        {
            if (_isSelectedLeagueShowing)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _selectedConfigClients = new();

            if (e != null)
            {
                _stateFilterModel.SportId = int.Parse((e == null ? _sports.FirstOrDefault()?.Value : e.Value?.ToString()) ?? "-1");
                _stateFilterModel.SportName = (e == null ? _sports.FirstOrDefault()?.Name : _sports.FirstOrDefault(x => x.Value == e.Value.ToString())?.Name) ?? string.Empty;
            }
            await SelectedPage(1);
        }

        public async Task OnModalSaveEvent(object e)
        {
            _selectedConfigClients = new();
            _alertModel = new();
            await OnStateHasChanged.InvokeAsync();
            await LoadLeagues(_stateFilterModel);

            if (e is AlertModel)
            {
                _alertModel = (AlertModel)e;
            }

            await SelectedPage(1);
            if (_isSelectedLeagueShowing)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }
        }

        public async Task OnInputSearchEvent()
        {
            _stateFilterModel.SearchText = _searchForm.SearchText;
            await LoadLeagues(_stateFilterModel);
        }

        public async Task SelectedPage(int selectedPage)
        {
            _metaData.CurrentPage = selectedPage;
            await LoadLeagues(_stateFilterModel);
        }

        public async Task OnResetClickEvent()
        {
            _metaData = new MetaData();
            _searchForm.SearchText = "";
            _stateFilterModel = new ConfigStateFilterModel()
            {
                ClientCodes = _clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                SportId = int.Parse(_sports.FirstOrDefault().Value),
                SportName = _sports.FirstOrDefault().Name,
                SearchText = _searchForm.SearchText
            };
            _selectedClientCount = (_clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>()).Count();

            await LoadLeagues(_stateFilterModel);
        }
        public async Task OnConfigSelectionChanged(ConfigClientViewModel config)
        {
            if (_configClients.Any(x => x.IsChecked))
                _isEdit = true;
            else
                _isEdit = false;

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            if (config.IsChecked)
                _selectedConfigClients.Add(config);
            else
                _selectedConfigClients.Remove(config);

            if (_selectedConfigClients.Count > 0 && !_isSelectedLeagueShowing)
            {
                _isSelectedLeagueShowing = true;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }

            if (_selectedConfigClients.Count == 0)
            {
                _isSelectedLeagueShowing = false;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }
        }
        public async Task OnClearSelectionClickedEvent()
        {
            _configClients.ForEach(x => x.IsChecked = false);
            _isEdit = false;

            var configs = _configClients.Where(x => !x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));

            if (_isSelectedLeagueShowing && _selectedConfigClients.Count == 0)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                _isSelectedLeagueShowing = false;
            }

            _shouldRender = true;
        }
        public void OnSelectAllClickedEvent()
        {
            _configClients.ForEach(x => x.IsChecked = true);
            var configs = _configClients.Where(x => x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));
            _selectedConfigClients.AddRange(configs);

            _shouldRender = true;
        }
        public void OnEditTemplateClickedEvent()
        {
            var leagues = _selectedConfigClients?.Where(x => x.IsChecked).ToList();

            var parameters = new ModalParameters();

            parameters.Add("Leagues", leagues);
            parameters.Add("State", _state);
            parameters.Add("SportId", _stateFilterModel.SportId);

            var selectedPrematch = _configClients.Where(x => x.IsChecked && x.PrematchTemplateId > 0).Select(s => s.PrematchTemplateId).Distinct().ToList();
            var selectedLive = _configClients.Where(x => x.IsChecked && x.LiveTemplateId > 0).Select(s => s.LiveTemplateId).Distinct().ToList();
            ;

            parameters.Add("CurrentPrematchId", selectedPrematch.Count() == 1 ? selectedPrematch.FirstOrDefault() : 0);
            parameters.Add("CurrentLiveId", selectedLive.Count() == 1 ? selectedLive.FirstOrDefault() : 0);

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            var options = new ModalOptions();

            switch (_state)
            {
                case ConfigState.Assigned:
                    _modalService.Open<ModifyLeagueTemplateModal>($"Change Template", parameters, options);
                    break;
                case ConfigState.UnAssigned:
                    _modalService.Open<ModifyLeagueTemplateModal>($"Assign Template", parameters, options);
                    break;
            }

            _shouldRender = true;
        }

        public void Dispose()
        {
            _selectedConfigClients = null;
            _state = null;
            GC.Collect();
        }

        #endregion

        #region Utilities
        private async Task LoadLeagues(ConfigStateFilterModel filter = null)
        {
            _cancellationTokenSource?.Cancel();

            if (!_isPreloader)
            {
                _isPreloader = true;
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }

            var payload = new ConfigTemplateLeagueRequest();
            if (filter != null)
            {
                payload.ClientCodes = filter.ClientCodes ?? new List<string>();
                payload.SportId = filter.SportId;
                payload.SearchText = filter.SearchText;
            }
            payload.MetaData = _metaData;

            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;
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
            }

            _isPreloader = false;
            _shouldRender = true;
        }

        #endregion
    }
}
