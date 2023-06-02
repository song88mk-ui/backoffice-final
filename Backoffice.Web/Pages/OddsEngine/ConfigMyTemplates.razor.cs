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

        [Inject] private IModalService ModalService { get; set; }

        public List<BreadcrumbModel> Breadcrumbs { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PageTitle = "My Templates";
            IconClass = "";

            Breadcrumbs = new List<BreadcrumbModel>()
            {
                new BreadcrumbModel
                {
                    Name = "Config",
                    Url = "odds-engine/config/my-templates",
                    IsCurrentPage = true
                }
            };
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

        public async Task OnEditTemplateClickEvent()
        {

        }

        public async Task OnDeleteTemplateClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", Region);

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

        public async Task OnViewAssignedClickEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("Region", Region);

            var options = new ModalOptions();

            ModalService.Open<ViewAssignedModal>($"View Assigned", parameters, options);
        }

        public void Dispose()
        {

        }
        #endregion
    }
}