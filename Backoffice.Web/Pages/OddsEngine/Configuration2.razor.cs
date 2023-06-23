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

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigTemplateModel> ConfigTemplates { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }
        public ConfigTemplateNavMenu SelectedConfigNavMenu { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "My Templates";
            IconClass = "";

            ConfigTemplates = new List<ConfigTemplateModel>();
            Sports = new List<SelectListItemModel<string>>();
            Scopes = new List<SelectListItemModel<string>>();
            SelectedConfigNavMenu = ConfigTemplateNavMenu.UnAssigned;

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config/my-templates",
                    IsCurrentPage = true
                }
            };

            await Init();
        }

        public async Task OnLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("DialogStyle", "max-width:1500px");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<ViewMyTemplatesLogs>($"View Logs", parameters, options);
        }

        public async Task OnCreateNewTemplateClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<CreateTemplateModal>($"Create Template", parameters, options);
        }

        public async Task OnEditTemplateClickEvent(ConfigTemplateModel configTemplateModel)
        {
            NavigationManager.NavigateTo($"/hk/odds-engine/config/edit/{configTemplateModel.Id}");
        }

        public async Task OnDeleteTemplateClickEvent(ConfigTemplateModel configTemplateModel)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);
            parameters.Add("ConfigTemplateModel", configTemplateModel);

            var options = new ModalOptions();

            ModalService.Open<DeleteTemplateModal>($"Delete Template", parameters, options);
        }

        public async Task OnExportTemplateClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<ExportTemplateModal>($"Export Templates", parameters, options);
        }

        public async Task OnViewAssignedClickEvent(ConfigTemplateModel configTemplateModel)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("Region", Region);
            parameters.Add("ConfigTemplateModel", configTemplateModel);

            var options = new ModalOptions();

            ModalService.Open<ViewAssignedModal>($"View Assigned", parameters, options);
        }

        public async Task OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());

            //await LoadConfigTemplates();
        }

        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            SelectedScopeId = int.Parse(e.Value.ToString());

            //await LoadConfigTemplates();
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

        private async Task Init()
        {
            Clients = await LoadClients();
            Sports = await LoadSports();
            Scopes = await LoadScopes();

            
            await LoadConfigTemplates();

            StateHasChanged();
        }

        private async Task LoadConfigTemplates()
        {
            ConfigTemplates = (await BackOfficeService.GetClientConfigTemplate(UserModel.Id)).Result;
        }

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