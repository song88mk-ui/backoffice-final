using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigAssigned : IDisposable
    {
        #region Properties

        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";
        [Parameter] public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Sports { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Scopes { get; set; }

        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigClientModel> ConfigClients { get; set; } = new List<ConfigClientModel>();

        public MetaData MetaData { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public string SelectedSportName { get; set; } = "";
        public int SelectedSportCount { get; set; }
        public string SelectedScopeName { get; set; } = "";
        public int SelectedScopeCount { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Assigned Leagues";
            IconClass = "";

            Clients = new List<SelectListItemWithCheckboxModel<string>>();
            Sports = new List<SelectListItemModel<string>>();
            Scopes = new List<SelectListItemModel<string>>();

            MetaData = new MetaData();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config/assigned",
                    IsCurrentPage = true
                }
            };

            await Init();
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            //var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

            //SelectedClientCount = selectedClients.Count;
            //SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnSportCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedSportName = string.Empty;
            SelectedSportCount = items.Count;

            if (SelectedSportCount == 1)
            {
                SelectedSportName = items.FirstOrDefault().Value;
            }
            //SelectedScopeCount = selectedScopes.Count;
            //SelectedScopeName = SelectedScopeCount > 0 ? string.Empty : Scopes.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnScopeCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedScopeName = string.Empty;
            SelectedScopeCount = items.Count;

            if (SelectedScopeCount == 1)
            {
                SelectedScopeName = items.FirstOrDefault().Value;
            }
        }

        public async Task SelectedPage()
        {
        }

        public async Task OnResetClickEvent()
        {

        }

        public async Task CheckboxClicked(SelectListItemWithCheckboxModel<string> model)
        {
            //if (model.IsChecked == false)
            //{
            //    if (model.Value == "all")
            //    {
            //        Items.ForEach(x =>
            //        {
            //            x.IsChecked = true;
            //        });
            //    }
            //    else
            //    {
            //        model.IsChecked = true;

            //        if (!Items.Where(c => c.Value != "all").Any(c => c.IsChecked == false))
            //            Items[0].IsChecked = true;
            //    }
            //}
            //else
            //{
            //    if (model.Value == "all")
            //    {
            //        Items.ForEach(x =>
            //        {
            //            x.IsChecked = false;
            //        });
            //    }
            //    else
            //    {
            //        model.IsChecked = false;
            //        Items[0].IsChecked = false;
            //    }
            //}

            //var dictionary = Items.Where(c => c.Value != "all" && c.IsChecked).ToDictionary(c => c.Value, c => c.Name);
            //await OnCheckboxCallback.InvokeAsync(dictionary);
        }
        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async Task Init()
        {
            await LoadLeaguesLeagues();
        }
        private async Task LoadLeaguesLeagues()
        {
            var response = await BackOfficeService.GetConfigClient(UserModel.Id, ConfigState.Assigned, MetaData);

            if (response.StatusCode == StatusCode.Success)
            {
                ConfigClients = new List<ConfigClientModel>();

                if (response.Result != null && response.Result.Any())
                {
                    ConfigClients = response.Result;
                }
            }
        }

        #endregion
    }
}
