using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class Configuration2 : IDisposable
    {
        #region Properties
        [Parameter] public string Region { get; set; } = "hk";

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public ConfigTemplateNavMenu SelectedConfigNavMenu { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedConfigNavMenu = ConfigTemplateNavMenu.MyTemplates;

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config/my-templates",
                    IsCurrentPage = true
                }
            };
        }

        public async Task OnConfigNavMenuSelected(ConfigTemplateNavMenu configNavMenu)
        {
            SelectedConfigNavMenu = configNavMenu;
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities        

        #endregion
    }
}