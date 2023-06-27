using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Sports { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Scopes { get; set; }
        [Parameter] public ConfigState State { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigClientViewModel> ConfigClients { get; set; } = new List<ConfigClientViewModel>();

        public ConfigStateFilterModel FilterModel = new ConfigStateFilterModel();
        public MetaData MetaData { get; set; }
        public SearchFormModel SearchForm { get; set; } = new SearchFormModel();

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public string SelectedSportName { get; set; } = "";
        public string SelectedScopeName { get; set; } = "";
        public bool IsPreloader { get; set; } = true;
        public bool IsEdit { get; set; } = false;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            IsEdit = false;
            PageTitle = State == ConfigState.Assigned ? "Assigned Leagues" : "Unassigned Leagues";
            IconClass = "";

            SearchForm.SearchText = "";
            FilterModel = new ConfigStateFilterModel()
            {
                ClientCodes = Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                ScopeId = int.Parse(Scopes.FirstOrDefault().Value),
                SportId = int.Parse(Sports.FirstOrDefault().Value),
                SearchText = SearchForm.SearchText
            };
            SelectedClientCount = (Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>()).Count();

            MetaData = new MetaData();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"odds-engine/config/{(State == ConfigState.Assigned ? "assigned":"unassigned")}",
                    IsCurrentPage = true
                }
            };

            await LoadLeagues(FilterModel);
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            FilterModel.ClientCodes = new List<string>();
            if (items != null)
            {
                FilterModel.ClientCodes = items.Keys.Where(x => x.ToUpper() != "ALL").ToList();
            }

            await LoadLeagues(FilterModel);
            //var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //SelectedClientCount = selectedClients.Count;
            //SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnSportCheckboxClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                FilterModel.SportId = int.Parse((e == null ? Sports.FirstOrDefault().Value : e.Value.ToString()));
            }
            await LoadLeagues(FilterModel);
        }

        public async Task OnScopeCheckboxClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                FilterModel.ScopeId = int.Parse((e == null ? Scopes.FirstOrDefault().Value : e.Value.ToString()));
            }
            await LoadLeagues(FilterModel);
        }
        public async Task OnInputSearchEvent()
        {
            FilterModel.SearchText = SearchForm.SearchText;
            await LoadLeagues(FilterModel);
        }

        public async Task SelectedPage(int selectedPage)
        {
            MetaData.CurrentPage = selectedPage;
            await LoadLeagues(FilterModel);
        }

        public async Task OnResetClickEvent()
        {
            MetaData = new MetaData();
            SearchForm.SearchText = "";
            FilterModel = new ConfigStateFilterModel()
            {
                ClientCodes = Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>(),
                ScopeId = int.Parse(Scopes.FirstOrDefault().Value),
                SportId = int.Parse(Sports.FirstOrDefault().Value),
                SearchText = SearchForm.SearchText
            };
            SelectedClientCount = (Clients?.Where(x => x.IsChecked && x.Value.ToUpper() != "ALL").Select(x => x.Value).ToList() ?? new List<string>()).Count();

            await LoadLeagues(FilterModel);
        }
        public async Task OnConfigSelectionChanged()
        {
            if (ConfigClients.Any(x => x.IsChecked))
                IsEdit = true;
            else
                IsEdit = false;
        }
        public async Task OnClearSelectionClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = false);
            IsEdit = false;
        }
        public async Task OnSelectAllClickedEvent()
        {
            ConfigClients.ForEach(x => x.IsChecked = true);
        }
        public async Task OnEditTemplateClickedEvent()
        {
            var leagues = ConfigClients.Where(x => x.IsChecked).ToList();

            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            switch (State)
            {
                case ConfigState.Assigned:
                    ModalService.Open<ModifyLeagueTemplateModal>($"Change Template", parameters, options);
                    break;
                case ConfigState.Unsigned:
                    ModalService.Open<ModifyLeagueTemplateModal>($"Assign Template", parameters, options);
                    break;
            }
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities
        private async Task LoadLeagues(ConfigStateFilterModel filter = null)
        {
            IsPreloader = true;

            StateHasChanged();
            var payload = new ConfigTemplateLeagueRequest();
            if (filter != null)
            {
                payload.ClientCodes = filter.ClientCodes ?? new List<string>();
                payload.SportId = filter.SportId;
                payload.ScopeId = filter.ScopeId;
                payload.SearchText = filter.SearchText;
            }
            payload.MetaData = MetaData;
            var response = await BackOfficeService.GetConfigClient(State, payload);

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
