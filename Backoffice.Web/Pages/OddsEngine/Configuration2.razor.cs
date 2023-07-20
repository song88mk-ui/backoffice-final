using Backoffice.Shared.Enums;
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

        [Parameter]
        [SupplyParameterFromQuery(Name = "M")]
        public string? QueryMessage { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public ConfigTemplateNavMenu SelectedConfigNavMenu { get; set; }

        public bool IsPreleader = true;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            SelectedConfigNavMenu = ConfigTemplateNavMenu.MyTemplates;

            IsPreleader = true;
            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config-templates",
                    IsCurrentPage = true
                }
            };
            await Init();
        }

        public async Task Init()
        {
            Sports = new List<SelectListItemModel<string>>();
            Scopes = new List<SelectListItemModel<string>>();

            var taskList = new List<Task>()
            {
                Task.Run(() => {Clients = LoadClients().Result; }),
                Task.Run(() => {while (Sports.Count == 0){Sports = LoadSports().Result;}}),
                Task.Run(() => {while (Scopes.Count == 0){Scopes = LoadScopes().Result;}})
            };

            Task.WaitAll(taskList.ToArray());
            IsPreleader = false;
        }
        public async Task OnConfigNavMenuSelected(ConfigTemplateNavMenu configNavMenu)
        {
            SelectedConfigNavMenu = configNavMenu;
        }

        public async Task OnModalSavedEvent(object payload)
        {
            StateHasChanged();
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

            if (response == null)
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
                        Value = e.ClientCode.ToString()
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