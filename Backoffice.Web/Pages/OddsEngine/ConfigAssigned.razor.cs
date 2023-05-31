using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties

        [Parameter] public string Region { get; set; } = "hk";

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Sports { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Scopes { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Assigned Leagues";
            IconClass = "";

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config/assigned",
                    IsCurrentPage = true
                }
            };

            //dummy data
            Clients = new List<SelectListItemWithCheckboxModel<string>>() 
            {
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10 Bet Brazil", Value = "10BetBrazil" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10 Bet Ghana", Value = "10BetGhana" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10 Bet Japan", Value = "10BetJapan" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10 Bet Kenya", Value = "10BetKenya" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10 Bet Nigeria", Value = "10BetNigeria" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "10BetTanzania", Value = "10BetTanzania" }
            };

            Sports = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Soccer", Value = "Soccer" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Basketball", Value = "Basketball" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Baseball", Value = "Baseball" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Ice Hockey", Value = "IceHockey" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Tennis", Value = "Tennis" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Handball", Value = "Handball" }
            };

            Scopes = new List<SelectListItemWithCheckboxModel<string>>()
            {
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Select All", Value = "all" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Live", Value = "Live" },
                new SelectListItemWithCheckboxModel<string>() { IsChecked = true, Name = "Prematch", Value = "Prematch" }
            };
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {

        }

        public async Task OnSportCheckboxClickEvent(Dictionary<string, string> items)
        {

        }

        public async Task OnScopeCheckboxClickEvent(Dictionary<string, string> items)
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
