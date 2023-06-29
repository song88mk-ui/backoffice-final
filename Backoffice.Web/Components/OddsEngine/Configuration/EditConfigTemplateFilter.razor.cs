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
            EditConfigTemplateFilterModel.SelectedMarkets = items.Any() ? items.Where(i => i.Value != "all").Select(i => i.Value).ToList() : new List<string>();
            EditConfigTemplateFilterModel.SelectedMarketCount = items.Count;

            await EditConfigTemplateFilterCallback.InvokeAsync(EditConfigTemplateFilterModel);
        }

        public async Task OnConfigCheckboxClickEvent(Dictionary<string, string> items)
        {
            EditConfigTemplateFilterModel.SelectedConfigStatus = items.Any() ? items.Where(i => i.Value != "all").Select(i => i.Value).ToList() : new List<string>();
            EditConfigTemplateFilterModel.SelectedConfigStatusCount = items.Count;

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
            EditConfigTemplateFilterModel.SelectedMarkets = EditConfigTemplateFilterModel.SelectedMarketCount > 0 ? EditConfigTemplateFilterModel.Markets.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList() : new List<string>();

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
            EditConfigTemplateFilterModel.SelectedConfigStatus = EditConfigTemplateFilterModel.SelectedConfigStatusCount > 0 ? EditConfigTemplateFilterModel.ConfigStatuses.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList() : new List<string>();
        }

        #endregion
    }
}
