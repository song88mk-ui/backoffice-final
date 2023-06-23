using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigUnassigned : IDisposable
    {
        #region Properties

        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Sports { get; set; }
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Scopes { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Unassigned Leagues";
            IconClass = "";

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config/unassigned",
                    IsCurrentPage = true
                }
            };

            await Init();
        }

        private async Task Init()
        {
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
