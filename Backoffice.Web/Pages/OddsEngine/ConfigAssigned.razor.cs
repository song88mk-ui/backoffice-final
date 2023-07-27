using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public EventCallback OnStateHasChanged { get; set; }
        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Sports { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Scopes { get; set; }
        [Parameter] public ConfigState State { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigClientViewModel> ConfigClients { get; set; } = new List<ConfigClientViewModel>();
        public MetaData MetaData { get; set; }
        public string IconClass { get; set; }
        public bool IsPreloader { get; set; } = true;
        public bool IsEdit { get; set; } = false;
        public int SelectedLeagueCount { get; set; }
        public bool IsSelectedLeagueShowing { get; set; } = false;

        #endregion
        private string _pageTitle;
        private ConfigState? _state;
        private List<SelectListItemModel<string>> _sports;
        private List<SelectListItemModel<string>> _scopes;
        private ConfigStateFilterModel _filterModel;
        private List<SelectListItemWithCheckboxModel<string>> _clients;
        private SearchFormModel _searchForm;
        private int _selectedClientCount;
        private string _region;
        private string _selectedClientName;
        private AlertModel _alertModel;
        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _state = null;
            IsPreloader = true;
            IsEdit = false;
            IconClass = "";
            _sports = new();
            _scopes = new();
            _clients = new();
            _searchForm = new();
            _alertModel = new();

            _filterModel = new();
            MetaData = new();
            _filterModel = new();
            _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";

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
            if (_region != Region)
                _region = Region;

            if (_filterModel == null)
                _filterModel = new();

            if (!Clients.Equals(_clients))
            {
                _selectedClientName = string.Empty;
                _clients = Clients;
                _filterModel.ClientCodes = Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>();
                _selectedClientCount = _filterModel.ClientCodes.Count();

                if (_selectedClientCount == 1)
                    _selectedClientName = Clients?.FirstOrDefault(x => x.IsChecked && x.Value.ToUpper() != "ALL")?.Name ?? string.Empty;
            }
            if (!Scopes.Equals(_scopes))
            {
                _scopes = Scopes;
                _filterModel.ScopeId = int.Parse(_scopes.FirstOrDefault()?.Value ?? "-1");
                _filterModel.ScopeName = _scopes.FirstOrDefault()?.Name ?? "-";
            }
            if (!Sports.Equals(_sports))
            {
                _sports = Sports;
                _filterModel.SportId = int.Parse(_sports.FirstOrDefault()?.Value ?? "-1");
                _filterModel.SportName = _sports.FirstOrDefault()?.Name ?? "-";
            }

            if (State != _state)
            {
                _state = State;
                _searchForm.SearchText = "";
                _pageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";
                _filterModel = new()
                {
                    ClientCodes = Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                    ScopeId = int.Parse(_scopes.FirstOrDefault()?.Value ?? "-1"),
                    ScopeName = _scopes.FirstOrDefault()?.Name ?? "-",
                    SportId = int.Parse(_sports.FirstOrDefault()?.Value ?? "-1"),
                    SportName = _sports.FirstOrDefault()?.Name ?? "-",
                    SearchText = _searchForm.SearchText
                };
                _selectedClientCount = _filterModel.ClientCodes.Count();

                if (_selectedClientCount == 1)
                    _selectedClientName = Clients?.FirstOrDefault(x => x.IsChecked && x.Value.ToUpper() != "ALL")?.Name ?? string.Empty;
                await LoadLeagues(_filterModel);
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

            _filterModel.ClientCodes = new List<string>();
            if (items != null)
            {
                _filterModel.ClientCodes = items.Keys.Where(x => x.ToUpper() != "ALL").ToList();
            }

            await LoadLeagues(_filterModel);
            //var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //SelectedClientCount = selectedClients.Count;
            //SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnSportCheckboxClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                _filterModel.SportId = int.Parse((e == null ? _sports.FirstOrDefault()?.Value : e.Value?.ToString()) ?? "-1");
                _filterModel.SportName = (e == null ? _sports.FirstOrDefault()?.Name : _sports.FirstOrDefault(x => x.Value == e.Value.ToString())?.Name) ?? string.Empty;
            }
            await LoadLeagues(_filterModel);
        }
        public async Task OnScopeCheckboxClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                _filterModel.ScopeId = int.Parse((e == null ? _scopes.FirstOrDefault()?.Value : e.Value?.ToString()) ?? "-1");
                _filterModel.ScopeName = (e == null ? _scopes.FirstOrDefault()?.Name : _scopes.FirstOrDefault(x => x.Value == e.Value.ToString())?.Name) ?? string.Empty;
            }
            await LoadLeagues(_filterModel);
        }
        public async Task OnModalSaveEvent(object e)
        {
            _alertModel = new();
            await OnStateHasChanged.InvokeAsync();
            await LoadLeagues(_filterModel);

            if(e is AlertModel)
            {
                _alertModel = (AlertModel)e;
            }

            IsSelectedLeagueShowing = false;
            await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
        }

        public async Task OnInputSearchEvent()
        {
            _filterModel.SearchText = _searchForm.SearchText;
            await LoadLeagues(_filterModel);
        }

        public async Task SelectedPage(int selectedPage)
        {
            MetaData.CurrentPage = selectedPage;
            await LoadLeagues(_filterModel);
        }

        public async Task OnResetClickEvent()
        {
            MetaData = new MetaData();
            _searchForm.SearchText = "";
            _filterModel = new ConfigStateFilterModel()
            {
                ClientCodes = _clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                ScopeId = int.Parse(_scopes.FirstOrDefault().Value),
                ScopeName = _scopes.FirstOrDefault().Name,
                SportId = int.Parse(_sports.FirstOrDefault().Value),
                SportName = _sports.FirstOrDefault().Name,
                SearchText = _searchForm.SearchText
            };
            _selectedClientCount = (_clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>()).Count();

            await LoadLeagues(_filterModel);
        }
        public async Task OnConfigSelectionChanged()
        {
            if (ConfigClients.Any(x => x.IsChecked))
                IsEdit = true;
            else
                IsEdit = false;

            SelectedLeagueCount = ConfigClients.Where(x => x.IsChecked).Count();

            if (SelectedLeagueCount > 0 && !IsSelectedLeagueShowing)
            {
                IsSelectedLeagueShowing = true;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }

            if (SelectedLeagueCount == 0)
            {
                IsSelectedLeagueShowing = false;
                await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
            }
        }
        public async Task OnClearSelectionClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = false);
            IsEdit = false;
            SelectedLeagueCount = 0;
            IsSelectedLeagueShowing = false;

            await JSRuntime.InvokeVoidAsync("showSelectedLeagueCount");
        }
        public async Task OnSelectAllClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = true);

            SelectedLeagueCount = ConfigClients.Where(x => x.IsChecked).Count();
        }
        public async Task OnEditTemplateClickedEvent()
        {
            var leagues = ConfigClients.Where(x => x.IsChecked).ToList();

            var parameters = new ModalParameters();

            parameters.Add("Leagues", leagues);
            parameters.Add("State", _state);
            parameters.Add("SportId", _filterModel.SportId);
            parameters.Add("ScopeId", _filterModel.ScopeId);
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
            _state = null;
            GC.Collect();
        }

        #endregion

        #region Utilities
        private async Task LoadLeagues(ConfigStateFilterModel filter = null)
        {
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
                payload.ScopeId = filter.ScopeId;
                payload.SearchText = filter.SearchText;
            }
            payload.MetaData = MetaData;
            var response = await BackOfficeService.GetConfigClient(_state ?? ConfigState.Assigned, payload);

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
            }
            IsPreloader = false;
        }

        #endregion
    }
}
