using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties

        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Sports { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Scopes { get; set; }
        public List<ConfigClientModel> ConfigClients { get; set; }

        public MetaData MetaData { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public string SelectedSportName { get; set; } = "";
        public int SelectedSportCount { get; set; }
        public string SelectedScopeName { get; set; } = "";
        public int SelectedScopeCount { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Assigned Leagues";
            IconClass = "";

            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Sports = new List<SelectListItemWithCheckboxModel<string>>();
            Scopes = new List<SelectListItemWithCheckboxModel<string>>();

            MetaData = new MetaData();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config/assigned",
                    IsCurrentPage = true
                }
            };

            await Init();                 
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;            

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnSportCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedSportName = string.Empty;
            SelectedSportCount = items.Count;

            if (SelectedSportCount == 1)
            {
                SelectedSportName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnScopeCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedScopeName = string.Empty;
            SelectedScopeCount = items.Count;

            if (SelectedScopeCount == 1)
            {
                SelectedScopeName = items.FirstOrDefault().Value;
            }
        }

        public async Task SelectedPage()
        {
        }

        public async Task OnResetClickEvent()
        {

        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities

        private async Task Init()
        {
            await LoadClients();
            await LoadSports();
            await LoadScopes();
            await LoadAssignedLeagues();
        }

        private async Task LoadClients()
        {
            var response = (await BackOfficeService.GetClients()).Result;

            if (response != null && response.Clients.Any())
            {
                if (UserModel.ClientCodes.Any())
                    response.Clients = response.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = response.Clients.Select(e => 
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientCode
                    }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Clients",
                    IsChecked = true
                });

                var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedClientCount = selectedClients.Count;
                SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        private async Task LoadSports()
        {
            var sports = (await BackOfficeService.GetSportsAsync()).Result;

            if (sports != null && sports.SportLookup.Any())
            {
                Sports = sports.SportLookup.Select(s => new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = s.SportId.ToString(),
                    Name = s.SportName
                }).ToList();

                Sports.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Sports",
                    IsChecked = true
                });

                var selectedSports = Sports.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                //var sport = Sports.FirstOrDefault(c => c.Value == "1"); // soccer as initial value

                SelectedSportCount = selectedSports.Count;
                SelectedSportName = SelectedSportCount > 0 ? string.Empty : Sports.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        private async Task LoadScopes()
        {
            Scopes = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Live", Value = "Live" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Prematch", Value = "Prematch" }
            };

            var selectedScopes = Scopes.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //var sport = Sports.FirstOrDefault(c => c.Value == "1"); // soccer as initial value

            SelectedScopeCount = selectedScopes.Count;
            SelectedScopeName = SelectedScopeCount > 0 ? string.Empty : Scopes.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        private async Task LoadAssignedLeagues()
        {
            var response = await BackOfficeService.GetConfigClient(UserModel.Id, "assigned", MetaData);

            if (response.StatusCode == StatusCode.Success)
            {

            }
        }

        #endregion
    }
}
