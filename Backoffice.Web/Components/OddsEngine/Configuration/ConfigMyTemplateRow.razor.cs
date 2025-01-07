using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Config;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigMyTemplateRow
    {
        [Inject] private NavigationManager NavigationManager { get; set; }

        [Parameter] public ConfigMyTemplateModel ConfigTemplate { get; set; }
        [Parameter] public EventCallback<ModalParameters> ModalClickedCallback { get; set; }

        private string _region = "HK";

        public void OnEditTemplateClickEvent() => NavigationManager.NavigateTo($"/hk/odds-engine/config-templates/edit/{ConfigTemplate.Id}");
        public void OnCopyTemplateClickEvent() => NavigationManager.NavigateTo($"/hk/odds-engine/config-templates/copy/{ConfigTemplate.Id}");
        public async Task OnExportTemplateClickEventAsync()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalType", "ExportTemplateModal");
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");                                                                                                                                                                                                                                          
            parameters.Add("Region", _region);
            parameters.Add("ConnectionId", _userState.ConnectionId);
            parameters.Add("UserId", _userState.Id);
            parameters.Add("ConfigTemplateId", ConfigTemplate.Id);
            parameters.Add("ConfigTemplateName", ConfigTemplate.Name);

            await ModalClickedCallback.InvokeAsync(parameters);
        }
        public async Task OnViewAssignedClickEventAsync()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalType", "ViewAssignedModal");
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("Region", _region);
            parameters.Add("ConfigTemplateId", ConfigTemplate.Id);

            await ModalClickedCallback.InvokeAsync(parameters);
        }
        public async Task OnDeleteTemplateClickEventAsync()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalType", "DeleteTemplateModal");
            parameters.Add("ModalClass", "modal-show");
            parameters.Add("ModalDialogClass", "modal-md modal-dialog-centered");
            parameters.Add("Region", _region);
            parameters.Add("ConfigTemplateId", ConfigTemplate.Id);
            parameters.Add("ConfigTemplateName", ConfigTemplate.Name);
            parameters.Add("IsReadOnly", ConfigTemplate.IsReadOnly);

            await ModalClickedCallback.InvokeAsync(parameters);
        }
    }
}
