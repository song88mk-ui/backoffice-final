using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
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
        public string SelectedScopeName { get; set; } = "";
        public bool IsPreloader { get; set; } = true;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            PageTitle = "Assigned Leagues";
            IconClass = "";

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

        public async Task OnSportCheckboxClickEvent(ChangeEventArgs e)
        {
            //SelectedSportName = string.Empty;
            //SelectedSportCount = items.Count;

            //if (SelectedSportCount == 1)
            //{
            //    SelectedSportName = items.FirstOrDefault().Value;
            //}
            //SelectedScopeCount = selectedScopes.Count;
            //SelectedScopeName = SelectedScopeCount > 0 ? string.Empty : Scopes.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
        }

        public async Task OnScopeCheckboxClickEvent(ChangeEventArgs e)
        {
            //SelectedScopeName = string.Empty;
            //SelectedScopeCount = items.Count;

            //if (SelectedScopeCount == 1)
            //{
            //    SelectedScopeName = items.FirstOrDefault().Value;
            //}
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
            await LoadLeagues();
        }
        private async Task LoadLeagues()
        {
            IsPreloader = true;

            StateHasChanged();
            var payload = new ConfigTemplateLeagueRequest();
            payload.MetaData = MetaData;
            var response = await BackOfficeService.GetConfigClient(UserModel.Id, ConfigState.Assigned, payload);

            if (response.StatusCode == StatusCode.Success)
            {
                ConfigClients = new List<ConfigClientModel>();
                MetaData = new MetaData();

                if (response.Result != null)
                {
                    if (response.Result.ClientConfigs != null && response.Result.ClientConfigs.Any())
                        ConfigClients = response.Result.ClientConfigs;
                    if (response.Result.MetaData != null)
                        MetaData = response.Result.MetaData;
                }
            }
            IsPreloader = false;
        }

        #endregion
    }
}
