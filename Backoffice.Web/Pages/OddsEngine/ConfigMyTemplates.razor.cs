using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.Configuration;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ConfigMyTemplates : IDisposable
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
        public List<SelectListItemModel<int>> Sports { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }

        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "My Templates";
            IconClass = "";

            ConfigTemplates = new List<ConfigTemplateModel>();
            Sports = new List<SelectListItemModel<int>>();

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
            NavigationManager.NavigateTo($"/hk/odds-engine/config/edit/{configTemplateModel.SportId}/{configTemplateModel.FixtureTypeId}/{configTemplateModel.Name}");
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

            await LoadConfigTemplates();
        }

        public async Task OnScopeChangedEvent(ChangeEventArgs e)
        {
            SelectedScopeId = int.Parse(e.Value.ToString());

            await LoadConfigTemplates();
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async Task Init()
        {                       
            Sports =  await LoadSports();
            

            await LoadConfigTemplates();
        }

        private async Task LoadConfigTemplates()
        {
            ConfigTemplates =  (await BackOfficeService.GetClientConfigTemplate(UserModel.Id)).Result;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        #endregion
    }
}