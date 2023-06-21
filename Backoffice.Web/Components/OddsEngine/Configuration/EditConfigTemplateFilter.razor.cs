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
        public List<SelectListItemWithCheckboxModel<string>> Markets { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> ConfigStatuses { get; set; }

        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";
        public int SelectedMarketCount { get; set; }
        public string SelectedMarketName { get; set; } = "";

        public int SelectedConfigStatusCount { get; set; }
        public string SelectedConfigStatusName { get; set; } = "";

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Leagues = new List<SelectListItemWithCheckboxModel<string>>();            
            Markets = new List<SelectListItemWithCheckboxModel<string>>();
            ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>();


            await LoadClients();
            await LoadLeagues();
            await LoadMarkets();
            await LoadConfigStatuses();
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

        public async Task OnMarketCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedMarketName = string.Empty;
            SelectedMarketCount = items.Count;

            if (SelectedMarketCount == 1)
            {
                SelectedMarketName = items.FirstOrDefault().Value;
            }
        }

        public async Task OnConfigCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedConfigStatusName = string.Empty;
            SelectedConfigStatusCount = items.Count;

            if (SelectedConfigStatusCount == 1)
            {
                SelectedConfigStatusName = items.FirstOrDefault().Value;
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

        private async Task LoadMarkets()
        {
            //mock data only

            Markets = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Main Markets", Value = "1" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Points", Value = "2" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Quarter", Value = "3" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Combo", Value = "4" }
            };

            var selectedMarkets = Markets.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            SelectedMarketCount = selectedMarkets.Count;
            SelectedMarketName = SelectedMarketCount > 0 ? string.Empty : Markets.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

            //var response = (await TradingService.GetLeagueLookup(21, 0)).Result;

            //if (response != null && response.LeagueLookup.Any())
            //{
            //    Leagues = response.LeagueLookup.Select(e =>
            //        new SelectListItemWithCheckboxModel<string>()
            //        {
            //            IsChecked = true,
            //            Name = e.LeagueName,
            //            Value = e.LeagueId.ToString()
            //        }).ToList();

            //    Leagues.Insert(0, new SelectListItemWithCheckboxModel<string>
            //    {
            //        Value = "all",
            //        Name = "All Leagues",
            //        IsChecked = true
            //    });

            //    var selectedLeagues = Leagues.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //    SelectedLeagueCount = selectedLeagues.Count;
            //    SelectedLeagueName = SelectedLeagueCount > 0 ? string.Empty : Leagues.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            //}
        }

        private async Task LoadConfigStatuses()
        {
            ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Active Markets", Value = "1" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Inactive Markets", Value = "2" },                
            };

            var selectedConfigs = ConfigStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            SelectedConfigStatusCount = selectedConfigs.Count;
            SelectedConfigStatusName = SelectedConfigStatusCount > 0 ? string.Empty : ConfigStatuses.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        #endregion
    }
}
