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

        [Parameter] public EventCallback<EditConfigTemplateFilterModel> EditConfigTemplateFilterCallback { get; set; }

        public EditConfigTemplateFilterModel EditConfigTemplateFilterModel { get; set; }


        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            EditConfigTemplateFilterModel = new EditConfigTemplateFilterModel();
            EditConfigTemplateFilterModel.Markets = new List<SelectListItemWithCheckboxModel<string>>();
            EditConfigTemplateFilterModel.ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>();


            await LoadMarkets();
            await LoadConfigStatuses();

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnMarketCheckboxClickEvent(Dictionary<string, string> items)
        {
            EditConfigTemplateFilterModel.SelectedMarketName = string.Empty;
            EditConfigTemplateFilterModel.SelectedMarketCount = items.Count;

            if (EditConfigTemplateFilterModel.SelectedMarketCount == 1)
            {
                EditConfigTemplateFilterModel.SelectedMarketName = items.FirstOrDefault().Value;
            }

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnConfigCheckboxClickEvent(Dictionary<string, string> items)
        {
            EditConfigTemplateFilterModel.SelectedConfigStatusName = string.Empty;
            EditConfigTemplateFilterModel.SelectedConfigStatusCount = items.Count;

            if (EditConfigTemplateFilterModel.SelectedConfigStatusCount == 1)
            {
                EditConfigTemplateFilterModel.SelectedConfigStatusName = items.FirstOrDefault().Value;
            }

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public void Dispose()
        {
            
        }

        #endregion

        #region Utilities
        
        private async Task LoadMarkets()
        {
            //mock data only

            EditConfigTemplateFilterModel.Markets = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Main Markets", Value = "1" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Points", Value = "2" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Quarter", Value = "3" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Combo", Value = "4" }
            };

            var selectedMarkets = EditConfigTemplateFilterModel.Markets.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            EditConfigTemplateFilterModel.SelectedMarketCount = selectedMarkets.Count;
            EditConfigTemplateFilterModel.SelectedMarketName = EditConfigTemplateFilterModel.SelectedMarketCount > 0 ? string.Empty : EditConfigTemplateFilterModel.Markets.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;

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
            EditConfigTemplateFilterModel.ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Active Markets", Value = "1" },
                new SelectListItemWithCheckboxModel<string>(){ IsChecked = true, Name = "Inactive Markets", Value = "2" },                
            };

            var selectedConfigs = EditConfigTemplateFilterModel.ConfigStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            EditConfigTemplateFilterModel.SelectedConfigStatusCount = selectedConfigs.Count;
            EditConfigTemplateFilterModel.SelectedConfigStatusName = EditConfigTemplateFilterModel.SelectedConfigStatusCount > 0 ? string.Empty : EditConfigTemplateFilterModel.ConfigStatuses.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        #endregion
    }
}
