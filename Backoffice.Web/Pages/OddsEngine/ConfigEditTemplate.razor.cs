using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigEditTemplate : IDisposable
    {
        #region Properties
        
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public string ConfigName { get; set; }
        [Parameter] public int ScopeId { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Leagues { get; set; }

        public string PageTitle { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = ConfigName;

            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Leagues = new List<SelectListItemWithCheckboxModel<string>>();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = $"odds-engine/config/edit/{PageTitle}",
                    IsCurrentPage = true
                }
            };

            await LoadClients();
            await LoadLeagues();
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

        public async Task OnLeagueCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedLeagueName = string.Empty;
            SelectedLeagueCount = items.Count;

            if (SelectedLeagueCount == 1)
            {
                SelectedLeagueName = items.FirstOrDefault().Value;
            }
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities
        private async Task LoadClients()
        {
            var response = (await TradingService.GetClientList()).Result;

            if (response != null && response.Any())
            {
                if (UserModel.ClientCodes.Any())
                    response = response.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = response.Select(e =>
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

        private async Task LoadLeagues()
        {
            var response = (await TradingService.GetLeagueLookup(21, 0)).Result;

            if (response != null && response.LeagueLookup.Any())
            {
                Leagues = response.LeagueLookup.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.LeagueName,
                        Value = e.LeagueId.ToString()
                    }).ToList();

                Leagues.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "all",
                    Name = "All Leagues",
                    IsChecked = true
                });

                var selectedLeagues = Leagues.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedLeagueCount = selectedLeagues.Count;
                SelectedLeagueName = SelectedLeagueCount > 0 ? string.Empty : Leagues.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }
        }

        #endregion
    }
}
