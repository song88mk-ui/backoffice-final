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
        [Inject] private ITradingService TradingService { get; set; }
        [Parameter] public string Region { get; set; } = "hk";
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }

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

            Clients = await LoadClients();
            Sports = await LoadSports();
            Scopes = await LoadScopes();
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
        private async Task<List<SelectListItemModel<string>>> LoadSports()
        {
            var response = (await TradingService.GetSportLookup()).Result;
            var result = new List<SelectListItemModel<string>>();

            if (result == null)
                return new List<SelectListItemModel<string>>();

            result = response.SportLookup.Select(s => new SelectListItemModel<string>()
            {
                Value = s.SportId.ToString(),
                Name = s.SportName
            }).ToList();

            return result;
        }
        private async Task<List<SelectListItemWithCheckboxModel<string>>> LoadClients()
        {
            var response = (await TradingService.GetClientList()).Result;
            var result = new List<SelectListItemWithCheckboxModel<string>>();

            if (response != null && response.Any())
            {
                if (UserModel.ClientCodes.Any())
                    response = response.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                result = response.Select(e =>
                    new SelectListItemWithCheckboxModel<string>()
                    {
                        IsChecked = true,
                        Name = e.ClientName,
                        Value = e.ClientId.ToString()
                    }).ToList();

                result.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    Value = "All",
                    Name = "All Clients",
                    IsChecked = true
                });
            }

            return result;
        }
        private async Task<List<SelectListItemModel<string>>> LoadScopes()
        {
            return new List<SelectListItemModel<string>>()
            {
                new SelectListItemModel<string>() { Name = "Live", Value = ((int)Scope.Live).ToString() },
                new SelectListItemModel<string>() { Name = "Prematch", Value = ((int)Scope.Prematch).ToString() }
            };
        }
        #endregion
    }
}