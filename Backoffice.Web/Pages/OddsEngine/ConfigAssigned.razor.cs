using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing.Text;
using System.Threading;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public ConfigState State { get; set; }
        [Parameter] public ConfigContentFilterModel FilterModel { get; set; }

        [Parameter] public EventCallback OnStateHasChanged { get; set; }


        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigClientViewModel> ConfigClients { get; set; } = new List<ConfigClientViewModel>();
        private List<ConfigClientViewModel>? _selectedConfigClients { get; set; }
        public MetaData MetaData { get; set; }
        public string IconClass { get; set; }
        public bool IsPreloader { get; set; } = true;
        public bool IsEdit { get; set; } = false;
        public bool IsSelectedLeagueShowing { get; set; } = false;

        #endregion
        private string _pageTitle;
        private ConfigState? _state;
        private List<SelectListItemModel<string>> _sports;
        private ConfigStateFilterModel _stateFilterModel = new();
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private SearchFormModel _searchForm;
        private int _selectedClientCount;
        private string _region;
        private string _selectedClientName;
        private AlertModel _alertModel;
        private CancellationTokenSource _cancellationTokenSource;
        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _state = null;
            IsPreloader = true;
            IsEdit = false;
            IconClass = "";
            _sports = new();
            _searchForm = new();
            _alertModel = new();
            MetaData = new();

            _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";
            _selectedConfigClients = new();

            Breadcrumbs = new List<BreadcrumbModel>()
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
            var shouldRender = false;

            if (!FilterModel.Clients.Equals(_clients))
            {
                _selectedClientName = string.Empty;
                _clients = new List<SelectListItemWithCheckboxModel<string>>(FilterModel.Clients);
                _stateFilterModel.ClientCodes = _clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>();
                _selectedClientCount = _stateFilterModel.ClientCodes.Count();

                if (_selectedClientCount == 1)
                    _selectedClientName = _clients?.FirstOrDefault(x => x.IsChecked && x.Value.ToUpper() != "ALL")?.Name ?? string.Empty;

                shouldRender = true;
            }

            if (!FilterModel.Clients.Equals(_sports))
            {
                _sports = new List<SelectListItemModel<string>>(FilterModel.Sports);
                _stateFilterModel.SportId = int.Parse(_sports.FirstOrDefault()?.Value ?? "-1");
                _stateFilterModel.SportName = _sports.FirstOrDefault()?.Name ?? "-";

                shouldRender = true;
            }

            if (State != _state || _region != FilterModel.Region)
            {
                _region = FilterModel.Region;
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

                shouldRender = true;
                await LoadLeagues(_stateFilterModel);
            }
        }

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

            if (IsSelectedLeagueShowing)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                IsSelectedLeagueShowing = false;
            }

            _selectedConfigClients = new();

            await SelectedPage(1);
            //var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //SelectedClientCount = selectedClients.Count;
            //SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnSportCheckboxClickEvent(ChangeEventArgs e)
        {
            if (IsSelectedLeagueShowing)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                IsSelectedLeagueShowing = false;
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
            if(IsSelectedLeagueShowing)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                IsSelectedLeagueShowing = false;
            }
        }

        public async Task OnInputSearchEvent()
        {
            _stateFilterModel.SearchText = _searchForm.SearchText;
            await LoadLeagues(_stateFilterModel);
        }

        public async Task SelectedPage(int selectedPage)
        {
            MetaData.CurrentPage = selectedPage;
            await LoadLeagues(_stateFilterModel);
        }

        public async Task OnResetClickEvent()
        {
            MetaData = new MetaData();
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
            if (ConfigClients.Any(x => x.IsChecked))
                IsEdit = true;
            else
                IsEdit = false;

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            if (config.IsChecked)
                _selectedConfigClients.Add(config);
            else
                _selectedConfigClients.Remove(config);

            if (_selectedConfigClients.Count > 0 && !IsSelectedLeagueShowing)
            {
                IsSelectedLeagueShowing = true;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }

            if (_selectedConfigClients.Count == 0)
            {
                IsSelectedLeagueShowing = false;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }
        }
        public async Task OnClearSelectionClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = false);
            IsEdit = false;

            var configs = ConfigClients.Where(x => !x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));

            if (IsSelectedLeagueShowing && _selectedConfigClients.Count == 0)
            {
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
                IsSelectedLeagueShowing = false;
            }
        }
        public async Task OnSelectAllClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = true);
            var configs = ConfigClients.Where(x => x.IsChecked).ToList();

            if (_selectedConfigClients == null)
                _selectedConfigClients = new();

            _selectedConfigClients.RemoveAll(x => configs.Any(cc => cc.ClientCode == x.ClientCode && cc.LeagueId == x.LeagueId && cc.SportId == x.SportId));
            _selectedConfigClients.AddRange(configs);
        }
        public async Task OnEditTemplateClickedEvent()
        {
            var leagues = _selectedConfigClients.Where(x => x.IsChecked).ToList();

            var parameters = new ModalParameters();

            parameters.Add("Leagues", leagues);
            parameters.Add("State", _state);
            parameters.Add("SportId", _stateFilterModel.SportId);

            var selectedPrematch = ConfigClients.Where(x => x.IsChecked && x.PrematchTemplateId > 0).Select(s => s.PrematchTemplateId).Distinct().ToList();
            var selectedLive = ConfigClients.Where(x => x.IsChecked && x.LiveTemplateId > 0).Select(s => s.LiveTemplateId).Distinct().ToList(); ;

            parameters.Add("CurrentPrematchId", selectedPrematch.Count() == 1 ? selectedPrematch.FirstOrDefault() : 0);
            parameters.Add("CurrentLiveId", selectedLive.Count() == 1 ? selectedLive.FirstOrDefault() : 0);

            parameters.Add("Region", _region);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            var options = new ModalOptions();

            switch (_state)
            {
                case ConfigState.Assigned:
                    ModalService.Open<ModifyLeagueTemplateModal>($"Change Template", parameters, options);
                    break;
                case ConfigState.UnAssigned:
                    ModalService.Open<ModifyLeagueTemplateModal>($"Assign Template", parameters, options);
                    break;
            }
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

            if (!IsPreloader)
            {
                IsPreloader = true;
                await InvokeAsync(StateHasChanged);
            }

            var payload = new ConfigTemplateLeagueRequest();
            if (filter != null)
            {
                payload.ClientCodes = filter.ClientCodes ?? new List<string>();
                payload.SportId = filter.SportId;
                payload.SearchText = filter.SearchText;
            }
            payload.MetaData = MetaData;

            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;
            var response = await BackofficeService.GetConfigClient(_state ?? ConfigState.Assigned, payload, token: token);

            if (token.IsCancellationRequested)
                return;

            if (response.StatusCode == StatusCode.Success)
            {
                ConfigClients = new List<ConfigClientViewModel>();
                MetaData = new MetaData();

                if (response.Result != null)
                {
                    if (response.Result.ClientConfigs != null && response.Result.ClientConfigs.Any())
                        ConfigClients = ConfigClientViewModel.ConvertList(response.Result.ClientConfigs);

                    if (response.Result.MetaData != null)
                        MetaData = response.Result.MetaData;
                }

                ConfigClients.ForEach(config =>
                {
                    config.IsChecked = _selectedConfigClients?.FirstOrDefault(x => x.ClientCode == config.ClientCode && x.LeagueId == config.LeagueId && x.SportId == config.SportId)?.IsChecked ?? config.IsChecked;
                });
            }
            IsPreloader = false;
        }

        #endregion
    }
}
