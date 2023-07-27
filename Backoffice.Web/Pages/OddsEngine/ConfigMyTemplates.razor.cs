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
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        [Parameter] public List<SelectListItemModel<string>> Sports { get; set; }
        [Parameter] public List<SelectListItemModel<string>> Scopes { get; set; }
        [Parameter] public string QueryMessage { get; set; }
        public List<BreadcrumbModel> Breadcrumbs { get; set; }
        public List<ConfigTemplateModel> ConfigTemplates { get; set; }

        public bool IsPreloader { get; set; } = true;
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public int SelectedSportId { get; set; }
        public int SelectedScopeId { get; set; }
        private AlertModel _alertModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;
            PageTitle = "My Templates";
            IconClass = "";

            ConfigTemplates = new List<ConfigTemplateModel>();

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "hk/odds-engine/config/my-templates",
                    IsCurrentPage = true
                }
            };

            SelectedSportId = -1;
            SelectedScopeId = int.Parse(Scopes.FirstOrDefault().Value);


            await LoadConfigTemplates();
        }
        protected override void OnParametersSet()
        {
            if (QueryMessage != null && !string.IsNullOrEmpty(QueryMessage))
            {
                _alertModel = new AlertModel();
                _alertModel.Message = QueryMessage;
                _alertModel.Status = AlertStatus.Success;
                _alertModel.Opacity = 1.0m;
            }
        }
        public async Task OnLogsClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("Sports", Sports);
            parameters.Add("Scopes", Scopes);
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

            parameters.Add("Sports", Sports);
            parameters.Add("Scopes", Scopes);
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<CreateTemplateModal>($"Create Template", parameters, options);
        }

        public async Task OnEditTemplateClickEvent(ConfigTemplateModel configTemplateModel)
        {
            NavigationManager.NavigateTo($"/hk/odds-engine/config-templates/edit/{configTemplateModel.Id}");
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

        public async Task OnExportTemplateClickEvent(ConfigTemplateModel configTemplate)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);
            parameters.Add("ConnectionId", UserModel.ConnectionId);
            parameters.Add("UserId", UserModel.Id);
            parameters.Add("ConfigTemplateId", configTemplate.Id);
            parameters.Add("ConfigTemplateName", configTemplate.Name);

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
        public async Task OnModalSaveEvent(object e)
        {
            if(e is string)
            {
                if(!string.IsNullOrEmpty((string)e))
                {
                    _alertModel = new()
                    {
                        Status = AlertStatus.Success,
                        Message =(string)e
                    };
                }
            }
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

        private async Task LoadConfigTemplates()
        {
            IsPreloader = true;
            StateHasChanged();

            ConfigTemplates = (await BackOfficeService.GetConfigTemplate(SelectedSportId, SelectedScopeId)).Result;

            IsPreloader = false;
        }

        #endregion
    }
}