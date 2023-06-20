using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class EditConfigTemplateFilter : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Leagues { get; set; }

        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Leagues = new List<SelectListItemWithCheckboxModel<string>>();

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
